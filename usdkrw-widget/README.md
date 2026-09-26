# USD/KRW Windows Widget

A lightweight Windows desktop widget for monitoring the USD/KRW exchange rate.

## Current MVP

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
- Right-click menu with manual refresh and exit
- Drag the widget to move it
- No external chart package

## Requirements

- Windows 10 or Windows 11
- .NET 8 Desktop Runtime, or publish as self-contained
- Twelve Data API key

## API key

The API key is intentionally not stored in source code.

Set it in Windows PowerShell:

```powershell
[Environment]::SetEnvironmentVariable("TWELVE_DATA_API_KEY", "YOUR_KEY_HERE", "User")
```

Then sign out/in or restart the terminal / app before launching the widget.

For a temporary PowerShell session only:

```powershell
$env:TWELVE_DATA_API_KEY="YOUR_KEY_HERE"
```

## Run from source

```powershell
cd usdkrw-widget
dotnet run
```

## Publish

Framework-dependent single file:

```powershell
dotnet publish -c Release -r win-x64 --self-contained false
```

Self-contained Windows build:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

## Data behavior

At startup the application requests recent 5-minute USD/KRW history. After startup it requests the current USD/KRW price every 5 minutes and appends it to the local cache.

Cache location:

```text
%LOCALAPPDATA%\UsdKrwWidget\rates.json
```

Only the most recent 30 days are retained locally.

## Planned next features

- Remember widget position
- Start with Windows option
- Settings dialog
- Refresh-interval selector
- Better tooltip / hover values on chart
- Exchange-rate threshold alerts
- Optional alternate data provider fallback
