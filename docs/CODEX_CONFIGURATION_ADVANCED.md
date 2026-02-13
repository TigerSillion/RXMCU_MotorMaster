# Codex Configuration for RXMCU_MotorMaster

This project now includes a professional MCP + Skill setup tailored for WPF GUI engineering.

## Config Targets
- `AGENTS.md`
- `.codex/config.toml`
- `.codex/mcp.json`
- `.codex/skills/motor-debug-gui/SKILL.md`
- `.codex/skills/gui-design-system/SKILL.md`
- `.codex/skills/wpf-performance/SKILL.md`
- `.codex/skills/ui-automation-playwright/SKILL.md`
- `.codex/skills/release-gate/SKILL.md`
- `docs/GUI_PROFESSIONAL_STACK.md`

## Usage
1. Open `E:\E2Sworkspace\202512\RXMCU_MotorMaster` as workspace root.
2. In Codex configuration UI:
   - Config file -> `.codex/config.toml`
   - Rules / Agents -> `AGENTS.md`
   - MCP file -> `.codex/mcp.json`
3. Keep skills enabled from `.codex/skills`.

## MCP Profiles
- `core`: filesystem + fetch + time
- `gui_advanced`: core + playwright + github

## Notes
- `npx` and `dotnet` are available locally.
- `uvx` is currently not installed; keep `git_local_uvx` disabled until available.
- For source-backed recommendations and tool choices, use `docs/GUI_PROFESSIONAL_STACK.md`.
