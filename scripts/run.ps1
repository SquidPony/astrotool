Param(
    [ValidateSet("auto", "windows", "android", "ios", "maccatalyst")]
    [string]$Platform = "auto",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$NoRestore
)

$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RepoRoot = Resolve-Path (Join-Path $ScriptDir "..")
$AppProject = Join-Path $RepoRoot "AstroTool/AstroTool.csproj"

function Write-Log {
    Param([string]$Message)
    Write-Host "[run] $Message"
}

function Is-WindowsHost {
    return $IsWindows
}

function Has-AndroidSdk {
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

function Has-Xcode {
    if (-not $IsMacOS) {
        return $false
    }

    return (Test-Path "/Applications/Xcode.app/Contents/Developer")
}

function Resolve-AutoPlatform {
    if ($IsMacOS) {
        return "maccatalyst"
    }

    if ($IsWindows) {
        return "windows"
    }

    if (Has-AndroidSdk) {
        return "android"
    }

    return $null
}

function Run-Dotnet {
    Param([string]$TargetFramework, [string[]]$ExtraArgs = @())

    $args = @("run", "--project", $AppProject, "-f", $TargetFramework, "-c", $Configuration)
    if ($NoRestore) {
        $args += "--no-restore"
    }

    $args += $ExtraArgs

    Write-Log "Executing: dotnet $($args -join ' ')"
    & dotnet @args
}

if ($Platform -eq "auto") {
    $resolved = Resolve-AutoPlatform
    if (-not $resolved) {
        throw "Could not determine a runnable local platform. On Linux, install Android SDK and run with -Platform android."
    }

    $Platform = $resolved
    Write-Log "Auto selected platform: $Platform"
}

switch ($Platform) {
    "windows" {
        if (-not (Is-WindowsHost)) {
            throw "Windows runs are only supported from a Windows host."
        }

        Run-Dotnet -TargetFramework "net10.0-windows10.0.19041.0"
        break
    }
    "android" {
        if (-not (Has-AndroidSdk)) {
            throw "Android SDK not found. Set ANDROID_SDK_ROOT or install Android tooling."
        }

        Run-Dotnet -TargetFramework "net10.0-android"
        break
    }
    "ios" {
        if (-not (Has-Xcode)) {
            throw "iOS run requires macOS with full Xcode installation."
        }

        Run-Dotnet -TargetFramework "net10.0-ios" -ExtraArgs @(
            "-p:EnableAppleTargets=true",
            "/p:RuntimeIdentifier=iossimulator-x64",
            "/p:CodesignKey=",
            "/p:CodesignProvision="
        )
        break
    }
    "maccatalyst" {
        if (-not (Has-Xcode)) {
            throw "MacCatalyst run requires macOS with full Xcode installation."
        }

        Run-Dotnet -TargetFramework "net10.0-maccatalyst" -ExtraArgs @(
            "-p:EnableAppleTargets=true",
            "/p:EnableCodeSigning=false",
            "/p:CodesignKey=",
            "/p:CodesignProvision="
        )
        break
    }
}
