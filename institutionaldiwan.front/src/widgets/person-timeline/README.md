# `src/widgets/person-timeline`

> **Status:** Implemented and browser-verified. Plan: `plans/document-persons.md`.

## Purpose

Everything that happened to a person's documents.

## Contents

| File | Purpose |
| --- | --- |
| `ui/PersonTimeline.tsx` | Paged, newest-first events, each naming and linking its document. |
| `index.ts` | Public barrel. |

## Implementation status

| Area | Status |
| --- | --- |
| Slice | ✅ Implemented |
| UI labels | ✅ Phase 4 (persons): renders from `persons.timeline.*` — the event connectors (من/إلى/في/رُبط…/بواسطة), empty state and pagination aria via `t()`, the newer/older buttons via `<Label>`; «تغيير الحالة» reuses `persons.status-change`, retry reuses `common.action.retry`. Lookup `descAr` and document titles stay out. |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-18 | Qoder | UI labels Phase 4 (persons): the timeline renders from the `persons.timeline.*` catalog. No Arabic literals remain. |
| 2026-11-09 | Claude | Created the slice. |
