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
- Manual refresh from the right-click menu or tray menu
- Drag the widget to move it
- Remembers widget position and selected chart period
- Optional **Start with Windows** setting
- System tray icon; double-click it to show the widget
- Closing the window hides it to the tray; use **Exit** to terminate
- No external chart package
- GitHub Actions Windows build verification
- Self-contained single-EXE publish configuration

## Requirements

- Windows 10 or Windows 11, x64
- Twelve Data API key

The release build is self-contained, so the target PC does not need a separate .NET installation.

## API key

The API key is intentionally not stored in source code or GitHub.

Set it in Windows PowerShell:

```powershell
[Environment]::SetEnvironmentVariable("TWELVE_DATA_API_KEY", "YOUR_KEY_HERE", "User")
```

Then restart the widget. If necessary, sign out/in once so newly launched applications see the user environment variable.

For a temporary PowerShell session only:

```powershell
$env:TWELVE_DATA_API_KEY="YOUR_KEY_HERE"
```

## Run from source

Install the .NET 8 SDK and run:

```powershell
cd usdkrw-widget
dotnet run
```

## Build a single EXE

Use the included script:

```powershell
cd usdkrw-widget
.\publish.ps1
```

Output:

```text
usdkrw-widget\dist\win-x64\UsdKrwWidget.exe
```

Equivalent command:

```powershell
dotnet publish UsdKrwWidget.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishReadyToRun=true -o dist\win-x64
```

## GitHub Actions

`.github/workflows/build-usdkrw-widget.yml` builds the project on `windows-latest` whenever relevant files are pushed to `main`.

The workflow also publishes and uploads this artifact:

```text
UsdKrwWidget-win-x64 / UsdKrwWidget.exe
```

This provides a real Windows CI compile check even when development is performed through GitHub rather than on a local Windows development machine.

## Data behavior

At startup the application requests recent 5-minute USD/KRW history. After startup it requests the current USD/KRW price every 5 minutes and appends it to the local cache.

Rate cache:

```text
%LOCALAPPDATA%\UsdKrwWidget\rates.json
```

Settings:

```text
%LOCALAPPDATA%\UsdKrwWidget\settings.json
```

Only the most recent 30 days of rate data are retained locally.

## Windows startup

Right-click the widget and enable:

```text
Start with Windows
```

The app uses the current user's standard Windows `Run` registry entry, so administrator privileges are not required.

## Still optional for later versions

- Refresh-interval selector
- Better tooltip / hover values on chart
- Exchange-rate threshold alerts
- Optional alternate data provider fallback
- Custom app/tray icon
- Signed installer or automatic updater
