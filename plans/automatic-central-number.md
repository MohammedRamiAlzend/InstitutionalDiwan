# Plan — automatic, system-owned central number and numbering year

Status: implemented (2026-11-05): backend, front end, tests (800) and a live API check. Not yet covered: a dedicated Register-with-movement test.

## Confirmed decisions (supersede the sections below where they conflict)

1. UI label: «رقم النظام».
2. A draft becomes REGISTERED the moment it receives its central number (unchanged flow, now automatic).
3. Numbering year: derived automatically from the clock; the UI never shows it. No back-dated override needed (the optional `NumberingYear` parameter on Register/Issue/Replace is removed from the UI and kept only as an internal default).
4. The central number is a **GUID** (`Guid.NewGuid()` text), not a formatted sequence. No `FormatPattern`, no reservation worry; a duplicate GUID is treated as practically impossible and the `OfficialNumberTaken` check is kept only as a cheap safety net.
   - Schema is untouched: `DocumentNumber.SequenceValue` (required, part of unique index `RegistryId+NumberingYear+SequenceValue`) is still filled from the registry counter internally and never shown. If that counter becomes a contention point, a follow-up migration can drop the index and store 0.
   - Department registers keep their typed number + date.
   - `OfficialNumberText` max length is 100, enough for a 36-char GUID.

## Goal

The user never types the central (Diwan) official number or the numbering year. The system issues the
number from the central registry and derives the year itself. The number is a **read-only serial** that
identifies the document to people; the document's technical identity stays `DocumentId` (GUID).

Decisions already taken with the user:
- Do **not** delete the number/year or their columns (department registers, counters, audit all depend on them).
- Do **not** replace the number with a GUID (`DocumentId` already is one; a GUID is unusable on paper and
  loses the gap-free audit value).
- Department registers keep their own typed number + date (unchanged).

## What already works (no change needed)

- `RegisterDocumentCommand`, `IssueOfficialNumberCommand`, `ReplaceOfficialNumberCommand` already allocate
  the next gap-free sequence from the matching registry, render it with `FormatPattern`, and default the
  year to `request.NumberingYear ?? document.DocumentDate?.Year ?? clock.Year`.
- The only place that accepts a **user-typed** number and year is `CreateDocumentCommand` (create + register
  in one step) and the front-end dialogs that feed it.

## Backend

1. `CreateDocumentCommand`: remove `OfficialNumberText` and `NumberingYear`.
2. `CreateDocumentCommandHandler`: drop the typed-number branch (`OfficialNumberEmpty`, `ExistsByNumberAndYear`
   on user text). Derive `numberingYear` from the clock, allocate via `registry.AllocateNextSequence`, format with
   `OfficialNumberFormatter.Format(registry.FormatPattern, year, sequence)`, keep the uniqueness check as a
   safety net, then `DocumentNumber.Issue` + `document.Register` as today. Order stays: schema values and
   required fields are validated before a sequence is consumed.
3. `CreateDocumentCommandValidator`: remove the number and year rules.
4. `Server/Contracts/Correspondence/CreateDocumentRequest.cs` + `DocumentsController` (arguments, XML docs,
   OpenAPI summary/description/examples — `OpenApiDocumentationTests` must stay green).
5. Numbering mode: the institution-setting `NumberingMode` (Automatic / Manual) loses its meaning for central
   numbers. Keep the column and lookup (no destructive migration) but ignore `Manual` for central numbers and
   document it; revisit removal in a later change.
6. Year: keep `NumberingYear` request parameters on Register/Issue/Replace as optional overrides for
   administrators (back-dated registrations), but the UI stops sending them.
7. Errors: `Numbering.OfficialNumberEmpty` becomes unreachable from the create flow; keep it for the domain.
   `Numbering.OfficialNumberTaken` stays as the safety net.
8. No migration needed (no schema change).

## Front end (`institutionaldiwan.front/src`)

1. `features/document-create`: remove the number and year fields from `CreateDocumentDialog.tsx`,
   `model/schemas.ts`, `model/types.ts`; the request no longer carries them. Show a short Arabic helper
   that the number is issued automatically.
2. `features/document-manage/ui/RegisterDraftDialog.tsx`, `features/numbering-manage/ui/IssueNumberDialog.tsx`,
   `ReplaceNumberDialog.tsx`: remove the year input (leave only reason/notes where applicable).
3. Read-only display stays: `entities/document/ui/OfficialNumberTag.tsx`, registry/archive/movement tables,
   `DocumentNumbersTab`, department register book. Rename user-facing label to the agreed wording if the user
   wants «الرقم التسلسلي» instead of «الرقم الرسمي» (open question below).
4. `features/numbering-manage/ui/RegistryFormDialog.tsx`: the central registry's `FormatPattern` is now the
   only control over the shape of the number; make sure the pattern help text is clear.
5. Institution settings form: hide or disable the numbering-mode select (value kept) with a note.
6. Tutorial guides (`shared/config/guides/*.ts`): update texts that mention typing a number/year; run
   `npm run lint` so guide ids stay in sync.
7. `shared/api/errorMessages.ts`: keep the number/year messages (still reachable via Register/Issue overrides).

## Tests

- Update the 8+ `CreateDocumentCommand` call sites in `InstitutionalDiwan.Tests` (handler, validator, lifecycle,
  access, archiving, attachment, movement, persistence tests) to the new signature.
- New tests: create allocates sequence 1 then 2 in the same year with the registry pattern; year follows the
  clock; two concurrent-style creates never collide; validator no longer requires number/year; a pattern
  collision still returns `Numbering.OfficialNumberTaken`.
- Remove the obsolete "typed number empty / taken by user text" tests.
- Gates: `dotnet test`, `npm run build`, `npm run lint`.

## Docs (docs-with-code rule)

- READMEs: `Application/Features/Correspondence/Documents`, `Application/Features/Numbering`,
  `Server/Controllers`, front `features/document-create`, `features/document-manage`,
  `features/numbering-manage`, `entities/document`, plus a Change Log row in each.
- `institutionaldiwan.front/knowledge.md`: new `POST /api/documents` body (no number/year).

## Verification

Run API + SPA, create a document as a department user: no number/year fields; the document appears
registered with a number like `2026/000001`, second document `2026/000002`; department-register entry still
works; the Numbers tab shows the central number and history.

## Risks / open questions

1. Wording: keep «الرقم الرسمي» or rename to «الرقم التسلسلي» in the UI?
2. Should a draft created through the draft flow stay number-less until registered? (Current behaviour: yes —
   the plan keeps it.)
3. Back-dated registrations: is the admin-only `NumberingYear` override on Register/Issue still needed, or can it
   be removed entirely?
4. Numbering mode `Manual`: acceptable to ignore it now and remove the lookup value later?

---

## Addendum (2026-11-05): creating a document also opens a movement and records the register line

User decision: when a document is created, a movement is opened in the same step, and the user types the
**number and date** that go into the register.

### Behaviour

One "create document" action now does, atomically (all-or-nothing, one transaction):

1. Creates the document and registers it with the automatic central **system number** (GUID) — as above.
2. Writes the **department register line** for the creator's own department: the number and date typed by the
   user (`RecordDepartmentRegistration` semantics: one line per department, number stable, dated history).
3. Opens the **movement** from the creator's department to the chosen destination department
   (`CreateMovement` semantics: only one active movement, source = caller's primary department).

### Backend

1. `CreateDocumentCommand` gains an optional block (all required together when present):
   - `DepartmentRegistryNumberText`, `DepartmentRegistryDate` (user input for the register line),
   - `Movement`: `DestinationDepartmentId`, `MovementTypeId` (default Referral), `AssignedToUserId?`,
     `Instructions?`, `DueDate?`.
2. Do not call three commands from the controller (three transactions, partial failure). Extract the logic of
   `RecordDepartmentRegistrationCommandHandler` and `CreateMovementCommandHandler` into small shared application
   services (e.g. `DepartmentRegisterWriter`, `MovementOpener`) that both the existing handlers and
   `CreateDocumentCommandHandler` call; the composite handler saves once through the unit of work.
3. Validation before anything is consumed: department register configured for the caller's department
   (`RequireDepartmentRegistryAsync`, error `DepartmentRegistryNotConfigured`), destination exists/active,
   route allowed by `CentralDiwanRouting` (see interaction below), schema/required fields valid.
4. Register date: user-supplied, validated not in the far future; stored as the department's own date.
5. Response: `DocumentResponse` plus the opened `MovementId` (new optional field), so the SPA can navigate.
6. Events/audit: the existing `DocumentCreated/Registered`, department-registration and movement-created events
   fire as today, in one unit of work, so the tamper-evident audit chain keeps all three entries.
7. Permissions: caller needs the create-document permission **and** the movement permission (`DocumentEdit`);
   if the caller lacks the movement permission the movement block is rejected, not silently skipped.
8. Back-compat: the block is optional so existing API clients and tests that create without movement still work.

### Front end

1. `CreateDocumentDialog`: add a «السجل والإحالة» section with: رقم السجل (text), تاريخ السجل (date, default
   today), القسم المستلم (department select, default = central Diwan when the caller is not the Diwan),
   نوع الحركة (default Referral, preselects the lookup default), تعليمات (optional).
2. Numbers typed by the user stay Latin/LTR islands (`dir="ltr"`); dates use the shared date input.
3. After success navigate to the document and show a toast with the system number and movement status.
4. New Arabic errors for the new codes (register not configured, route not allowed) in `errorMessages.ts`.
5. Tutorial guide ids for the new fields; `npm run lint` must pass.

### Interaction with the routing-rules plan (`plans/movement-routing-rules.md`)

- The creator's department may only send directly to the central Diwan (or from it). If the user picks another
  ordinary department, either the movement is rejected with `Movements.CentralDiwanRoutingRequired`, or — once
  the routing plan (auto-route through the hub, `FinalDestinationDepartmentId`) lands — the dialog sends the final
  destination and the system builds the route. Until then the destination select lists only reachable departments.

### Tests

- Handler: create + register line + movement in one call; failure of any step leaves nothing persisted
  (no document, no number, no register line, no movement); movement block absent → old behaviour.
- Validator: number/date required together with the movement block; destination required.
- Persistence: department register line has the typed number and date; movement step 1 is
  creator department → destination; document status ends REGISTERED then IN_MOVEMENT per existing rules.

### Docs

READMEs for Documents, Movements, Numbering/Numbers, front `features/document-create`, a `knowledge.md` update for
the new `POST /api/documents` body, and Change Log rows.

### Open questions for this addendum

1. Is the register line always the **creator's own department** register (assumed), or can the user pick another?
2. Is the destination department **mandatory** at creation, or optional (create + register line only, movement later)?
3. If the creator's department has no register configured, should creation fail (assumed) or fall back to just the
   central number?
4. Should the movement open already **sent**, or stay a draft movement until the user presses send?

### Confirmed answers to the addendum questions

1. The register line is always the creator's own department register.
2. The movement block is **optional**: the user may create with only the register line and open the movement later,
   or choose a destination at creation and open it directly.
3. A department register is **mandatory**: creation fails with `Numbering.DepartmentRegistryNotConfigured` when
   the creator's department has none. The register number and date are therefore always required.
4. When a destination is chosen the movement is opened **already sent** (create + send in one step).
