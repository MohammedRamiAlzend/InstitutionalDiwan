# `src/pages/persons`

> **Status:** Implemented and browser-verified. Plan: `plans/document-persons.md`.

## Purpose

The persons routes, behind `PERSON_VIEW`.

## Contents

| File | Purpose |
| --- | --- |
| `ui/PersonsPage.tsx` | `/persons`. |
| `ui/PersonDetailsPage.tsx` | `/persons/:personId` with the documents and timeline tabs in `?tab=`. |
| `index.ts` | Public barrel. |

## Implementation status

| Area | Status |
| --- | --- |
| Slice | ✅ Implemented |

## Change Log

- 2026-11-17: UI labels Phase 3 (`plans/ui-labels-edit-mode.md`): the page header title/description render from the catalog (`page.persons.title` / `.description`) via `PageHeader` `titleKey`/`descriptionKey`. The person-details title stays dynamic (the person's name).

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-09 | Claude | Created the slice. |
