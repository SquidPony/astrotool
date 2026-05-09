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

## Restore Dependencies

From the repository root:

```bash
dotnet restore
```

## Build

Build everything in the solution:

```bash
dotnet build AstroTool.slnx
```

Build only the core library (fast sanity check):

```bash
dotnet build AstroTool.Core/AstroTool.Core.csproj
```

Build MAUI app for a specific target framework:

```bash
dotnet build AstroTool/AstroTool.csproj -f net10.0-android
```

Build for Apple targets (macOS with full Xcode installed and selected):

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

## Optional: Launch the MAUI App

After selecting a valid target framework and having platform tooling installed:

```bash
dotnet run --project AstroTool/AstroTool.csproj -f net10.0-maccatalyst
```

On macOS, use `net10.0-ios` when running on an iOS simulator/device.