# AGENTS.md (RXMCU_MotorMaster)

## Scope
- This repository is for PC GUI development (`MotorDebugStudio`) that integrates with RX26T UART protocol.
- Primary goal: stable, testable, maintainable GUI for waveform, variable read/write, command control, and diagnostics.

## Non-Negotiable Rules
- Any protocol-facing change must keep backward compatibility or provide explicit migration notes.
- Any UI behavior change must keep core flow intact:
  - connect -> import map -> read/write -> scope run/stop -> fault/log view.
- No silent feature removal from UI without update in `README.md` and `docs/DEVELOPMENT_LOG.md`.
- No direct edits of generated build outputs (`bin/`, `obj/`).

## UI/UX Engineering Standards
- Layout must be resizable and persistent.
- Controls must remain keyboard-accessible and avoid hidden critical actions.
- Keep terminology consistent with protocol docs and MCU variable names.
- Avoid decorative-only UI features that hurt debug efficiency.

## Protocol & Data Standards
- Contract source:
  - `docs/PROTOCOL_UART_BIN_V1.md`
  - MCU-side requirement docs maintained in firmware repository.
- For read/write variables:
  - preserve type safety (`U8/S8/U16/S16/U32/S32/F32`).
  - preserve clear write-protection behavior and readback verification.
- HELLO/HEARTBEAT stays as transport health mechanism; UI noise should be minimized.

## Quality Gates
- Build must pass: `dotnet build .\MotorDebugStudio.csproj`.
- Before merge, verify:
  - COM connect/disconnect
  - MAP import + address refresh
  - parameter read/write/readback
  - scope start/stop + plot refresh
  - fault/log panel behavior
- Document regressions and mitigations in `docs/DEVELOPMENT_LOG.md`.

## MCP & Skills Policy
- MCP is optional support; local build/test workflow is mandatory baseline.
- Skills under `.codex/skills/` should be task-oriented and executable.
- If MCP unavailable, continue with local scripts/commands and log the fallback.
