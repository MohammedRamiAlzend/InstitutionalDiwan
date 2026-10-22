# institutionaldiwan.client / src/shared/ui — RTL-First Design System & UI Kit Primitives
> **Status:** Scaffolding & Rules — UI kit rules defined; code pending.
## Purpose
Holds reusable, domain-agnostic UI primitives (`shadcn/ui` + Radix UI + Tailwind CSS v4) built for Arabic-first (`dir="rtl"`) and bilingual (`ltr`) institutional interfaces. Components here know nothing about documents, departments, or users.
## Contents
| File | Purpose |
| --- | --- |
| `button.tsx` | Accessible button with variants (`primary`, `secondary`, `outline`, `ghost`, `destructive`) and loading spinner state. |
| `input.tsx` / `textarea.tsx` | Form inputs supporting RTL text alignment, start/end icons (`ps-*` / `pe-*`), and validation error states. |
| `select.tsx` | Radix Select primitive with `dir="rtl"` support for lookup dropdowns. |
| `dialog.tsx` | Modal dialog primitive for create/edit/confirm workflows. |
| `table.tsx` | Base RTL table primitives (`Table`, `TableHeader`, `TableRow`, `TableHead`, `TableCell`). |
| `badge.tsx` | Status and classification badge primitive with semantic color variants. |
| `form-field.tsx` | Label + control + error message wrapper integrating cleanly with `react-hook-form`. |
| `index.ts` | Public barrel exporting all UI kit primitives. |
## Conventions / Notes
- Use logical CSS properties exclusively: Use `ms-*`, `me-*`, `ps-*`, `pe-*`, `start-*`, `end-*`, `text-start`, `text-end`, `rounded-s-*`, `rounded-e-*`—never `ml-*`, `mr-*`, `pl-*`, `pr-*`, `left-*`, `right-*`, or `text-left`/`text-right`.
- No domain imports: `shared/ui` may only import from `@/shared/lib` and external UI packages (`@radix-ui/*`, `lucide-react`, `class-variance-authority`).
## Implementation status
| Item | State |
| --- | --- |
| Core primitives (`button`, `input`, `select`, `dialog`, `table`, `badge`, `form-field`) | ❌ Not implemented |
## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown, so the `README.md`-per-folder rule in `AGENT.md` holds and the content is diffable and agent-editable. |
| 2026-10-19 | agent | Initialized `src/shared/ui` segment with RTL logical-property rules and planned shadcn/Radix primitives. |
