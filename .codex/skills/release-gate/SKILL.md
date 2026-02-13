# SKILL: release-gate

## Purpose
Enforce production-grade release checklist for GUI deliverables.

## Trigger
- preparing backup tag, release branch, or handoff build.

## Checklist
1. Build and run
- `dotnet build .\\MotorDebugStudio.csproj` succeeds.
- GUI starts with no fatal startup errors.

2. Functional validation
- serial connect/disconnect verified.
- map import and address refresh verified.
- read/write/readback verified.
- scope streaming verified.

3. Safety and docs
- protocol-impacting changes reflected in docs.
- user-facing behavior changes updated in README.
- known issues recorded with workaround.

4. Source control
- no temporary debug code left behind.
- release commit message includes scope and risk.

## Deliverables
- release note summary
- rollback note (previous known-good commit/tag)
