# AstroTool Build & Release Guide

## Supported Platforms

AstroTool is a cross-platform application built with MAUI (.NET Multi-platform App UI). The following platforms are officially supported and have automated builds:

| Platform | Architecture | Format | Status |
|----------|--------------|--------|--------|
| **Windows** | x64 | Self-contained EXE | ✅ Full support |
| **Android** | ARM64 | APK | ✅ Full support |
| **iOS** | Simulator (ARM64) | App Bundle | ✅ Full support |
| **macOS** | ARM64 | macCatalyst App | ✅ Full support |
| **Linux** | x64 | - | ⚠️ Not supported (see note below) |

### Platform Notes

#### Linux Support
MAUI does not officially support Linux UI applications. To add Linux support in the future, one of the following approaches would be needed:

1. **GTK Support**: Add GTK bindings to MAUI and implement Linux-specific UI code
2. **Alternative Framework**: Migrate to a framework like Avalonia that has native Linux support
3. **Headless/Server**: Build as a backend service without UI

For now, Linux builds are not included in the release process.

## Version Management

The application uses semantic versioning stored in `version.txt` (format: `major.minor.patch`).

### Version Commands

```bash
# Show current version
./scripts/version.sh show

# Increment versions
./scripts/version.sh bump-major  # 1.2.3 → 2.0.0
./scripts/version.sh bump-minor  # 1.2.3 → 1.3.0
./scripts/version.sh bump-patch  # 1.2.3 → 1.2.4

# Set specific version
./scripts/version.sh set 2.5.0
```

Windows equivalents use `.ps1` files:
```powershell
.\scripts\version.ps1 show
.\scripts\version.ps1 bump-minor
```

### Automatic Version Bumping

The CI/CD pipeline automatically bumps the minor version after each successful merge to `main`:
- When a PR is merged to `main`, a GitHub Release is created
- After the release is created, the minor version is automatically incremented
- The new version is committed back to the repository

## Local Build

### Prerequisites

- .NET 10.0 SDK
- Platform-specific requirements:
  - **Windows**: Windows 10.0.19041 or later
  - **Android**: Android SDK (automatically detected)
  - **iOS/macCatalyst**: macOS with Xcode
  - **All platforms**: dotnet MAUI workload

### Build Commands

```bash
# Build and test (all platforms available on this system)
./scripts/build.sh

# Build specific platforms
./scripts/build.sh -p windows
./scripts/build.sh -p android
./scripts/build.sh -p ios
./scripts/build.sh -p maccatalyst

# Build multiple platforms
./scripts/build.sh -p windows -p android

# With strict mode (fail if platform unavailable)
./scripts/build.sh -p ios --strict

# Debug configuration
./scripts/build.sh -c Debug
```

### Output Artifacts

Built artifacts are placed in `artifacts/` directory:
- **Windows**: `artifacts/windows/astrotool-windows-x64.zip` (self-contained EXE at zip root, dependencies in `deps/`)
- **Android**: `artifacts/android/AstroTool*.apk`
- **iOS**: `artifacts/ios/` (app bundle)
- **macCatalyst**: `artifacts/maccatalyst/` (app bundle)

## Continuous Integration

### Build Triggers

Automated builds are triggered on:
- Every push to `main` branch
- Every pull request to `main` branch
- Manual release creation

### Workflow Files

- [`.github/workflows/build.yml`](.github/workflows/build.yml) - Main build and test pipeline
- [`.github/workflows/auto-release.yml`](.github/workflows/auto-release.yml) - Automatic release creation and version management

### Release Process

1. **PR Opened**: Creates a preview release (🔍 Preview: PR Title)
2. **PR Merged**: Creates a full release (✅ Release: PR Title) with all platform artifacts
3. **Version Bumped**: Minor version incremented automatically after release
4. **Main Branch Commit**: Creates a build release (📦 Build: YYYY-MM-DD) if not already covered by PR release

## Verifying Builds

### Windows
```bash
# Run the executable directly
./artifacts/windows/AstroTool.exe

# Windows dependencies are in
./artifacts/windows/deps

# Extract and verify
unzip artifacts/windows/astrotool-windows-x64.zip -d test-windows
./test-windows/AstroTool.exe
```

### Android
```bash
# Install on connected device/emulator
adb install artifacts/android/AstroTool.apk

# Launch
adb shell am start -n com.companyname.astrotool/.MainActivity
```

### iOS
```bash
# Requires Xcode and iOS simulator
open artifacts/ios/AstroTool.app
```

### macCatalyst
```bash
# Launch the app
open artifacts/maccatalyst/AstroTool.app
```

## Troubleshooting

### Build Failures

**"NETSDK1100: To build a project targeting Windows on this operating system..."**
- This is expected when building for Windows on non-Windows systems
- The workflow uses `-p:EnableWindowsTargeting=true` to enable cross-compilation
- Cross-compilation is supported and builds are successful

**"Assets file doesn't have a target for..."**
- Run `dotnet restore` to update the project assets
- Ensure the correct platform-specific properties are set

**"Workload not installed"**
- The CI/CD pipeline automatically installs required workloads
- For local builds, install manually: `dotnet workload install maui-windows` etc.

## Artifact Signatures

All release artifacts are signed by GitHub and available through the GitHub Release page with platform-specific naming:
- `AstroTool-*.apk` - Android
- `astrotool-windows-x64.zip` - Windows
- `astrotool-ios-simulator.zip` - iOS
- `astrotool-maccatalyst.zip` - macCatalyst

## Future Improvements

- [ ] Add arm64 Windows support (in addition to x64)
- [ ] Add Linux support via GTK or alternative framework
- [ ] Add macOS (Cocoa) native app support
- [ ] Implement app notarization for macOS
- [ ] Add CodeSign support for iOS/macCatalyst production builds
