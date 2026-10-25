# institutionaldiwan.client / src/app/styles — Global Styles, Theme Tokens & RTL Typography

> **Status:** Scaffolding & Rules — Styling and RTL rules defined; code pending.

## Purpose

Houses the Tailwind CSS v4 entry stylesheet, CSS variables for the institutional palette, and
Arabic typography. This is the single source of visual tokens for the SPA.

## Contents (planned)

| File | Purpose |
| --- | --- |
| `tokens.css` | **The single source of truth for color**: the raw Forest / Golden Wheat / Charcoal palette, the semantic light and dark theme mappings, and the functional status colors. |
| `index.css` | `@import "tailwindcss";`, imports `tokens.css`, RTL/LTR base rules (`html[lang="ar"][dir="rtl"]`), Arabic typography, focus rings, and print rules for official extracts. |

## Design tokens (contract)

The palette has exactly three families. Components never use these raw values; they consume the
semantic variables below. Editing a value in `tokens.css` changes both themes everywhere.

### Raw brand palette

```css
:root {
  /* Forest */
  --forest-500: #428177;
  --forest-700: #054239;
  --forest-900: #002623;

  /* Golden Wheat */
  --wheat-100: #edebe0;
  --wheat-400: #b9a779;
  --wheat-600: #988561;

  /* Charcoal */
  --charcoal-0: #ffffff;
  --charcoal-500: #3d3a3b;
  --charcoal-900: #161616;
}
```

### Semantic light theme (default / `data-theme="light"`)

```css
:root,
[data-theme="light"] {
  color-scheme: light;

  --color-bg: var(--wheat-100);
  --color-surface: var(--charcoal-0);
  --color-surface-muted: color-mix(in srgb, var(--wheat-400) 22%, var(--charcoal-0));
  --color-text: var(--charcoal-900);
  --color-text-muted: var(--charcoal-500);
  --color-border: color-mix(in srgb, var(--wheat-600) 45%, var(--charcoal-0));

  --color-accent: var(--forest-700);
  --color-accent-hover: var(--forest-900);
  --color-accent-contrast: var(--wheat-100);
  --color-highlight: var(--forest-500);
  --color-gold: var(--wheat-600);
  --color-focus-ring: var(--forest-500);
}
```

### Semantic dark theme (`data-theme="dark"`)

```css
[data-theme="dark"] {
  color-scheme: dark;

  --color-bg: var(--charcoal-900);
  --color-surface: var(--charcoal-500);
  --color-surface-muted: var(--forest-900);
  --color-text: var(--wheat-100);
  --color-text-muted: var(--wheat-400);
  --color-border: var(--forest-700);

  --color-accent: var(--wheat-400);
  --color-accent-hover: var(--wheat-600);
  --color-accent-contrast: var(--charcoal-900);
  --color-highlight: var(--forest-500);
  --color-gold: var(--wheat-400);
  --color-focus-ring: var(--wheat-400);
}
```

Functional status tokens (`--color-danger`, `--color-success`, `--color-warning`) are declared in the
same `tokens.css`. They are not part of the nine-color brand palette, but they are the only source for
those states — never hardcode a status color in a component.

### Theme switching

- No `data-theme` attribute (or `data-theme="system"`) → follow `prefers-color-scheme`.
- `data-theme="light"` / `data-theme="dark"` → explicit user choice.
- The choice persists in `localStorage` under `diwan.theme`; a planned `ThemeProvider` in
  `src/app/providers` applies it, listens to `matchMedia` changes for `system`, and exposes the
  toggle used by `widgets/app-shell`.
- A tiny inline boot script in `index.html` applies the stored/system theme **before first paint** so
  the app never flashes the wrong background.
- `color-scheme` flips with the theme so native controls, scrollbars, and form widgets match.

Rules:

- **Only `tokens.css` contains hex colors.** Components use semantic variables (or Tailwind utility
  classes mapped to them); no hex in TSX, CSS modules, charts, or Mermaid output.
- Body text, table text, and menu items must meet WCAG AA in both themes; focus rings must be visible
  on every surface. Verify accent-on-surface pairs before shipping (for example `--color-accent` with
  `--color-accent-contrast`).
- Prefer logical spacing utilities; never hard-code `ml-`/`mr-` for layout that must flip.
- Hash / official-number / ISO-date text uses a monospace or `dir="ltr"` island.
- Avoid purple-on-white gradients, glow stacks, and pill-heavy chrome (see repo UI rules).
- `@media print`: hide shell chrome; force the light token set; keep numbers LTR.

## Implementation status

| Item | State |
| --- | --- |
| `tokens.css` (palette + light/dark semantic tokens) | ❌ Not implemented |
| `index.css` | ❌ Not implemented |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | Buffy | Locked the palette contract: raw Forest / Golden Wheat / Charcoal tokens, full light and dark semantic mappings, `data-theme` + `diwan.theme` switching, and `tokens.css` as the only place hex colors may appear. |
| 2026-10-25 | Cursor | Added concrete design-token contract, RTL/print rules, and anti-patterns for institutional UI. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/app/styles` segment. |
