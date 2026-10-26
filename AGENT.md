# AGENT.md — Repository Rules for AI Agents & Contributors

> **Audience:** Any AI agent (Cline, Copilot, Cursor, …) or human contributor working in this
> repository. Read this file **before** making any change.

## Golden rule: code and docs change together

**Whenever a folder's contents change, that folder's `README.md` must be updated in the same
commit/changeset.** A change is "done" only when both the code and its README are updated.

A folder's `README.md` must always let a reader — especially an agent with no prior context —
answer three questions **without opening the code**:

1. **What does this folder contain?** (every file, listed)
2. **What is the purpose of each `.cs` file?** (one clear sentence per file)
3. **What changed recently and why?** (Change Log)

## Required README structure

Every folder that contains code MUST have a `README.md` with at least these sections, in this order:

```markdown
# <Project> / <FolderPath> — <Short layer purpose>

> **Status:** <e.g. Implemented | Partially implemented | Docs only>

## Purpose
<2–5 sentences: the responsibility of this folder and its place in the architecture.>

## Contents
| File | Purpose |
| --- | --- |
| `Foo.cs` | One sentence: what this type does and why it exists. |
| `Bar.cs` | One sentence: what this type does and why it exists. |

## Conventions / Notes          ← optional but recommended
<Rules an agent must follow when adding code here.>

## Implementation status
| Item | State |
| --- | --- |
| <feature/file> | ✅ / ⚠️ / ❌ |

## Change Log
| Date | Author | Change |
| --- | --- | --- |
| YYYY-MM-DD | <name or agent> | What changed and why. |
```

### Rules for the `Contents` table (per-`.cs` purpose)

- **Every `.cs` file in the folder must appear** — including new, renamed, or deleted files
  (mark deletions as `~~File.cs~~ (deleted)` in the Change Log, remove from Contents).
- One sentence per file describing **what it does and why it exists**, not just its name.
  - ✅ "`RegisterDocumentCommand.cs` — CQRS command carrying the document id to register; defines the request contract consumed by the controller."
  - ❌ "`RegisterDocumentCommand.cs` — the command." 
- For classes with multiple responsibilities, mention the key members briefly.
- When a file is added, renamed, or has its responsibility changed, update the row **and add a
  Change Log entry**.

### Rules for the `Change Log` section

- **Append-only**: add new rows at the top; never rewrite history.
- One row per meaningful change (a feature, a refactor, a rename, a deletion, a behavior change).
  Do not log whitespace/typo fixes.
- Format: `| YYYY-MM-DD | <author> | <imperative summary + reason> |`
- Reference the issue/commit when one exists: `… — see commit 36a9e24 / issue #12`.
- If several folders changed in one task, **each** folder's README gets its own Change Log row.

## Workflow for agents

1. **Before editing a folder**, read its `README.md` to understand intent and conventions.
2. **Make the code change.**
3. **Update that folder's `README.md` in the same turn:**
   - refresh **Purpose** / **Implementation status** if the responsibility or completeness shifted,
   - add/update rows in **Contents** for every added/renamed/deleted/changed `.cs` file,
   - append a row to **Change Log**.
4. **Verify** (`dotnet build`, `dotnet test`) before reporting the task complete.
5. If a parent folder's overview (e.g. `InstitutionalDiwan.Server/README.md`) lists subfolder
   statuses, update the parent row too.

## Project layout at a glance

| Folder | Layer | README |
| --- | --- | --- |
| `InstitutionalDiwan.Domain/` | Pure domain — entities, value objects, errors, events, repository contracts. No external packages. | `README.md` |
| `InstitutionalDiwan.Application/` | CQRS use cases — commands/queries, validators, behaviors, abstractions. No EF Core / ASP.NET. | `README.md` |
| `InstitutionalDiwan.Infrastructure/` | EF Core persistence, auth, storage, outbox, background jobs, clock. | `README.md` |
| `InstitutionalDiwan.Server/` | ASP.NET Core presentation — controllers, contracts, middleware, extensions. | `README.md` |
| `InstitutionalDiwan.Tests/` | Unit/integration tests (xUnit) for all layers. | `README.md` |
| `institutionaldiwan.front/` | Standalone React SPA front end (Feature-Sliced Design; has its own `AGENTS.md`). Not part of the solution. | `README.md` |

Subfolders with their own `README.md` (e.g. `Abstractions/`, `Behaviors/`, `Features/`,
`Persistence/`, `Controllers/`, `Contracts/`, `Middleware/`, `Extensions/`, …) follow the same
structure and the same update rule. The front-end project is separate from the .NET solution and
follows its own rules, described in `institutionaldiwan.front/AGENTS.md`.

## Housekeeping rules

- **Naming:** code namespaces are `InstitutionalDiwan.*`. Legacy `NewDiwan.*` references and the
  old `NewDiwanDbContext` name are being phased out — do not introduce new ones.
- **Dependency direction:** Domain → nothing; Application → Domain; Infrastructure → Domain +
  Application abstractions; Server → Application + Infrastructure.
- Keep README status tables truthful — an out-of-date status is a bug.
- Never commit secrets or connection strings with real credentials; use `appsettings.Development.json`
  local values or user-secrets.

## Definition of done

A task is complete when:

- [ ] Code builds with 0 errors (`dotnet build`)
- [ ] All tests pass (`dotnet test`) and new behavior is covered
- [ ] Every touched folder's `README.md` reflects the new contents (`Contents` table accurate)
- [ ] A `Change Log` row was appended to each touched folder's README
- [ ] No new naming-drift or dependency-rule violations
