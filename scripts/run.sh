#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
APP_PROJECT="${REPO_ROOT}/AstroTool/AstroTool.csproj"

PLATFORM="auto"
CONFIGURATION="Debug"
NO_RESTORE="false"

log() {
  printf "[run] %s\n" "$*"
}

usage() {
  cat <<'EOF'
AstroTool local run script

Usage:
  ./scripts/run.sh [options]

Options:
  -p, --platform <name>   Platform to run.
                          Values: auto, windows, android, ios, maccatalyst
                          Default: auto
  -c, --configuration     Build configuration (Debug or Release). Default: Debug
  --no-restore            Skip restore before run
  -h, --help              Show this help message

Examples:
  ./scripts/run.sh
  ./scripts/run.sh --platform android
  ./scripts/run.sh --platform ios --configuration Release
EOF
}

normalize_platform() {
  local value
  value="$(echo "$1" | tr '[:upper:]' '[:lower:]')"
  case "$value" in
    auto|windows|android|ios|maccatalyst)
      printf "%s" "$value"
      ;;
    *)
      printf ""
      ;;
  esac
}

HOST_OS="$(uname -s | tr '[:upper:]' '[:lower:]')"

is_windows_host() {
  [[ "$HOST_OS" == mingw* ]] || [[ "$HOST_OS" == msys* ]] || [[ "$HOST_OS" == cygwin* ]] || [[ "$HOST_OS" == *"nt"* ]]
}

is_wsl() {
  [[ -n "${WSL_DISTRO_NAME:-}" ]] || [[ -f /proc/version && "$(< /proc/version)" == *"Microsoft"* ]]
}

has_android_sdk() {
  if [[ -n "${ANDROID_SDK_ROOT:-}" ]] || [[ -n "${AndroidSdkDirectory:-}" ]]; then
    return 0
  fi
  if [[ -d "${HOME}/Android/Sdk" ]] || [[ -d "${HOME}/Library/Android/sdk" ]]; then
    return 0
  fi
  return 1
}

has_xcode() {
  [[ "$HOST_OS" == "darwin" ]] && [[ -d "/Applications/Xcode.app/Contents/Developer" ]]
}

resolve_auto_platform() {
  if [[ "$HOST_OS" == "darwin" ]]; then
    printf "maccatalyst"
    return
  fi

  if is_windows_host; then
    printf "windows"
    return
  fi

  if has_android_sdk; then
    printf "android"
    return
  fi

  printf ""
}

run_dotnet() {
  local tfm="$1"
  shift

  local cmd=(dotnet run --project "$APP_PROJECT" -f "$tfm" -c "$CONFIGURATION")
  if [[ "$NO_RESTORE" == "true" ]]; then
    cmd+=(--no-restore)
  fi
  cmd+=("$@")

  log "Executing: ${cmd[*]}"
  "${cmd[@]}"
}

run_windows_from_wsl() {
  local ps1_path="${REPO_ROOT}/scripts/run.ps1"
  local ps1_windows_path
  ps1_windows_path="$(wslpath -w "$ps1_path")"

  local cmd=(powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$ps1_windows_path" -Platform windows -Configuration "$CONFIGURATION")
  if [[ "$NO_RESTORE" == "true" ]]; then
    cmd+=(-NoRestore)
  fi

  log "Executing in Windows from WSL: ${cmd[*]}"
  "${cmd[@]}"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    -p|--platform)
      if [[ $# -lt 2 ]]; then
        printf "Missing value for %s\n" "$1" >&2
        usage
        exit 1
      fi
      normalized="$(normalize_platform "$2")"
      if [[ -z "$normalized" ]]; then
        printf "Unknown platform: %s\n" "$2" >&2
        usage
        exit 1
      fi
      PLATFORM="$normalized"
      shift 2
      ;;
    -c|--configuration)
      CONFIGURATION="$2"
      shift 2
      ;;
    --no-restore)
      NO_RESTORE="true"
      shift
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      printf "Unknown argument: %s\n" "$1" >&2
      usage
      exit 1
      ;;
  esac
done

if [[ "$PLATFORM" == "auto" ]]; then
  PLATFORM="$(resolve_auto_platform)"
  if [[ -z "$PLATFORM" ]]; then
    printf "[run][error] Could not determine a runnable local platform.\n" >&2
    printf "[run][error] On Linux, install Android SDK and run with --platform android.\n" >&2
    exit 1
  fi
  log "Auto selected platform: ${PLATFORM}"
fi

case "$PLATFORM" in
  windows)
    if is_windows_host; then
      run_dotnet "net10.0-windows10.0.19041.0"
      exit 0
    fi

    if is_wsl; then
      if ! command -v powershell.exe >/dev/null 2>&1; then
        printf "[run][error] powershell.exe was not found in WSL. Enable Windows interop or install PowerShell on Windows.\n" >&2
        exit 1
      fi
      run_windows_from_wsl
      exit 0
    fi

    printf "[run][error] Windows runs are supported on native Windows or WSL only.\n" >&2
    exit 1
    ;;
  android)
    if ! has_android_sdk; then
      printf "[run][error] Android SDK not found. Set ANDROID_SDK_ROOT or install Android tooling.\n" >&2
      exit 1
    fi
    run_dotnet "net10.0-android"
    ;;
  ios)
    if ! has_xcode; then
      printf "[run][error] iOS run requires macOS with full Xcode installation.\n" >&2
      exit 1
    fi
    run_dotnet "net10.0-ios" -p:EnableAppleTargets=true /p:RuntimeIdentifier=iossimulator-x64 /p:CodesignKey="" /p:CodesignProvision=""
    ;;
  maccatalyst)
    if ! has_xcode; then
      printf "[run][error] MacCatalyst run requires macOS with full Xcode installation.\n" >&2
      exit 1
    fi
    run_dotnet "net10.0-maccatalyst" -p:EnableAppleTargets=true /p:EnableCodeSigning=false /p:CodesignKey="" /p:CodesignProvision=""
    ;;
  *)
    printf "[run][error] Unsupported platform: %s\n" "$PLATFORM" >&2
    exit 1
    ;;
esac
