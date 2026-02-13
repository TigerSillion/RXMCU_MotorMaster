# SKILL: wpf-performance

## Purpose
Prevent UI lag, dropped frames, and data-path stalls in real-time waveform + variable dashboards.

## Trigger
- Changes in plot rendering, dispatcher timers, large tables, or high-frequency bindings.

## Baseline Commands
- `dotnet build .\\MotorDebugStudio.csproj`
- `dotnet-trace --help`
- `dotnet-counters --help`

## Steps
1. Rendering path review
- avoid unnecessary full redraws.
- guard refresh by data version / dirty flags.

2. DataGrid performance
- keep virtualization enabled for large parameter sets.
- avoid expensive converters in high-frequency columns.

3. Threading model
- background transport work stays off UI thread.
- UI collection updates marshaled safely to dispatcher.

4. Instrumentation
- use `dotnet-counters` for runtime trends.
- use `dotnet-trace` for hotspots when regressions appear.

5. Exit criteria
- no visible freeze during scope run.
- command latency remains acceptable while plotting.

## Deliverables
- performance note in `docs/DEVELOPMENT_LOG.md`
- explicit risk callout if trade-offs remain
