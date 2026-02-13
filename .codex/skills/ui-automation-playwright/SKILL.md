# SKILL: ui-automation-playwright

## Purpose
Use browser/automation style interaction patterns to script repeatable GUI behavior checks.

## Trigger
- Regression test for connect/read/write/scope flow.
- Verification after major UI layout refactor.

## MCP Dependency
- enable `playwright_mcp` in `.codex/mcp.json`.

## Steps
1. Define critical path
- connect serial
- import MAP
- read variable
- write variable
- start/stop scope

2. Automate checks
- verify key control states (enabled/disabled)
- verify status transitions (Disconnected -> Connected -> Streaming)
- verify expected logs/fault entries are visible

3. Snapshot verification
- capture screenshots for before/after diff on key screens.

## Deliverables
- scripted test notes
- pass/fail list for core interaction path
