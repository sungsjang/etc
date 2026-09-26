# Development Plan — USD/KRW Windows Widget

## Goal

Build a low-memory Windows desktop widget that stays near the upper-right corner and shows:

- Current USD/KRW exchange rate
- Today's numeric and percentage change
- Today's intraday trend
- Recent 1-week trend
- 5-minute refresh
- Last update status

## Design priorities

1. Low memory and near-zero CPU while idle
2. Simple, glanceable UI
3. Resilience when the network/API is unavailable
4. No API secrets in Git
5. Data-provider abstraction can be added later
6. Minimal dependencies

## Technical stack

- C#
- .NET 8
- WinForms
- System.Drawing custom sparkline
- HttpClient
- Local JSON cache for MVP
- Twelve Data REST API

## Phase 1 — MVP

Status: in progress / initial implementation committed.

Implemented:

- Borderless compact window
- Top-most behavior
- Upper-right initial position
- Drag repositioning
- Current USD/KRW price request
- 5-minute update timer
- Recent 5-minute historical data request
- Today / 1-week mode
- High / low calculation
- Percentage change calculation
- Custom lightweight chart
- Local 30-day JSON cache
- Offline indicator
- Manual refresh context menu

## Phase 2 — Windows integration

Planned:

- Remember last window position
- Start with Windows checkbox
- Persist user settings
- Optional system tray icon
- Hide/show widget command
- Multiple-monitor position handling

## Phase 3 — Reliability

Planned:

- Exponential retry/backoff
- Better API error categorization
- Rate-limit handling
- Data-gap detection
- Secondary provider fallback
- Cache integrity recovery

## Phase 4 — UI refinement

Planned:

- Hover timestamp/rate tooltip
- Better compact typography
- Optional opacity
- Light/dark theme
- Korean vs global rise/fall color convention
- Optional border/shadow
- User-selectable refresh interval

## Phase 5 — Alerts

Planned:

- Notify above/below selected USD/KRW threshold
- Daily high/low alert
- Rapid movement alert
- Windows toast notification

## Phase 6 — Distribution

Planned:

- Release build
- Single EXE publish
- Optional self-contained version
- GitHub Releases packaging
- Version display
- Basic update-check mechanism

## Data flow

```text
App starts
  -> load local cache
  -> request recent 5-minute history
  -> request current USD/KRW
  -> render widget
  -> wait 5 minutes
  -> request current USD/KRW
  -> append to local cache
  -> redraw selected 1D/1W view
```

## Security

The Twelve Data API key must not be committed to the repository.

Current MVP reads:

```text
TWELVE_DATA_API_KEY
```

from the Windows user/process environment.

## Definition of MVP complete

The MVP is considered complete when:

- It builds cleanly on Windows 10/11 with .NET 8
- It launches as a compact widget
- Current USD/KRW appears correctly
- Refresh occurs every 5 minutes
- 1D and 1W graphs render correctly
- Cached data appears when temporarily offline
- Memory and CPU use remain suitable for all-day operation
