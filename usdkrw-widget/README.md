# USD/KRW Windows Widget

A lightweight Windows desktop widget for monitoring the USD/KRW exchange rate using Naver Finance data.

## Features

- Small borderless WinForms widget
- Opens near the upper-right corner of the primary monitor
- Always-on-top by default
- Current USD/KRW rate from Naver Finance
- 5-minute automatic refresh
- Today's change and percentage
- 1-day / 1-week mini chart toggle
- Period high / low
- 30-day lightweight local JSON cache
- Offline fallback to the most recently cached data
- Drag the widget to move it; position is remembered
- System tray icon with show / refresh / exit
- Optional Start with Windows support
- No API key or account required
- No external chart package
- Self-contained single-file Windows build
- GitHub Actions Windows build/publish verification

## First run

1. Download the latest `UsdKrwWidget-win-x64` artifact from the GitHub Actions workflow.
2. Extract the ZIP.
3. Run `UsdKrwWidget.exe`.

No API key is required.

## Controls

Right-click the widget:

- Refresh now
- Hide widget
- Always on top
- Start with Windows
- Exit

Double-click the tray icon to show the widget again.

## Data behavior

The app uses Naver Finance public market-index endpoints for USD/KRW.

- Current rate: refreshed every 5 minutes from Naver Finance.
- 1-week view: seeded from Naver's recent daily USD/KRW data and supplemented by locally collected samples.
- 1-day view: built from locally cached samples collected every 5 minutes while the app is running. On the first run of a day, the intraday graph starts with the data available at launch and becomes denser as the app continues running.

This design avoids requiring a paid or registered API while still keeping the current quote fresh.

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

The provider is isolated in `Services/NaverFinanceClient.cs`.

Current quote endpoint:

```text
https://api.stock.naver.com/marketindex/exchange/FX_USDKRW
```

Recent daily history endpoint:

```text
https://m.stock.naver.com/front-api/marketIndex/prices?category=exchange&reutersCode=FX_USDKRW
```

These are public web endpoints used by Naver Finance rather than a documented developer API, so their response format may change in the future. The provider is intentionally isolated so parsing can be updated without redesigning the UI.

## Release status

The project is automatically compiled and packaged on a real Windows GitHub Actions runner after every relevant change. Runtime acceptance on the user's PC remains the final visual check for DPI, placement, and Naver network access.
