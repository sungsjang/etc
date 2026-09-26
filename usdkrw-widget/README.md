# USD/KRW Windows Widget

A lightweight Windows desktop widget for monitoring the USD/KRW exchange rate.

## Features

- Small borderless WinForms widget
- Opens near the upper-right corner of the primary monitor
- Always-on-top by default
- Current USD/KRW rate
- 5-minute automatic refresh
- Today's change and percentage
- 1-day / 1-week mini chart toggle
- Period high / low
- 30-day lightweight local JSON cache
- Offline fallback to the most recently cached data
- Drag the widget to move it; position is remembered
- System tray icon with show / refresh / exit
- Optional Start with Windows support
- In-app Twelve Data API-key setup
- No external chart package
- Self-contained single-file Windows build
- GitHub Actions Windows build/publish verification

## First run

1. Download the latest `UsdKrwWidget-win-x64` artifact from the GitHub Actions workflow.
2. Extract the ZIP.
3. Run `UsdKrwWidget.exe`.
4. On first launch, enter your Twelve Data API key in the settings dialog.

The API key is stored in your Windows user environment as `TWELVE_DATA_API_KEY`. It is never committed to GitHub.

You can change the key later by right-clicking the widget or tray icon and choosing **API settings...**.

## Controls

Right-click the widget:

- Refresh now
- API settings...
- Hide widget
- Always on top
- Start with Windows
- Exit

Double-click the tray icon to show the widget again.

## Data behavior

At startup the application requests recent 5-minute USD/KRW history. After startup it requests the current USD/KRW price every 5 minutes and appends it to the local cache.

Cache location:

```text
%LOCALAPPDATA%\UsdKrwWidget\rates.json
```

Settings location:

```text
%LOCALAPPDATA%\UsdKrwWidget\settings.json
```

Only the most recent 30 days of rate data are retained locally.

## Build from source

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK

```powershell
cd usdkrw-widget
dotnet restore
dotnet build -c Release
```

## Publish a single EXE

Use the included script:

```powershell
.\publish.ps1
```

Or run:

```powershell
dotnet publish .\UsdKrwWidget.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

The self-contained build does not require users to install the .NET runtime separately.

## Continuous build

`.github/workflows/build-usdkrw-widget.yml` builds the application on `windows-latest`, publishes a self-contained single-file executable, and uploads it as the `UsdKrwWidget-win-x64` artifact after changes to the widget project.

## Data provider

The current provider is Twelve Data using the `USD/KRW` pair and 5-minute history. The provider code is isolated in `Services/TwelveDataClient.cs` so another provider can be added later without redesigning the UI.

## Current release status

The Windows CI pipeline has successfully completed restore, build, single-file publish, and artifact upload. The remaining acceptance step is normal end-user runtime verification on a physical Windows desktop with a valid Twelve Data API key and live network access.
