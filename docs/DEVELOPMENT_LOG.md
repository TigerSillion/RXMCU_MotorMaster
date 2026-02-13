# Development Log

## 2026-02-13 (GUI simplification + resizable persistent layout)
- Implemented medium-level UI simplification to keep only core workflow:
  - Removed UI entries for theme toggle.
  - Removed UI entries for auto test.
  - Removed UI entries for session save/load/export.
  - Kept only core panes: variable table (left), scope (center), controls/connection (right), logs/faults (bottom).
- HELLO/HEARTBEAT behavior adjusted:
  - Handshake and heartbeat remain active in transport flow.
  - Handshake capability text and heartbeat detail text are hidden from top bar.
  - HELLO/HEARTBEAT logs are downgraded to optional verbose link debug (disabled by default).
- Reworked main layout to VSCode-style split panes:
  - Horizontal splitters for left/center/right panes.
  - Vertical splitter for bottom log/fault panel height.
- Added layout persistence:
  - New `Models/LayoutState.cs`.
  - New `Services/LayoutStateService.cs`.
  - Persist path: `%AppData%/MotorDebugStudio/layout.json`.
  - Persisted fields: `LeftRatio`, `CenterRatio`, `RightRatio`, `BottomPanelHeight`.
  - Startup restore + closing save implemented in `MainWindow.xaml.cs`.
- MAP workflow alignment:
  - Variable import入口统一为 `File -> Import Variables From MAP...`.
  - Address refresh入口统一为 `File -> Refresh Addresses`.
  - Removed duplicate left-pane MAP import buttons.
- Switched default resource theme to Light (`App.xaml` now loads `Theme.Light.xaml`).

## 2026-02-13 (Protocol v1.0 integration)
- Reworked GUI transport to MCU UART5 protocol v1.0 (`55 AA` frame).
- Implemented request/response with sequence waiter in `UartTransport`.
- Implemented event routing:
  - `EVT_LOG_TEXT` -> system log
  - `EVT_SCOPE_DATA` -> waveform sample batches
- Added protocol models and codec helpers (`Hello/Heartbeat/ScopeLayout/ScopeEvent`).
- Upgraded `ITransportService` with protocol-level APIs:
  - hello/heartbeat/motorCtrl
  - scope layout/control
  - typed read/write
- Refactored ViewModels:
  - `ConnectionViewModel`: connect + HELLO + periodic heartbeat
  - `ScopeViewModel`: layout sync, start/stop control, stream stats
  - `ParamViewModel`: real address/type table + read/write/readback
- Updated default parameter binding with addresses from RX26T map.
- Updated main UI for protocol status, heartbeat, layout refresh, and runtime cards.
- Added UI-thread marshaling in `AppEventBus` to avoid cross-thread WPF collection errors.
- Build verification passed:
  - `dotnet build .\MotorDebugStudio.csproj` (0 errors)

## Known warning
- NuGet `NU1701` for `SkiaSharp.Views.WPF` compatibility warning from dependency chain.
  Current build/run still works under `net8.0-windows`.

## 2026-02-13 (Auto Test panel)
- Added `AutoTestViewModel` for one-click UART protocol validation.
- Added UI section in right panel: `Run UART Auto Test` + result/duration/scope-event counters.
- Test sequence: HELLO -> HEARTBEAT -> SCOPE_LAYOUT -> SCOPE_START/STOP + event count -> READ_TYPED -> WRITE_TYPED + readback -> MOTOR_CTRL(STOP).
