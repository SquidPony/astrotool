Param(
    [string[]]$Platform = @("all"),
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Output = "artifacts",
    [string]$WindowsRid = "win-x64",
    [switch]$SkipTests,
    [switch]$Strict
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
$SolutionPath = Join-Path $RepoRoot "AstroTool.slnx"
$AppProject = Join-Path $RepoRoot "AstroTool/AstroTool.csproj"
$CoreProject = Join-Path $RepoRoot "AstroTool.Core/AstroTool.Core.csproj"
$TestProject = Join-Path $RepoRoot "AstroTool.Core.Tests/AstroTool.Core.Tests.csproj"
$OutputRoot = if ([System.IO.Path]::IsPathRooted($Output)) { $Output } else { Join-Path $RepoRoot $Output }
$AndroidSdkPath = $null
$AndroidUnavailableReason = $null

function Write-Log {
    Param([string]$Message)
    Write-Host "[build] $Message"
}

function Write-Warn {
    Param([string]$Message)
    Write-Host "[build][warn] $Message" -ForegroundColor Yellow
}

function Invoke-Dotnet {
    Param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet command failed with exit code ${LASTEXITCODE}: dotnet $($Arguments -join ' ')"
    }
}

function Resolve-Platform {
    Param([string]$Value)

    $normalized = $Value.ToLowerInvariant()
    switch ($normalized) {
        "all" { return "all" }
        "windows" { return "windows" }
        "android" { return "android" }
        "ios" { return "ios" }
        "maccatalyst" { return "maccatalyst" }
        default { throw "Unknown platform: $Value" }
    }
}

function Is-Requested {
    Param([string]$Name, [string[]]$Selected)

    return $Selected -contains "all" -or $Selected -contains $Name
}

function Handle-Unavailable {
    Param([string]$PlatformName, [string]$Reason)

    if ($Strict) {
        throw "Requested platform '$PlatformName' is unavailable: $Reason"
    }

    Write-Warn "Skipping ${PlatformName}: $Reason"
}

function Can-BuildAndroid {
    $script:AndroidUnavailableReason = $null

    if ($AndroidSdkPath) {
        # Path already detected from a previous check.
    }

    if (-not $AndroidSdkPath) {
        if ($env:AndroidSdkDirectory -and (Test-Path $env:AndroidSdkDirectory)) {
            $script:AndroidSdkPath = $env:AndroidSdkDirectory
        }
    }

    if (-not $AndroidSdkPath) {
        if ($env:ANDROID_SDK_ROOT -and (Test-Path $env:ANDROID_SDK_ROOT)) {
            $script:AndroidSdkPath = $env:ANDROID_SDK_ROOT
        }
    }

    if (-not $AndroidSdkPath) {
        $defaultPaths = @(
            (Join-Path $HOME "AppData/Local/Android/Sdk"),
            (Join-Path $HOME "Library/Android/sdk")
        )

        foreach ($path in $defaultPaths) {
            if (Test-Path $path) {
                $script:AndroidSdkPath = $path
                break
            }
        }
    }

    if (-not $AndroidSdkPath) {
        $script:AndroidUnavailableReason = "Android SDK not found. Set ANDROID_SDK_ROOT or AndroidSdkDirectory."
        return $false
    }

    $javaVersion = $null
    $javaCommand = $null

    if ($env:JAVA_HOME) {
        $javaFromHome = Join-Path $env:JAVA_HOME "bin/java.exe"
        if (Test-Path $javaFromHome) {
            $javaCommand = $javaFromHome
        }
    }

    if (-not $javaCommand) {
        $javaInfo = Get-Command java -ErrorAction SilentlyContinue
        if ($javaInfo) {
            $javaCommand = $javaInfo.Source
        }
    }

    if (-not $javaCommand) {
        $script:AndroidUnavailableReason = "Java JDK not found. Install JDK 21 and set JAVA_HOME."
        return $false
    }

    $versionOutput = (& $javaCommand -version 2>&1 | Select-Object -First 1)
    if ($versionOutput -match '"(?<major>\d+)') {
        $javaVersion = [int]$Matches.major
    }

    if (-not $javaVersion) {
        $script:AndroidUnavailableReason = "Unable to determine Java version from '$javaCommand -version'."
        return $false
    }

    if ($javaVersion -ne 21) {
        $script:AndroidUnavailableReason = "Unsupported JDK version $javaVersion detected. Android build requires JDK 21."
        return $false
    }

    return $true
}

function Can-BuildApple {
    if (-not $IsMacOS) {
        return $false
    }

    return Test-Path "/Applications/Xcode.app/Contents/Developer"
}

function Publish-Windows {
    Param([string]$OutDir)

    Write-Log "Publishing Windows ($WindowsRid) -> $OutDir"
    New-Item -Path $OutDir -ItemType Directory -Force | Out-Null

    # Ensure the output folder only contains files from this publish.
    Get-ChildItem -Path $OutDir -Force -ErrorAction SilentlyContinue | Remove-Item -Recurse -Force

    $args = @(
        "publish",
        $AppProject,
        "-f", "net10.0-windows10.0.19041.0",
        "-c", $Configuration,
        "-r", $WindowsRid,
        "--self-contained", "true",
        "/p:WindowsAppSdkBootstrapInitialize=true",
        "/p:WindowsAppSDKSelfContained=true",
        "-o", $OutDir
    )

    Invoke-Dotnet $args

    $exePath = Join-Path $OutDir "AstroTool.exe"
    if (-not (Test-Path $exePath)) {
        $exePath = Get-ChildItem -Path $OutDir -Filter "*.exe" -File | Select-Object -First 1 -ExpandProperty FullName
    }

    if (-not $exePath) {
        throw "Windows publish did not produce an executable in '$OutDir'."
    }

    $exeName = Split-Path $exePath -Leaf
    $depsDir = Join-Path $OutDir "deps"
    New-Item -Path $depsDir -ItemType Directory -Force | Out-Null

    Get-ChildItem -Path $OutDir -Force |
        Where-Object { $_.Name -ne $exeName -and $_.Name -ne "deps" } |
        ForEach-Object {
            Move-Item -Path $_.FullName -Destination (Join-Path $depsDir $_.Name)
        }
}

function Publish-Android {
    Param([string]$OutDir)

    Write-Log "Publishing Android -> $OutDir"
    New-Item -Path $OutDir -ItemType Directory -Force | Out-Null

    $args = @(
        "publish",
        $AppProject,
        "-f", "net10.0-android",
        "-c", $Configuration,
        "-o", $OutDir,
        "/p:AndroidSigningKeyAlias=",
        "/p:AndroidSigningStorePass="
    )

    if ($AndroidSdkPath) {
        $args += "/p:AndroidSdkDirectory=$AndroidSdkPath"
    }

    Invoke-Dotnet $args
}

function Publish-iOS {
    Param([string]$OutDir)

    Write-Log "Building iOS simulator app -> $OutDir"
    New-Item -Path $OutDir -ItemType Directory -Force | Out-Null

    $args = @(
        "build",
        $AppProject,
        "-f", "net10.0-ios",
        "-c", $Configuration,
        "-o", $OutDir,
        "-p:EnableAppleTargets=true",
        "/p:RuntimeIdentifier=iossimulator-x64",
        "/p:CodesignKey=",
        "/p:CodesignProvision="
    )

    Invoke-Dotnet $args
}

function Publish-MacCatalyst {
    Param([string]$OutDir)

    Write-Log "Building MacCatalyst app -> $OutDir"
    New-Item -Path $OutDir -ItemType Directory -Force | Out-Null

    $args = @(
        "build",
        $AppProject,
        "-f", "net10.0-maccatalyst",
        "-c", $Configuration,
        "-o", $OutDir,
        "-p:EnableAppleTargets=true",
        "/p:EnableCodeSigning=false",
        "/p:CodesignKey=",
        "/p:CodesignProvision="
    )

    Invoke-Dotnet $args
}

$selectedPlatforms = @()
foreach ($entry in $Platform) {
    $selectedPlatforms += Resolve-Platform $entry
}

New-Item -Path $OutputRoot -ItemType Directory -Force | Out-Null

Write-Log "Restoring solution"
Invoke-Dotnet @("restore", $SolutionPath)

Write-Log "Building core library"
Invoke-Dotnet @("build", $CoreProject, "-c", $Configuration, "--no-restore")

if (-not $SkipTests) {
    Write-Log "Running unit tests"
    Invoke-Dotnet @("test", $TestProject, "-c", $Configuration, "--no-restore")
}
else {
    Write-Log "Skipping unit tests"
}

if (Is-Requested "windows" $selectedPlatforms) {
    Publish-Windows (Join-Path $OutputRoot "windows")
}

if (Is-Requested "android" $selectedPlatforms) {
    if (Can-BuildAndroid) {
        Publish-Android (Join-Path $OutputRoot "android")
    }
    else {
        Handle-Unavailable "android" $AndroidUnavailableReason
    }
}

if (Is-Requested "ios" $selectedPlatforms) {
    if (Can-BuildApple) {
        Publish-iOS (Join-Path $OutputRoot "ios")
    }
    else {
        Handle-Unavailable "ios" "Requires macOS with full Xcode installation."
    }
}

if (Is-Requested "maccatalyst" $selectedPlatforms) {
    if (Can-BuildApple) {
        Publish-MacCatalyst (Join-Path $OutputRoot "maccatalyst")
    }
    else {
        Handle-Unavailable "maccatalyst" "Requires macOS with full Xcode installation."
    }
}

$summaryPath = Join-Path $OutputRoot "BUILD_SUMMARY.txt"
@(
    "AstroTool build summary"
    "Configuration: $Configuration"
    "Output root: $OutputRoot"
    ""
    "Expected artifacts by platform:"
    "- Windows: $(Join-Path $OutputRoot 'windows')"
    "- Android: $(Join-Path $OutputRoot 'android')"
    "- iOS: $(Join-Path $OutputRoot 'ios')"
    "- MacCatalyst: $(Join-Path $OutputRoot 'maccatalyst')"
) | Set-Content -Path $summaryPath

Write-Log "Build completed. Summary: $summaryPath"
