# Infrastructure / Authorization

> **Status:** Target design — **no code yet**.

## Purpose

Implements InstitutionalDiwan's three-tier access-control model (Modules 02 & 03): **permission-based
RBAC**, **organizational/department scope**, and **per-document exceptions**.

## Planned components

### `HasPermissionAttribute.cs` & `PermissionAuthorizationPolicyProvider.cs`
Declarative permission checks on endpoints, e.g.
`[HasPermission("DOCUMENTS.REGISTER")]`, `[HasPermission("MOVEMENTS.REDIRECT")]`.
The policy provider dynamically builds an authorization policy for each permission code instead of
requiring one policy per permission to be registered by hand.

### `PermissionAuthorizationHandler.cs`
Evaluates whether the current user holds the required `PermissionCode` through their active
`UserRoles → RolePermissions` chain.

### `DepartmentScopeEvaluator.cs`
Decides whether a target `DepartmentId` falls within the user's scope:
- the user's `PrimaryDepartmentId`, **or**
- an active `UserDepartmentAssignment` (optionally expanded to the **subtree** when `IncludeSubtree = true`), **or**
- institution-wide scope (`ScopeType`).

### `DocumentAccessEvaluator.cs`
Combines **organizational scope**, **movement participation** (`DOCUMENT_MOVEMENTS`), **confidentiality
level**, and active `DocumentAccessGrant` exceptions (`IsCurrentlyValid(utcNow)`) to decide whether a
user may `VIEW`, `DOWNLOAD`, `FORWARD`, or `MANAGE` a specific document.

## How it maps to the domain

| Domain concept | Used for |
| --- | --- |
| `User.ScopeType`, `User.PrimaryDepartmentId` | Base scope decision |
| `UserDepartmentAssignment` (`IncludeSubtree`, `StartDate`/`EndDate`, `IsActive`) | Extra department scope, subtree expansion |
| `Role` / `RolePermission` / `UserRole` | Permission resolution |
| `DocumentAccessGrant.AccessLevel` (`VIEW|DOWNLOAD|FORWARD|MANAGE`) and `IsCurrentlyValid` | Document-level exceptions |
| `Document.ConfidentialityLevel` | Additional restriction |

## Implementation status

| Component | State |
| --- | --- |
| Permission attribute + dynamic policy provider | ❌ Not implemented |
| Permission handler | ❌ Not implemented |
| Department scope evaluator | ❌ Not implemented |
| Document access evaluator | ❌ Not implemented |

The domain already models every input these evaluators need (grants, assignments, roles); only the
evaluation logic is missing. `Program.cs` does not call `AddAuthorization` beyond the default.
