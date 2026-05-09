#!/usr/bin/env bash
# Version management script for AstroTool
# Handles reading, displaying, and incrementing the application version

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
VERSION_FILE="${REPO_ROOT}/version.txt"

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log() {
  printf "${GREEN}[version]${NC} %s\n" "$*"
}

error() {
  printf "${RED}[version] ERROR${NC}: %s\n" "$*" >&2
  exit 1
}

warn() {
  printf "${YELLOW}[version] WARN${NC}: %s\n" "$*"
}

# Parse semantic version
# Input: "1.2.3"
# Output: major=1, minor=2, patch=3
parse_version() {
  local version="$1"
  local -a parts
  IFS='.' read -ra parts <<< "$version"
  
  if [[ ${#parts[@]} -ne 3 ]]; then
    error "Invalid version format: $version (expected: major.minor.patch)"
  fi
  
  major="${parts[0]}"
  minor="${parts[1]}"
  patch="${parts[2]}"
  
  # Validate numeric values
  if ! [[ "$major" =~ ^[0-9]+$ ]] || ! [[ "$minor" =~ ^[0-9]+$ ]] || ! [[ "$patch" =~ ^[0-9]+$ ]]; then
    error "Version components must be numeric"
  fi
}

# Read current version from file
read_version() {
  if [[ ! -f "$VERSION_FILE" ]]; then
    error "Version file not found: $VERSION_FILE"
  fi
  cat "$VERSION_FILE" | xargs echo -n
}

# Write version to file
write_version() {
  local version="$1"
  echo "$version" > "$VERSION_FILE"
  log "Updated version to $version"
}

# Get current version
get_version() {
  read_version
}

# Show current version
show_version() {
  local version
  version=$(read_version)
  echo "$version"
}

# Increment major version (e.g., 1.2.3 -> 2.0.0)
bump_major() {
  local version
  version=$(read_version)
  parse_version "$version"
  
  local new_version="$((major + 1)).0.0"
  write_version "$new_version"
  echo "$new_version"
}

# Increment minor version (e.g., 1.2.3 -> 1.3.0)
bump_minor() {
  local version
  version=$(read_version)
  parse_version "$version"
  
  local new_version="${major}.$((minor + 1)).0"
  write_version "$new_version"
  echo "$new_version"
}

# Increment patch version (e.g., 1.2.3 -> 1.2.4)
bump_patch() {
  local version
  version=$(read_version)
  parse_version "$version"
  
  local new_version="${major}.${minor}.$((patch + 1))"
  write_version "$new_version"
  echo "$new_version"
}

# Set version explicitly
set_version() {
  local version="$1"
  parse_version "$version" # Validate format
  write_version "$version"
  echo "$version"
}

# Usage information
usage() {
  cat <<'EOF'
AstroTool Version Management

Usage:
  ./scripts/version.sh [command]

Commands:
  get, show       Display current version
  bump-major      Increment major version (1.2.3 -> 2.0.0)
  bump-minor      Increment minor version (1.2.3 -> 1.3.0)
  bump-patch      Increment patch version (1.2.3 -> 1.2.4)
  set <version>   Set specific version (must be in format: major.minor.patch)
  -h, --help      Show this help message

Examples:
  ./scripts/version.sh show
  ./scripts/version.sh bump-minor
  ./scripts/version.sh set 2.0.0
EOF
}

# Main
main() {
  local command="${1:-show}"
  
  case "$command" in
    show|get)
      show_version
      ;;
    bump-major)
      bump_major
      ;;
    bump-minor)
      bump_minor
      ;;
    bump-patch)
      bump_patch
      ;;
    set)
      if [[ $# -lt 2 ]]; then
        error "set command requires a version argument"
      fi
      set_version "$2"
      ;;
    -h|--help|help)
      usage
      ;;
    *)
      error "Unknown command: $command"
      ;;
  esac
}

main "$@"
