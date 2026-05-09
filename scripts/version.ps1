# Version management script for AstroTool
# Handles reading, displaying, and incrementing the application version

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$repoRoot = Split-Path -Parent $scriptDir
$versionFile = Join-Path $repoRoot "version.txt"

# Helper functions
function Write-Log {
    param([string]$Message)
    Write-Host "[version] $Message" -ForegroundColor Green
}

function Write-Error-Custom {
    param([string]$Message)
    Write-Host "[version] ERROR: $Message" -ForegroundColor Red
    exit 1
}

function Write-Warn {
    param([string]$Message)
    Write-Host "[version] WARN: $Message" -ForegroundColor Yellow
}

# Parse semantic version
# Input: "1.2.3"
# Output: [int]$major, [int]$minor, [int]$patch
function Parse-Version {
    param([string]$version)
    
    $parts = $version -split '\.'
    
    if ($parts.Count -ne 3) {
        Write-Error-Custom "Invalid version format: $version (expected: major.minor.patch)"
    }
    
    $major = [int]$parts[0]
    $minor = [int]$parts[1]
    $patch = [int]$parts[2]
    
    return @{
        Major = $major
        Minor = $minor
        Patch = $patch
    }
}

# Read current version from file
function Read-Version {
    if (-not (Test-Path $versionFile)) {
        Write-Error-Custom "Version file not found: $versionFile"
    }
    return (Get-Content $versionFile).Trim()
}

# Write version to file
function Write-Version {
    param([string]$version)
    Set-Content -Path $versionFile -Value $version -NoNewline
    Write-Log "Updated version to $version"
}

# Get current version
function Get-Version {
    return Read-Version
}

# Show current version
function Show-Version {
    $version = Read-Version
    Write-Output $version
}

# Increment major version (e.g., 1.2.3 -> 2.0.0)
function Bump-Major {
    $version = Read-Version
    $parsed = Parse-Version $version
    
    $newVersion = "{0}.0.0" -f ($parsed.Major + 1)
    Write-Version $newVersion
    Write-Output $newVersion
}

# Increment minor version (e.g., 1.2.3 -> 1.3.0)
function Bump-Minor {
    $version = Read-Version
    $parsed = Parse-Version $version
    
    $newVersion = "{0}.{1}.0" -f $parsed.Major, ($parsed.Minor + 1)
    Write-Version $newVersion
    Write-Output $newVersion
}

# Increment patch version (e.g., 1.2.3 -> 1.2.4)
function Bump-Patch {
    $version = Read-Version
    $parsed = Parse-Version $version
    
    $newVersion = "{0}.{1}.{2}" -f $parsed.Major, $parsed.Minor, ($parsed.Patch + 1)
    Write-Version $newVersion
    Write-Output $newVersion
}

# Set version explicitly
function Set-Version {
    param([string]$version)
    $parsed = Parse-Version $version  # Validate format
    Write-Version $version
    Write-Output $version
}

# Usage information
function Show-Usage {
    @"
AstroTool Version Management

Usage:
  .\scripts\version.ps1 [command] [args]

Commands:
  get, show       Display current version
  bump-major      Increment major version (1.2.3 -> 2.0.0)
  bump-minor      Increment minor version (1.2.3 -> 1.3.0)
  bump-patch      Increment patch version (1.2.3 -> 1.2.4)
  set <version>   Set specific version (must be in format: major.minor.patch)
  -h, --help      Show this help message

Examples:
  .\scripts\version.ps1 show
  .\scripts\version.ps1 bump-minor
  .\scripts\version.ps1 set 2.0.0
"@
}

# Main
$command = $args[0]
if ([string]::IsNullOrEmpty($command)) {
    $command = "show"
}

switch ($command) {
    { @("show", "get") -contains $_ } {
        Show-Version
    }
    "bump-major" {
        Bump-Major
    }
    "bump-minor" {
        Bump-Minor
    }
    "bump-patch" {
        Bump-Patch
    }
    "set" {
        if ($args.Count -lt 2) {
            Write-Error-Custom "set command requires a version argument"
        }
        Set-Version $args[1]
    }
    { @("-h", "--help", "help") -contains $_ } {
        Show-Usage
    }
    default {
        Write-Error-Custom "Unknown command: $command"
    }
}
