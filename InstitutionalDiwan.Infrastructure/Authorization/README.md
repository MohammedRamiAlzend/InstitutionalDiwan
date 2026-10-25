# Infrastructure / Authorization

> **Status:** Implemented — `DepartmentScopeEvaluator` and `DocumentAccessEvaluator` (the two
> contracts declared in `Application/Abstractions/Authorization/`). RBAC enforcement itself lives in
> `Server/Authorization/PermissionAuthorizationHandler` (one policy per permission code).

## Purpose

Implements InstitutionalDiwan's three-tier access-control model (Modules 02 & 03): **permission-based
RBAC**, **organizational/department scope**, and **per-document exceptions**. This folder owns the
second and third tiers; the first tier is enforced by ASP.NET Core policies generated from
`Permissions.All`.

## Components

### `DepartmentScopeEvaluator.cs` (`: IDepartmentScopeEvaluator`)
Decides whether a target `DepartmentId` falls within the caller's scope, in this order:
1. an institution-wide scope on the token (`INSTITUTION` / `SYSTEM`) or a matching primary
   department → allow immediately;
2. re-reads the user record (assignments are managed after the token was issued) and allows again if
   the persisted profile is institution-wide or its primary department matches;
3. `DEPARTMENT_TREE` scope → `IDepartmentRepository.GetDescendantIdsAsync` over the primary
   department;
4. any active `UserDepartmentAssignment` — the department itself, plus its subtree when the
   assignment has `IncludeSubtree = true`, respecting `StartDate`/`EndDate`.

Unauthenticated callers, empty department ids, and deactivated users are denied.

### `DocumentAccessEvaluator.cs` (`: IDocumentAccessEvaluator`)
Combines, in order: an active unexpired `DocumentAccessGrant` at or above the requested access
level → authorship (the creator always keeps access) → confidentiality gate (`SECRET` and
`TOP_SECRET` are never reachable through scope alone) → department scope over the document's owning
department (scope grants up to `DOWNLOAD`; `FORWARD` and `MANAGE` additionally require an
institution-wide scope type) → movement participation. A document that does not exist is reported as
“not allowed”, so callers do not leak existence.

Movement participation (Module 05) reads the document's active movement through
`IDocumentMovementRepository.GetActiveByDocumentIdAsync`. If the caller's organizational scope covers
either side of the current dispatched step (sender or receiver), the document is readable and
downloadable even outside the owning department. Participation never grants `FORWARD` or `MANAGE`,
and it does not bypass the confidentiality gate; a draft step (not yet sent) grants nothing.

## How it maps to the domain

| Domain concept | Used for |
| --- | --- |
| `User.ScopeType`, `User.PrimaryDepartmentId` | Base scope decision |
| `UserDepartmentAssignment` (`IncludeSubtree`, `StartDate`/`EndDate`, `IsActive`) | Extra department scope, subtree expansion |
| `Department.ParentDepartmentId` (via `GetDescendantIdsAsync`) | Tree expansion |
| `Role` / `RolePermission` / `UserRole` | Permission resolution (RBAC tier, token claims) |
| `DocumentAccessGrant.AccessLevel` (`VIEW|DOWNLOAD|FORWARD|MANAGE`) and `IsCurrentlyValid` | Document-level exceptions |
| `Document.ConfidentialityLevel` | Additional restriction |
| `DocumentMovement` current step (`From`/`To` department, dispatched status) | Movement participation (VIEW/DOWNLOAD) |
| `Document.CreatedByUserId` | Authorship bypass |

## Implementation status

| Component | State |
| --- | --- |
| Permission handler + one-policy-per-code | ✅ Implemented in `Server/Authorization/PermissionAuthorizationHandler` (registered by `AddInstitutionalAuthentication`) |
| `DepartmentScopeEvaluator` | ✅ Implemented |
| `DocumentAccessEvaluator` | ✅ Implemented (including Module 05 movement participation) |
| Dynamic policy provider / `HasPermissionAttribute` | ❌ Not implemented (the explicit `AddPolicy` loop covers the same codes) |

Both evaluators are registered as scoped services in `Infrastructure/DependencyInjection.cs` and
depend on `IUserContext` rather than on `HttpContext` directly.

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-25 | opencode | Added Module 05 movement participation to `DocumentAccessEvaluator`: sender/receiver on the active movement's current dispatched step gains VIEW/DOWNLOAD, still bounded by confidentiality and never FORWARD/MANAGE. |
| 2026-10-19 | Buffy | Implemented `DepartmentScopeEvaluator` (primary department, persisted assignments with subtree expansion, institution-wide scope) and `DocumentAccessEvaluator` (grants → authorship → confidentiality → scope, with FORWARD/MANAGE requiring institution-wide scope); both registered in DI behind the new `Application/Abstractions/Authorization` contracts. |
