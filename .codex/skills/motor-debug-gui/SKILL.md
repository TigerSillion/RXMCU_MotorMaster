# SKILL: motor-debug-gui

## Purpose
Execute reliable development workflow for `RXMCU_MotorMaster` (WPF + UART protocol GUI).

## Inputs
- COM port / baudrate
- target MCU firmware protocol version
- optional MAP file (`*.map`)

## Standard Workflow
1. Build check
- `dotnet build .\MotorDebugStudio.csproj`

2. Transport check
- Verify connect/disconnect path from `ConnectionViewModel`.
- Ensure HELLO/HEARTBEAT remains internal health mechanism (low UI noise).

3. Variable workflow check
- Import MAP -> verify variable list and types.
- Refresh addresses -> keep user annotations and auto-read states.
- Verify read/write/readback behavior for writable parameters.

4. Scope workflow check
- Start/stop sampling.
- Confirm plot update performance and no UI freeze.
- Validate stream stats shown in top/plot status.

5. Layout and UX check
- Left-center-right split behavior.
- GridSplitter resize persistence across restart.
- Log/fault tabs readable and non-blocking.

## Deliverables
- concise change summary
- regression risks
- updated `README.md` and `docs/DEVELOPMENT_LOG.md` when behavior changes

## Guardrails
- No protocol drift without doc update.
- No thread-unsafe collection updates from background transport callbacks.
- No breaking command bindings in `MainWindow.xaml`.
