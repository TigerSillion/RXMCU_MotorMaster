# GUI Professional MCP + Skill Stack (RXMCU_MotorMaster)

## Objective
Build a professional, high-performance, maintainable GUI development workflow for WPF + UART protocol integration.

## Local Environment Check
- `npx`: available (`10.9.2`)
- `dotnet`: available (`10.0.103`)
- `uvx`: not installed (optional servers depending on uvx are disabled)

## MCP Stack (Recommended)

### Tier 1: Core (enable first)
1. `filesystem_gui_repo`
- Purpose: safe file/context access for this repository.
- Why: baseline productivity and deterministic workspace operations.

2. `fetch`
- Purpose: source-backed docs retrieval.
- Why: keeps protocol/UI decisions grounded in official docs.

3. `time`
- Purpose: deterministic time-zone/time conversion utilities.
- Why: useful for log timestamp debugging and test consistency.

### Tier 2: Extended (enable when needed)
1. `playwright_mcp`
- Purpose: automated interaction and screenshot-based UI regression checks.
- Why: protects UX during rapid UI iteration.

2. `github`
- Purpose: repository/issue/PR context integration.
- Why: release-quality change tracking and collaboration.

3. `memory`
- Purpose: multi-session context retention.
- Why: useful for long-running debugging programs.

### Tier 3: Optional
1. `git_local_uvx`
- Purpose: local git-aware MCP actions.
- Why: helpful but requires uvx setup.

## Skill Pack (Professional)
1. `motor-debug-gui`
- End-to-end domain workflow (connect/map/read-write/scope/layout).

2. `gui-design-system`
- Visual hierarchy, control consistency, operational usability.

3. `wpf-performance`
- Render/data path guardrails, tracing/counters guidance.

4. `ui-automation-playwright`
- Repeatable UI regression path with automation mindset.

5. `release-gate`
- Pre-release checklist for build, function, docs, rollback.

## Activation Order
1. Enable MCP profile `core` in `.codex/mcp.json`.
2. Use `motor-debug-gui` + `gui-design-system` as default development path.
3. Enable `playwright_mcp` before large UI refactors.
4. Apply `release-gate` before push/tag.

## Authoritative Sources
- MCP official spec and docs: https://modelcontextprotocol.io/specification/2025-06-18
- MCP official servers repository: https://github.com/modelcontextprotocol/servers
- OpenAI MCP guide: https://platform.openai.com/docs/guides/tools-remote-mcp
- OpenAI Codex repository: https://github.com/openai/codex
- OpenAI Skills repository: https://github.com/openai/skills
- Playwright MCP repository: https://github.com/microsoft/playwright-mcp
- Microsoft WPF performance guidance: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-wpf-application-performance
- Microsoft WPF controls performance guidance: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/optimizing-performance-controls
- .NET diagnostic tools (`dotnet-trace`): https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-trace
- .NET counters (`dotnet-counters`): https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters
- Fluent 2 design guidance: https://fluent2.microsoft.design/
