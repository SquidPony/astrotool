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

function Write-Log {
    Param([string]$Message)
    Write-Host "[build] $Message"
}

function Write-Warn {
    Param([string]$Message)
    Write-Host "[build][warn] $Message" -ForegroundColor Yellow
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

    Write-Warn "Skipping $PlatformName: $Reason"
}

function Can-BuildAndroid {
    if ($env:ANDROID_SDK_ROOT -or $env:AndroidSdkDirectory) {
        return $true
    }

    $defaultPaths = @(
        (Join-Path $HOME "AppData/Local/Android/Sdk"),
        (Join-Path $HOME "Library/Android/sdk")
    )

    foreach ($path in $defaultPaths) {
        if (Test-Path $path) {
            return $true
        }
    }

    return $false
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

    dotnet publish $AppProject `
        -f net10.0-windows10.0.19041.0 `
        -c $Configuration `
        -r $WindowsRid `
        --self-contained true `
        -o $OutDir
}

function Publish-Android {
    Param([string]$OutDir)

    Write-Log "Publishing Android -> $OutDir"
    New-Item -Path $OutDir -ItemType Directory -Force | Out-Null

    dotnet publish $AppProject `
        -f net10.0-android `
        -c $Configuration `
        -o $OutDir `
        /p:AndroidSigningKeyAlias="" `
        /p:AndroidSigningStorePass=""
}

function Publish-iOS {
    Param([string]$OutDir)

    Write-Log "Building iOS simulator app -> $OutDir"
    New-Item -Path $OutDir -ItemType Directory -Force | Out-Null

    dotnet build $AppProject `
        -f net10.0-ios `
        -c $Configuration `
        -o $OutDir `
        -p:EnableAppleTargets=true `
        /p:RuntimeIdentifier=iossimulator-x64 `
        /p:CodesignKey="" `
        /p:CodesignProvision=""
}

function Publish-MacCatalyst {
    Param([string]$OutDir)

    Write-Log "Building MacCatalyst app -> $OutDir"
    New-Item -Path $OutDir -ItemType Directory -Force | Out-Null

    dotnet build $AppProject `
        -f net10.0-maccatalyst `
        -c $Configuration `
        -o $OutDir `
        -p:EnableAppleTargets=true `
        /p:EnableCodeSigning=false `
        /p:CodesignKey="" `
        /p:CodesignProvision=""
}

$selectedPlatforms = @()
foreach ($entry in $Platform) {
    $selectedPlatforms += Resolve-Platform $entry
}

New-Item -Path $OutputRoot -ItemType Directory -Force | Out-Null

Write-Log "Restoring solution"
dotnet restore $SolutionPath

Write-Log "Building core library"
dotnet build $CoreProject -c $Configuration --no-restore

if (-not $SkipTests) {
    Write-Log "Running unit tests"
    dotnet test $TestProject -c $Configuration --no-restore
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
        Handle-Unavailable "android" "Android SDK not found. Set ANDROID_SDK_ROOT or install Android tooling."
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
