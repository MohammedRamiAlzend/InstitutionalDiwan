# Seed/

Ordered, environment-aware database seeding.

> **Status:** Implemented — three seeding modes (`Everything`, `Standard`, `None`), 33 reference-catalog seeders, the institution-settings singleton, a
> 15-department demo organization tree (Development/Staging), 28 demo employees attached to that
> tree (Development/Staging), 17 permissions, system roles with 20 grants, and demo users.

## How it works

| Piece | Role |
| --- | --- |
| `ISeeder` | Contract: `Order` (execution position, lower first), `Environments` (host environments it may run in), `Modes` (seeding modes it runs in; default every mode), `SeedAsync` (idempotent work). |
| `SeedingEnvironments` | Well-known environment names: `All` (wildcard), `Development`, `Staging`, `Production`. |
| `SeederContext` | Shared services per run: `Db`, `PasswordHasher`, `EnvironmentName`, `Options` (`SeedingOptions`: mode and default password). |
| `SeedingModes` / `SeedingOptions` | The mode names (`Everything`, `Standard`, `None`, plus the `Any` wildcard) and the options parsed from `Seeding:Mode` / `Seeding:DefaultPassword` (empty mode = `Everything`; unknown mode throws at startup). |
| `SeederRunner` | Sorts registered seeders by `Order` (ties broken by type name), filters by environment and mode, executes, returns the count. |
| `LookupSeederBase<TEntity>` | Base for reference-catalog seeders: supplies the fixed `Catalog` of stable ids + English/Arabic labels and inserts only the missing ids, so seeding is idempotent and never overwrites an edited label. |

Seeders are registered in DI (`AddInfrastructure`) and executed once at startup by
`DatabaseExtensions.InitializeDatabaseAsync`, which passes the host's current environment name
(`app.Environment.EnvironmentName`), so each seeder decides on its own whether it runs in
Development, Production, etc.

## Registered seeders

| Order | Seeder | Environments | Data |
| --- | --- | --- | --- |
| 41–90 | 34 `LookupSeederBase<T>` subclasses (`IdentityLookupSeeders.cs`, `ParticipantLookupSeeders.cs`, `MovementsLookupSeeders.cs`, `AttachmentsLookupSeeders.cs`, `RelationsLookupSeeders.cs`, `ArchivingLookupSeeders.cs`, `CorrespondenceLookupSeeders.cs`, `AuditLookupSeeders.cs`, `NotificationsLookupSeeders.cs`, `NumberingLookupSeeders.cs`, `OrganizationLookupSeeders.cs`, `DepartmentTypeSeeder.cs`) | All | Every reference catalog: scope/assignment/access levels, participant roles + target types, movement types/statuses/step statuses/event types, storage provider types + file types + the default local provider, relation types + action types, archive event types + locations, document directions/types/statuses/confidentiality/priority, audit action types (8, including `Execute`) + audit entity types (13, including `Operation`), notification types/priorities/channels/outbox statuses, numbering modes/statuses/event types/scopes (central vs department register)/reset policies, department types. |
| 95 | `InstitutionSettingSeeder` | All | The singleton `InstitutionSetting` row (automatic numbering, yearly reset) plus the `CENTRAL_DIWAN` root department it designates; runs after the numbering lookups and department types it references. |
| 96 | `DepartmentSeeder` | Development, Staging; mode `Everything` | A demo organization tree of 15 departments across four levels: the `CENTRAL_DIWAN` root (reused from Order 95), four directorates, seven departments, and three sections (e.g. `DIR-ADMIN` → `DEP-HR` → `SEC-PAYROLL`), so the departments screen has a realistic hierarchy to display. Idempotent by `Code`. |
| 97 | `EmployeeSeeder` | Development, Staging; mode `Everything` | 28 demo employees attached by department code — the Central Diwan office, every directorate, department, and section — so a department card can list the people who belong to it. Includes Arabic/English names, employee numbers (`EMP-0001`…), job titles, and a few contact details. Idempotent by `EmployeeNumber`. |
| 98 | `StandardDepartmentSeeder` | All; mode `Standard` | `INDIVIDUAL_DIWAN` (ديوان الفرد) and `INVESTIGATION_DIWAN` (ديوان التحقيق), directorates under `CENTRAL_DIWAN`. Idempotent by `Code`. |
| 100 | `PermissionSeeder` | All | Permission catalog — the 17 codes in `Permissions.All`, including `DOCUMENT_ARCHIVE`, `LOOKUP_MANAGE`, `NUMBERING_MANAGE`, `AUDIT_VIEW`, and `AUDIT_MANAGE`. |
| 200 | `RoleSeeder` | All | `ADMIN` / `USER` roles plus their permission grants (ADMIN receives all 17, USER the three document permissions; 20 grants total). |
| 300 | `UserSeeder` | Development, Staging; mode `Everything` | Demo accounts `admin` / `user` (password `12345678`) with role assignments. `admin` belongs to the Central Diwan and `user` to `DEP-HR`. Idempotent by username; existing accounts are never changed. |
| 305 | `SuperAdminSeeder` | All; modes `Standard`, `None` | Role `SUPER_ADMIN` with every code in `Permissions.All` (re-synced each startup) and the `superadmin` account (system scope, Central Diwan, password from `Seeding:DefaultPassword`). An existing account is never changed. |
| 310 | `StandardAccountSeeder` | All; mode `Standard` | Six accounts with the `DIWAN_OPERATOR` role (created here: document view/create/edit + `NUMBERING_VIEW`/`NUMBERING_MANAGE`, so a diwan can write its department register number before receiving) and a linked employee (`STD-0001`…`STD-0006`): «الديوان المركزي» and «احمد ديوان مركزي» (institution scope, Central Diwan), «ديوان الفرد» and «احمد ديوان الفرد» (department scope), «ديوان التحقيق» and «احمد ديوان التحقيق» (department scope). The username is the Arabic name itself. |

## Seeding modes

Set `Seeding:Mode` in appsettings (or the `Seeding__Mode` environment variable):

| Mode | Result |
| --- | --- |
| `Everything` (default) | The previous behaviour: reference data, demo tree, demo employees and the `admin` / `user` accounts (Development/Staging). |
| `Standard` | Reference data, the three diwans, `superadmin` and the six diwan accounts — every password `Seeding:DefaultPassword` (default `12345678`). Environment-independent. |
| `None` | Reference data and only `superadmin`. |

> **Security:** the standard and super-admin seeders run in every environment, so a well-known password reaches Production unless `Seeding:DefaultPassword` is overridden there. Set the mode and password per environment. Switching mode never removes rows a previous mode created.

Order matters: the lookup catalogs are seeded before permissions/roles so business tables can
reference them, roles grant permission codes that the catalog defines first, and demo users are
assigned roles and primary departments that must already exist. The demo department tree reuses the Central Diwan root that
`InstitutionSettingSeeder` creates, so it runs immediately after it and before permissions/roles;
`EmployeeSeeder` runs right after the tree because every demo employee is attached by department
code.

All seeders are idempotent — they only insert what is missing and are safe to run on every
startup. Demo credentials are deliberately gated to Development/Staging; seeding users in
Production requires opting in by adding `SeedingEnvironments.Production` to
`UserSeeder.Environments` and supplying credentials from configuration.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-11-04 | Claude | Standard-mode diwan accounts now get a new `DIWAN_OPERATOR` role (document view/create/edit plus `NUMBERING_VIEW` and `NUMBERING_MANAGE`) instead of `USER`: a department must write its own register number (numbers tab, `POST /api/document-numbers/department-registration`) before it can receive a document, and `USER` could neither see the tab nor register. `StandardAccountSeeder` also moves already-seeded accounts to the new role on the next startup (users must log in again for the new permissions). |
| 2026-11-04 | Claude | Added seeding modes: `Seeding:Mode` in appsettings picks `Everything` (the former demo data, default), `Standard` (three diwans — الديوان المركزي, ديوان الفرد, ديوان التحقيق — with a `superadmin` account holding all 17 permissions plus two accounts per diwan, all with password `12345678`) or `None` (reference data and only `superadmin`). New seeders `StandardDepartmentSeeder` (98), `SuperAdminSeeder` (305), `StandardAccountSeeder` (310); the demo seeders now run only in `Everything`. |
| 2026-11-03 | Claude | `UserSeeder` now places `admin` in the Central Diwan and `user` in `DEP-HR` instead of a fixed department id with no row, so documents they create have a real owning department. Existing accounts and documents are not changed. |
| 2026-11-03 | Claude | Added `DOCUMENT_ARCHIVE` to the catalog (16 → 17) and granted it to `ADMIN` only (19 → 20 grants), because `USER` already holds `DOCUMENT_EDIT` and archiving must stay restricted. Both seeders are idempotent, so existing databases receive it on the next startup. |
| 2026-11-02 | Claude | The audit entity-type seed gained a 14th row, `DocumentSchemaVersion` (مخطط حقول الوثيقة), used by the dynamic-fields audit rows. |
| 2026-10-28 | agent | Added `EmployeeSeeder` (Order 97, Development/Staging): 28 demo employees spread across the seeded tree (including the Central Diwan office), idempotent by `EmployeeNumber`, so the department detail card can list real people. |
| 2026-10-28 | agent | Added `DepartmentSeeder` (Order 96, Development/Staging): a 15-department demo organization tree across four levels (Central Diwan → 4 directorates → 7 departments → 3 sections) so the departments screen has a realistic hierarchy; reuses the Central Diwan root, idempotent by `Code`, gated away from Production. |
| 2026-10-26 | Qoder | Extended the audit lookups to 8 action types (`Execute` = 8) and 13 entity types (`Operation` = 13) so action-level audit rows can reference them; startup seeding inserts the new ids idempotently in every environment. |
| 2026-10-25 | opencode | Added `AUDIT_VIEW` and `AUDIT_MANAGE` to the catalog (14 → 16) and granted both to `ADMIN` (17 → 19 grants); extended the audit entity-type lookup to 12 types. |
| 2026-10-20 | Buffy | Added `DocumentNumberScopeSeeder` (Order 88, `DOCUMENT_NUMBER_SCOPES`: central register / department register) and two department event types (`RegisteredInDepartment`, `ReRegisteredInDepartment`) to `DocumentNumberEventTypeSeeder`; registered the new seeder in `Infrastructure/DependencyInjection.cs` (34 lookup seeders). |
| 2026-10-19 | Buffy | Added `InstitutionSettingSeeder` (Order 95) seeding the settings singleton and its `CENTRAL_DIWAN` root department; `PermissionSeeder` now seeds `NUMBERING_MANAGE` and `RoleSeeder` grants it to `ADMIN` (catalog 13 → 14 codes, ADMIN grants 16 → 17). |
| 2026-10-19 | opencode | `PermissionSeeder` now seeds `LOOKUP_MANAGE` and `RoleSeeder` grants it to `ADMIN` (catalog 12 → 13 codes, ADMIN grants 12 → 13); documented the 33 lookup seeders that this README previously omitted, including `LookupSeederBase<T>` and the 41–90 order range. |
| 2026-10-18 | opencode | Lookup seeders added for every reference catalog during the code-string → lookup-table refactor. |
