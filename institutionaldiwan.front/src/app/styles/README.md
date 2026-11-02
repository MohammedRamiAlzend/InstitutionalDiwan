# institutionaldiwan.front / src/app/styles — Global Styles, Theme Tokens & RTL Typography

> **Status:** Implemented — `tokens.css` and the Tailwind v4 entry `index.css` are live; `/login` and the dashboard render in both themes.

## Purpose

Houses the Tailwind CSS v4 entry stylesheet, CSS variables for the institutional palette, and
Arabic typography. This is the single source of visual tokens for the SPA.

## Contents

| File | Purpose |
| --- | --- |
| `tokens.css` | **The single source of truth for color**: the raw Forest / Golden Wheat / Charcoal palette, the semantic light and dark theme mappings, and the functional status colors. |
| `guide-tour.css` | Dresses the Driver.js guide popover (`.diwan-tour`) in the semantic tokens: theme, RTL/LTR, logical close button, arrow colors. Imported by `index.css` after `driver.css`. |
| `index.css` | `@import "tailwindcss";`, imports `tokens.css`, maps the semantic variables to Tailwind utilities via `@theme inline` (e.g. `bg-background`, `text-foreground`, `bg-accent`, `text-accent-contrast`, `bg-overlay`), Arabic font stack (`Readex Pro` / `Cairo`), base focus rings, `dir`-safe base rules, shared overlay/popover/page entrance animations, the `html[data-density='compact']` scale, the `html[data-motion='off']` switch, and the print token override. |

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

  --color-overlay: color-mix(in srgb, var(--charcoal-900) 45%, transparent);
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

  --color-overlay: color-mix(in srgb, var(--charcoal-900) 70%, transparent);
}
```

Functional status tokens (`--color-danger`, `--color-success`, `--color-warning`) are declared in the
same `tokens.css`. They are not part of the nine-color brand palette, but they are the only source for
those states — never hardcode a status color in a component.

Modal overlays use `--color-overlay` (45% charcoal over light surfaces, 70% in dark) so dialogs never
hardcode a translucent black.

### Motion

`index.css` declares the shared entrance/exit animations in `@theme`:

| Utility | Used by |
| --- | --- |
| `animate-overlay-in` / `animate-overlay-out` | Dialog / modal backdrop fade (150 ms in, 120 ms out). |
| `animate-content-in` / `animate-content-out` | Dialog, dropdown, context menu, and select surface (180 ms in, 140 ms out). |
| `animate-page-in` | The shell page container and the department view body, each re-keyed on what changed (route path / selected view) — 220 ms fade-up. |

These are the only motion primitives shared components may assume; feature code does not define its
own keyframes. Exits are attached through Radix's state attribute —
`animate-content-in data-[state=closed]:animate-content-out` — because `Presence` keeps the node
mounted until the closing animation ends, and the variant selector
(`.data-\[state\=closed\]\:animate-content-out[data-state=closed]`, specificity 0-2-0) outranks the
plain entrance utility (0-1-0).

The user can stop that motion from «إعدادات العرض» (see `widgets/app-shell`). The shell writes
`html[data-motion]` (`on` / `off`, persisted as `diwan.motion`, seeded from
`prefers-reduced-motion` by the `index.html` boot script), and two unlayered rules turn it off:

```css
html[data-motion='off'] .animate-overlay-in,
html[data-motion='off'] .animate-content-in,
html[data-motion='off'] .animate-page-in,
html[data-motion='off'] [data-state] { animation: none; }
html[data-motion='off'] * { scroll-behavior: auto; transition-duration: 0s; }
```

They sit outside `@layer base` on purpose: unlayered declarations beat the generated `animate-*`
utilities, which a `@layer base` rule could never override. The `[data-state]` arm is needed because
a variant-prefixed class token (`data-[state=closed]:animate-content-out`) is not matched by the
plain `.animate-content-out` selector. With `animation: none` the computed duration is `0s`, so
Radix `Presence` removes the popup immediately instead of waiting for an `animationend` that never
comes. Loading spinners (`animate-spin`) keep turning — they report work in progress, not
decoration, so a motion-free user still sees that a request is running.

### Density

The shell toggles a single density scale on the document element:

```css
html[data-density='comfortable'] { /* default 16px root */ }
html[data-density='compact'] { font-size: 15px; }
```

All spacing uses `rem`-based Tailwind utilities, so flipping the attribute tightens the whole shell
and every table without per-component classes. The preference persists as `diwan.density`; the app
shell removes the attribute when it unmounts so public pages (`/login`) stay at the default scale.

### Theme switching

- No `data-theme` attribute (or `data-theme="system"`) → follow `prefers-color-scheme`.
- `data-theme="light"` / `data-theme="dark"` → explicit user choice.
- The choice persists in `localStorage` under `diwan.theme`; the inline boot script in `index.html`
  applies it before first paint, and `ThemeProvider` in `src/app/providers` re-applies it after
  mount. The toggle consumed by the login page / app shell is `shared/ui/theme-toggle`.
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
| `tokens.css` (palette + light/dark semantic tokens) | ✅ Implemented |
| `guide-tour.css` | ✅ Implemented |
| `index.css` | ✅ Implemented (Tailwind v4 entry + `@theme inline` mapping + overlay/content/page entrance animations + compact density scale + `data-motion` switch) |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-02 | agent | Added `guide-tour.css` and imported Driver.js styles; the `data-motion` switch no longer touches `.driver-popover`, so the guide tour keeps its animation. |
| 2026-11-01 | agent | Added `animate-overlay-out` / `animate-content-out` keyframes and utilities for popup closes, extended the `data-motion='off'` kill rule with `[data-state]` so variant-prefixed exit classes are covered, and documented the specificity/Presence contract. |
| 2026-11-01 | agent | Added `animate-page-in` for the shell page container and the unlayered `html[data-motion='off']` rules that stop entrance animations and colour transitions while leaving loading spinners running. |
| 2026-10-27 | agent | Added the `html[data-density='compact']` root-scale rule backing the shell's `diwan.density` preference. |
| 2026-10-27 | agent | Added the `--color-overlay` semantic token (light/dark) and shared overlay/content entrance animations used by dialogs, menus, and selects. |
| 2026-10-27 | agent | Implemented `tokens.css` exactly per the locked contract and the Tailwind v4 entry `index.css` with semantic-utility mapping, Arabic fonts, focus rings, and print token override. |
| 2026-10-25 | Buffy | Locked the palette contract: raw Forest / Golden Wheat / Charcoal tokens, full light and dark semantic mappings, `data-theme` + `diwan.theme` switching, and `tokens.css` as the only place hex colors may appear. |
| 2026-10-25 | Cursor | Added concrete design-token contract, RTL/print rules, and anti-patterns for institutional UI. |
| 2026-10-22 | opencode | Converted this folder's `README.md` from the Word document it was authored in to markdown. |
| 2026-10-19 | agent | Initialized `src/app/styles` segment. |
