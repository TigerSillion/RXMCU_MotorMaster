# Codex Configuration for RXMCU_MotorMaster

This project-level advanced configuration is now scoped specifically to GUI development.

## Config Targets
- `AGENTS.md`
- `.codex/config.toml`
- `.codex/mcp.json`
- `.codex/skills/motor-debug-gui/SKILL.md`

## Usage
1. Open `E:\E2Sworkspace\202512\RXMCU_MotorMaster` as workspace root.
2. In Codex configuration UI:
   - Config file -> `.codex/config.toml`
   - Rules / Agents -> `AGENTS.md`
   - MCP file -> `.codex/mcp.json`
3. Keep skills enabled from `.codex/skills`.

## Notes
- MCP filesystem server is enabled by default and points to this GUI repo.
- Git MCP server is prepared but disabled until dependencies are installed.
- All rules are tailored for WPF GUI + UART protocol integration workflow.
