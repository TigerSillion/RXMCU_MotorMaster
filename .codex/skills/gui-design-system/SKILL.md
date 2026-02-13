# SKILL: gui-design-system

## Purpose
Keep UI professional, consistent, and readable under industrial debug workloads.

## Trigger
- Any XAML visual redesign.
- New panel, toolbar, table, or dialog.

## Steps
1. Define component intent
- classify as: navigation, data table, command action, status, or diagnostics.

2. Apply hierarchy rules
- command priority: primary actions near context, destructive actions isolated.
- dense data views default to legible spacing and clear headers.

3. Apply style rules
- use shared resources in `Themes/Styles.xaml` and theme dictionaries.
- avoid one-off inline colors unless required by status semantics.

4. Accessibility and operations
- keyboard focus order valid.
- controls have clear labels and no hidden critical path.

5. Verify
- check layout in 1280x760 and 1920x1080.
- ensure splitter resize and persistence still work.

## Deliverables
- updated XAML + theme resources
- concise UX rationale in PR/commit notes
