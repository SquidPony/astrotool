#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
SOLUTION_PATH="${REPO_ROOT}/AstroTool.slnx"
APP_PROJECT="${REPO_ROOT}/AstroTool/AstroTool.csproj"
CORE_PROJECT="${REPO_ROOT}/AstroTool.Core/AstroTool.Core.csproj"
TEST_PROJECT="${REPO_ROOT}/AstroTool.Core.Tests/AstroTool.Core.Tests.csproj"

CONFIGURATION="Release"
OUTPUT_DIR="${REPO_ROOT}/artifacts"
WINDOWS_RID="win-x64"
RUN_TESTS="true"
STRICT_MODE="false"
PLATFORMS=()

log() {
  printf "[build] %s\n" "$*"
}

warn() {
  printf "[build][warn] %s\n" "$*"
}

usage() {
  cat <<'EOF'
AstroTool CLI build script

Usage:
  ./scripts/build.sh [options]

Options:
  -p, --platform <name>   Platform to build. Repeatable.
                          Values: all, windows, android, ios, maccatalyst
                          Default: all
  -c, --configuration     Build configuration (Debug or Release). Default: Release
  -o, --output            Output directory. Default: ./artifacts
  --windows-rid           Windows runtime identifier. Default: win-x64
  --skip-tests            Skip unit tests
  --strict                Fail when a requested platform is unavailable on this host
  -h, --help              Show this help message

Examples:
  ./scripts/build.sh
  ./scripts/build.sh -p windows -p android
  ./scripts/build.sh -p ios -p maccatalyst --strict
EOF
}

has_value() {
  local needle="$1"
  shift
  local item
  for item in "$@"; do
    if [[ "$item" == "$needle" ]]; then
      return 0
    fi
  done
  return 1
}

normalize_platform() {
  local value
  value="$(echo "$1" | tr '[:upper:]' '[:lower:]')"
  case "$value" in
    all|windows|android|ios|maccatalyst)
      printf "%s" "$value"
      ;;
    *)
      printf ""
      ;;
  esac
}

HOST_OS="$(uname -s | tr '[:upper:]' '[:lower:]')"

can_build_android() {
  if [[ -n "${ANDROID_SDK_ROOT:-}" ]] || [[ -n "${AndroidSdkDirectory:-}" ]]; then
    return 0
  fi
  if [[ -d "${HOME}/Android/Sdk" ]] || [[ -d "${HOME}/Library/Android/sdk" ]]; then
    return 0
  fi
  return 1
}

can_build_apple() {
  if [[ "$HOST_OS" != "darwin" ]]; then
    return 1
  fi
  [[ -d "/Applications/Xcode.app/Contents/Developer" ]]
}

can_build_ios() {
  if ! can_build_apple; then
    return 1
  fi
  # iOS simulator builds require a runtime whose build version matches the installed iphonesimulator SDK.
  # A mismatch causes actool to fail even if a runtime is present.
  local sdk_build
  sdk_build="$(xcrun --sdk iphonesimulator --show-sdk-build-version 2>/dev/null)"
  [[ -n "$sdk_build" ]] && xcrun simctl list runtimes 2>/dev/null | grep -q "$sdk_build"
}

can_build_windows() {
  [[ "$HOST_OS" == "windows_nt" || "$HOST_OS" == "mingw"* || "$HOST_OS" == "cygwin"* ]]
}

is_requested() {
  local platform="$1"
  has_value "all" "${PLATFORMS[@]}" || has_value "$platform" "${PLATFORMS[@]}"
}

handle_unavailable() {
  local platform="$1"
  local reason="$2"
  if [[ "$STRICT_MODE" == "true" ]]; then
    printf "[build][error] Requested platform '%s' is unavailable: %s\n" "$platform" "$reason" >&2
    exit 1
  fi
  warn "Skipping ${platform}: ${reason}"
}

publish_windows() {
  local out_dir="${OUTPUT_DIR}/windows"
  log "Publishing Windows (${WINDOWS_RID}) -> ${out_dir}"
  mkdir -p "$out_dir"
  rm -rf "${out_dir:?}"/*

  dotnet publish "$APP_PROJECT" \
    -f net10.0-windows10.0.19041.0 \
    -c "$CONFIGURATION" \
    -r "$WINDOWS_RID" \
    --self-contained true \
    -o "$out_dir"

  local exe_path="${out_dir}/AstroTool.exe"
  if [[ ! -f "$exe_path" ]]; then
    exe_path="$(find "$out_dir" -maxdepth 1 -type f -name '*.exe' | head -n 1)"
  fi

  if [[ -z "$exe_path" ]]; then
    printf "[build][error] Windows publish did not produce an executable in '%s'\n" "$out_dir" >&2
    exit 1
  fi

  local exe_name
  exe_name="$(basename "$exe_path")"
  local deps_dir="${out_dir}/deps"
  mkdir -p "$deps_dir"

  find "$out_dir" -mindepth 1 -maxdepth 1 \
    ! -name "$exe_name" \
    ! -name deps \
    -exec mv {} "$deps_dir/" \;
}

publish_android() {
  local out_dir="${OUTPUT_DIR}/android"
  log "Publishing Android -> ${out_dir}"
  mkdir -p "$out_dir"
  dotnet publish "$APP_PROJECT" \
    -f net10.0-android \
    -c "$CONFIGURATION" \
    -o "$out_dir" \
    /p:AndroidSigningKeyAlias="" \
    /p:AndroidSigningStorePass=""
}

publish_ios() {
  local out_dir="${OUTPUT_DIR}/ios"
  log "Building iOS simulator app -> ${out_dir}"
  mkdir -p "$out_dir"
  dotnet build "$APP_PROJECT" \
    -f net10.0-ios \
    -c "$CONFIGURATION" \
    -o "$out_dir" \
    -p:EnableAppleTargets=true \
    -p:EnableIosTarget=true \
    /p:RuntimeIdentifier=iossimulator-x64 \
    /p:CodesignKey="" \
    /p:CodesignProvision=""
}

publish_maccatalyst() {
  local out_dir="${OUTPUT_DIR}/maccatalyst"
  log "Building MacCatalyst app -> ${out_dir}"
  mkdir -p "$out_dir"
  dotnet build "$APP_PROJECT" \
    -f net10.0-maccatalyst \
    -c "$CONFIGURATION" \
    -o "$out_dir" \
    -p:EnableAppleTargets=true \
    /p:EnableCodeSigning=false \
    /p:CodesignKey="" \
    /p:CodesignProvision=""
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
      PLATFORMS+=("$normalized")
      shift 2
      ;;
    -c|--configuration)
      CONFIGURATION="$2"
      shift 2
      ;;
    -o|--output)
      OUTPUT_DIR="$2"
      shift 2
      ;;
    --windows-rid)
      WINDOWS_RID="$2"
      shift 2
      ;;
    --skip-tests)
      RUN_TESTS="false"
      shift
      ;;
    --strict)
      STRICT_MODE="true"
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

if [[ ${#PLATFORMS[@]} -eq 0 ]]; then
  PLATFORMS=("all")
fi

mkdir -p "$OUTPUT_DIR"

log "Restoring solution"
dotnet restore "$SOLUTION_PATH"

log "Building core library"
dotnet build "$CORE_PROJECT" -c "$CONFIGURATION" --no-restore

if [[ "$RUN_TESTS" == "true" ]]; then
  log "Running unit tests"
  dotnet test "$TEST_PROJECT" -c "$CONFIGURATION" --no-restore
else
  log "Skipping unit tests"
fi

if is_requested "windows"; then
  if can_build_windows; then
    publish_windows
  else
    handle_unavailable "windows" "Windows cross-compilation is not supported on this host (${HOST_OS}). Build on a Windows machine."
  fi
fi

if is_requested "android"; then
  if can_build_android; then
    publish_android
  else
    handle_unavailable "android" "Android SDK not found. Set ANDROID_SDK_ROOT or install Android tooling."
  fi
fi

if is_requested "ios"; then
  if can_build_ios; then
    publish_ios
  else
    handle_unavailable "ios" "Requires macOS with Xcode and an iOS simulator runtime whose build version matches the installed SDK ($(xcrun --sdk iphonesimulator --show-sdk-build-version 2>/dev/null || echo 'unknown')). Install the matching runtime via Xcode → Platforms."
  fi
fi

if is_requested "maccatalyst"; then
  if can_build_apple; then
    publish_maccatalyst
  else
    handle_unavailable "maccatalyst" "Requires macOS with full Xcode installation."
  fi
fi

SUMMARY_FILE="${OUTPUT_DIR}/BUILD_SUMMARY.txt"
{
  echo "AstroTool build summary"
  echo "Configuration: ${CONFIGURATION}"
  echo "Output root: ${OUTPUT_DIR}"
  echo ""
  echo "Expected artifacts by platform:"
  echo "- Windows: ${OUTPUT_DIR}/windows"
  echo "- Android: ${OUTPUT_DIR}/android"
  echo "- iOS: ${OUTPUT_DIR}/ios"
  echo "- MacCatalyst: ${OUTPUT_DIR}/maccatalyst"
} > "$SUMMARY_FILE"

log "Build completed. Summary: ${SUMMARY_FILE}"
