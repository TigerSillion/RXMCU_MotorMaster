# RXMCU_MotorMaster

UART-first motor tuning and diagnostics tool for RX26T targets.

## Current Protocol Target
- MCU protocol: `UART5 custom protocol v1.0`
- Frame: `55 AA + VER/TYPE/SEQ/CMD/LEN + PAYLOAD + CRC16`
- GUI status: implemented `HELLO/HEARTBEAT/MOTOR_CTRL/READ_TYPED/WRITE_TYPED/SCOPE_LAYOUT/SCOPE_CTRL + EVT_LOG/EVT_SCOPE`

## Build
```powershell
dotnet build .\MotorDebugStudio.csproj
```

## Run
```powershell
dotnet run --project .\MotorDebugStudio.csproj
```

## Quick Start
1. Select `COM6`, baud `115200`, click `Connect`.
2. Use `File -> Import Variables From MAP...` to load symbols.
3. Use `File -> Refresh Addresses` to refresh address/type for existing variables.
4. Start scope (`Run`) and observe waveform in center pane.
5. Read/write variables from left pane (`Read All`, `Read Selected`, `Write Selected`).

## UI/UX Rules (Current)
- Three-pane layout: left variables, center waveform, right controls.
- `GridSplitter` supports live resizing for left/center/right and bottom log panel.
- Layout is persisted to `%AppData%/MotorDebugStudio/layout.json` and restored on startup.
- HELLO/HEARTBEAT stays in transport layer for stability, but related noisy UI status is hidden.
- Theme toggle, auto-test entry, and session import/export entry are removed from UI.
- Right panel `CCRX MAP` supports quick browse+import and explicit `Import` / `Refresh Addresses` actions.
- Right panel supports adding custom variables (name/address/type/unit/writable/note) for ad-hoc diagnostics.

## Default bound parameter addresses (from RX26T map)
- `com_u1_system_mode` @ `0x00001801` (`u8`)
- `com_f4_ref_speed_rpm` @ `0x00001DC0` (`f32`)
- `com_f4_speed_rate_limit_rpm` @ `0x00001DC4` (`f32`)
- `com_f4_overspeed_limit_rpm` @ `0x00001DC8` (`f32`)
- `com_u1_ctrl_loop_mode` @ `0x00001807` (`u8`)
- `com_u1_sw_userif` @ `0x00001805` (`u8`)
- `g_u1_system_mode` @ `0x00001802` (`u8`, read-only)
- `com_u1_enable_write` @ `0x00001803` (`u8`, read-only)

## Docs
- `docs/PRD.md`
- `docs/ARCHITECTURE.md`
- `docs/PROTOCOL_UART_BIN_V1.md`
- `docs/DEVELOPMENT_LOG.md`
- `docs/CODEX_CONFIGURATION_ADVANCED.md`
- `docs/GUI_PROFESSIONAL_STACK.md`
