# AstroTool

AstroTool is a .NET MAUI + Blazor app with a shared core library and xUnit tests.

## Local Development Setup

### Prerequisites

Install the following tools before building locally:

1. .NET 10 SDK, pinned by the repo to 10.0.203
2. .NET MAUI workloads
3. Platform toolchains you want to target:
	- Android: Android SDK (via Visual Studio or Android Studio)
	- iOS/MacCatalyst (macOS only): Xcode + command line tools
	- Windows (on Windows): Windows 10/11 SDK

Check your SDK version:

```bash
dotnet --version
```

Install MAUI workloads:

```bash
dotnet workload install maui
```

If workloads are already installed but builds fail after SDK updates, run:

```bash
dotnet workload restore
```

On macOS, the repo enables Apple targets automatically when Xcode is available, so the default solution build includes MacCatalyst and iOS support.

## Build From CLI (Recommended)

Use the repo scripts to run restore, build, tests, and publish artifacts with one command.

On Linux/macOS:

```bash
./scripts/build.sh
```

On Windows PowerShell:

```powershell
./scripts/build.ps1
```

By default, scripts:

1. Restore dependencies
2. Build `AstroTool.Core`
3. Run `AstroTool.Core.Tests`
4. Publish platform outputs into `./artifacts`

### Platform Selection

Build selected platforms only:

```bash
./scripts/build.sh --platform windows --platform android
```

```powershell
./scripts/build.ps1 -Platform windows,android
```

Available platforms:

- `windows`
- `android`
- `ios` (macOS + Xcode required)
- `maccatalyst` (macOS + Xcode required)

The scripts automatically skip unavailable platform targets unless `--strict` (or `-Strict`) is used.

### Build Outputs

Published app artifacts are written to:

- `artifacts/windows`
- `artifacts/android`
- `artifacts/ios`
- `artifacts/maccatalyst`

Each run also writes `artifacts/BUILD_SUMMARY.txt`.

### Useful Options

Linux/macOS:

```bash
./scripts/build.sh --skip-tests --configuration Debug --output ./out --windows-rid win-x64
```

Windows PowerShell:

```powershell
./scripts/build.ps1 -SkipTests -Configuration Debug -Output ./out -WindowsRid win-x64
```

Use `--help` or `Get-Help ./scripts/build.ps1 -Detailed` for full details.

## Run Locally From CLI (Recommended)

Use the repo run scripts to launch the app locally with a host-aware default target.

On Linux/macOS:

```bash
./scripts/run.sh
```

On Windows PowerShell:

```powershell
./scripts/run.ps1
```

By default (`auto` platform):

1. macOS runs `maccatalyst`
2. Windows runs `windows`
3. Linux runs `android` when Android SDK is available

### Choose a Specific Run Target

Linux/macOS:

```bash
./scripts/run.sh --platform android
./scripts/run.sh --platform ios
./scripts/run.sh --platform maccatalyst --configuration Release
```

Windows PowerShell:

```powershell
./scripts/run.ps1 -Platform windows
./scripts/run.ps1 -Platform android
./scripts/run.ps1 -Platform maccatalyst -Configuration Release
```

Available run platforms:

- `auto`
- `windows` (Windows host or Linux WSL)
- `android` (requires Android SDK)
- `ios` (macOS + Xcode required)
- `maccatalyst` (macOS + Xcode required)

From Linux WSL, `./scripts/run.sh --platform windows` automatically forwards to Windows PowerShell and launches the Windows target.

Useful option:

```bash
./scripts/run.sh --no-restore
```

```powershell
./scripts/run.ps1 -NoRestore
```

## Manual Build Commands (Optional)

If you want to run dotnet commands directly instead of using the scripts:

```bash
dotnet restore
dotnet build AstroTool.slnx
```

Build MAUI app for a specific target framework:

```bash
dotnet build AstroTool/AstroTool.csproj -f net10.0-android
```

Build Apple targets (macOS with full Xcode installed and selected):

```bash
dotnet build AstroTool/AstroTool.csproj -f net10.0-maccatalyst
```

Build for iOS (requires iOS simulator runtimes installed in Xcode):

```bash
dotnet build AstroTool/AstroTool.csproj -f net10.0-ios -p:EnableIosTarget=true
```

## Run Tests

Run all tests:

```bash
dotnet test AstroTool.Core.Tests/AstroTool.Core.Tests.csproj
```

Or run tests from solution level:

```bash
dotnet test AstroTool.slnx
```

## Manual Run Command (Optional)

If you want to run dotnet directly instead of using scripts:

```bash
dotnet run --project AstroTool/AstroTool.csproj -f net10.0-maccatalyst -p:EnableAppleTargets=true
```

For iOS on macOS, enable the iOS target explicitly:

```bash
dotnet run --project AstroTool/AstroTool.csproj -f net10.0-ios -p:EnableAppleTargets=true -p:EnableIosTarget=true
```
