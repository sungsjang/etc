# USD/KRW Windows Widget

A lightweight Windows desktop widget for monitoring the USD/KRW exchange rate using Naver Finance data.

## Features

- Compact borderless WinForms widget (about 300 × 205 px)
- Bright light-tone card-style UI
- Opens near the upper-right corner of the primary monitor
- Always-on-top by default
- Current USD/KRW rate from Naver Finance
- 5-minute automatic refresh
- Period change and percentage
- 1D / 1W / 1M / 6M / 1Y chart views
- Period high / low
- Up to roughly 400 days of lightweight local JSON cache
- Offline fallback to the most recently cached data
- Drag the widget to move it; position is remembered
- Last selected chart period is remembered
- System tray icon with show / refresh / exit
- Optional Start with Windows support
- No API key or account required
- No external chart package

## Recommended installation: build locally

To avoid browser warnings caused by downloading an unsigned executable, download only the source code and build the EXE on your own Windows PC.

1. On the GitHub repository page, choose **Code → Download ZIP**.
2. Extract the source ZIP.
3. Open the `usdkrw-widget` folder.
4. Double-click `build-local.bat`.
5. The script checks for the .NET 8 SDK and builds the application automatically.
6. When the build succeeds, the executable is created here:

```text
usdkrw-widget\dist\local\UsdKrwWidget.exe
```

The script then asks whether to run the widget immediately.

If the .NET 8 SDK is not installed, `build-local.bat` explains what is missing and opens Microsoft's official .NET 8 download page. The SDK is only needed to build the app; the generated EXE is self-contained.

## Controls

Period buttons:

- `1D`: intraday samples collected locally every 5 minutes
- `1W`: recent 7-day view
- `1M`: recent 1-month view
- `6M`: recent 6-month view
- `1Y`: recent 1-year view

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
- 1D: built from locally cached 5-minute samples collected while the app is running.
- 1W / 1M / 6M / 1Y: seeded from Naver daily USD/KRW history and supplemented by locally collected samples.
- The app requests enough paged daily history at startup to cover approximately one year.

On the first run of a day, the 1D graph starts with the data available at launch and becomes denser as the app continues running.

Cache location:

```text
%LOCALAPPDATA%\UsdKrwWidget\rates.json
```

Settings location:

```text
%LOCALAPPDATA%\UsdKrwWidget\settings.json
```

## Manual build from source

Requirements:

- Windows 10 or Windows 11
- .NET 8 SDK

```powershell
cd usdkrw-widget
dotnet restore
dotnet build -c Release
```

## Manual self-contained publish

```powershell
dotnet publish .\UsdKrwWidget.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Continuous integration

`.github/workflows/build-usdkrw-widget.yml` runs on `windows-latest`, restores, compiles, and verifies that a self-contained Windows executable can be published. Executable artifacts are intentionally not uploaded for distribution; end users should build locally with `build-local.bat`.

## Data provider

The provider is isolated in `Services/NaverFinanceClient.cs`.

Current quote endpoint:

```text
https://api.stock.naver.com/marketindex/exchange/FX_USDKRW
```

Daily history endpoint:

```text
https://m.stock.naver.com/front-api/marketIndex/prices?category=exchange&reutersCode=FX_USDKRW
```

These are public web endpoints used by Naver Finance rather than a documented developer API, so their response format may change in the future. The provider is intentionally isolated so parsing can be updated without redesigning the UI.
