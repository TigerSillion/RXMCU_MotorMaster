# SKILL: motor-debug-gui

## Purpose
Execute end-to-end development workflow for `RXMCU_MotorMaster` (WPF + UART protocol GUI).

## Inputs
- COM port / baudrate
- target MCU firmware protocol version
- optional MAP file (`*.map`)

## Standard Workflow
1. Build check
- `dotnet build .\\MotorDebugStudio.csproj`

2. Transport check
- verify connect/disconnect path in `ConnectionViewModel`.
- keep HELLO/HEARTBEAT as internal health mechanism, not noisy UI telemetry.

3. Variable workflow check
- import MAP -> verify variable list and types.
- refresh addresses -> keep user annotations and auto-read flags.
- verify writable parameter write + readback.

4. Scope workflow check
- start/stop sampling.
- confirm plot refresh smoothness and no UI freeze.
- validate stream stats and state badges.

5. Layout and UX check
- left/center/right split behavior.
- splitter persistence across restart.
- log/fault tabs readable and non-blocking.

## Companion Skills
- `gui-design-system` for visual consistency.
- `wpf-performance` for render/data-path optimization.
- `ui-automation-playwright` for repeatable UI regression checks.
- `release-gate` before publish/push.

## Deliverables
- concise change summary
- regression risks
- updates to `README.md` and `docs/DEVELOPMENT_LOG.md` for behavior changes

## Guardrails
- no protocol drift without docs update.
- no thread-unsafe collection updates from background callbacks.
- no broken command bindings in `MainWindow.xaml`.
