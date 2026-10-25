# institutionaldiwan.client / src/app/styles — Global Styles, Theme Tokens & RTL Typography

> **Status:** Scaffolding & Rules — Styling and RTL rules defined; code pending.

## Purpose

Houses the Tailwind CSS v4 entry stylesheet, CSS variables for the institutional palette, and
Arabic typography. This is the single source of visual tokens for the SPA.

## Contents (planned)

| File | Purpose |
| --- | --- |
| `index.css` | `@import "tailwindcss";`, design tokens, RTL base rules (`html[dir="rtl"]`), print rules for official extracts. |

## Design tokens (contract)

Implement these CSS variables on `:root` (names may gain a `--diwan-` prefix, but semantics stay):

```css
:root {
  --color-bg: /* warm paper / soft institutional ground */;
  --color-surface: /* panels, tables */;
  --color-text: /* primary ink */;
  --color-text-muted: /* secondary */;
  --color-accent: /* deep teal / institutional accent — not purple */;
  --color-danger: /* validation / integrity failure */;
  --color-success: /* integrity pass / saved */;
  --color-border: /* hairline rules */;
  --font-sans: /* Cairo or Readex Pro + Noto Sans Arabic fallbacks */;
  --radius-sm: 0.25rem;
  --radius-md: 0.5rem;
}
```

Rules:

- Prefer logical spacing utilities; never hard-code `ml-`/`mr-` for layout that must flip.
- Hash / official-number / ISO-date text uses a monospace or `dir="ltr"` island.
- Avoid purple-on-white gradients, glow stacks, and pill-heavy chrome (see repo UI rules).
- `@media print`: hide shell chrome; force black text; keep numbers LTR.

## Implementation status

| Item | State |
| --- | --- |
| `index.css` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Cursor | Added concrete design-token contract, RTL/print rules, and anti-patterns for institutional UI. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/app/styles` segment. |
