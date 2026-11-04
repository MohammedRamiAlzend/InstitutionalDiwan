# examples / workflow — Interactive document-lifecycle explainer

> **Status:** Implemented (standalone HTML + CSS + JavaScript; no build step, no dependencies, no server calls)

## Purpose
An Arabic-first (RTL, light + dark) page that explains how Institutional Diwan works and lets a reader
play a document through its whole lifecycle: draft, registration with an official number, routing
between departments (send, department registration, receive, complete/reject/return/redirect, close),
attachments, archiving, notifications, and the tamper-evident audit chain. It is an illustration with
an in-memory simulation; the authoritative rules and endpoints remain in
`institutionaldiwan.front/knowledge.md`.

Open it by double-clicking `index.html`, or serve the folder with any static server
(for example `npx serve examples/workflow`).

## Contents
| File | Purpose |
| --- | --- |
| `index.html` | Page structure: overview of the layers, the lifecycle simulator shell, the module cards and the rules section; applies the saved theme before first paint. |
| `workflow.css` | Styling with semantic color tokens for light and dark themes, logical (RTL-safe) properties only, and the stepper, custody map, timeline and table components. |
| `workflow.js` | The simulation: roles and permission/scope gating, the table of lifecycle actions, the hash-chained audit log with integrity check and tamper demo, notifications, the autoplay scenario, and rendering. |

## Conventions / Notes
- Roles, permissions (17 codes), access levels, notification types and error codes follow `knowledge.md`; the
  `Movements.ReceiverRegistrationRequired` code is illustrative, `Correspondence.DocumentArchived` is real.
- The hash chain uses a small FNV-style function for readability; the real chain uses SHA-256.
- Keep the page free of external requests and frameworks so it works offline.

## Implementation status
| Item | State |
| --- | --- |
| Lifecycle simulator (document + movement steppers, custody map, facts) | ✅ |
| Role switching with permission and department-scope gating | ✅ |
| Audit chain, integrity check, tamper demo | ✅ |
| Notifications and attachments panels | ✅ |
| Autoplay scenario | ✅ |
| Relations and dynamic fields in the simulation | ❌ (described in the module cards only) |

## Change Log
| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | Added the interactive document-lifecycle workflow example. |
