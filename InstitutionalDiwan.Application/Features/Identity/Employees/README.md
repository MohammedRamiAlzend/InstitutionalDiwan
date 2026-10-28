# Application / Features / Identity / Employees — employee records

> **Status:** Implemented — create, update, deactivate, activate, get by id, and a paged list.

## Purpose

Every use case behind `/api/employees`: the personnel records a user account can be linked to. An
employee belongs to a department, may carry an employee number and contact details, and is
soft-deleted through `IsActive` so historical references survive.

## Contents

| File | Purpose |
| --- | --- |
| `EmployeeResponse.cs` | Read model returned by every endpoint in the slice (`From(Employee)`). |
| `CreateEmployeeCommand.cs` / `CreateEmployeeCommandHandler.cs` / `CreateEmployeeCommandValidator.cs` | Validates the department exists and is active and that the employee number is unique, then persists a new active employee. |
| `UpdateEmployeeCommand.cs` / `UpdateEmployeeCommandHandler.cs` / `UpdateEmployeeCommandValidator.cs` | Loads the employee (404), revalidates the department and employee number, and applies `Employee.Update`. |
| `DeactivateEmployeeCommand.cs` / `DeactivateEmployeeCommandHandler.cs` | Soft delete through `Employee.Deactivate` (idempotent). |
| `ActivateEmployeeCommand.cs` / `ActivateEmployeeCommandHandler.cs` | Reactivates a deactivated employee through `Employee.Activate` (idempotent). |
| `GetEmployeeByIdQuery.cs` / `GetEmployeeByIdQueryHandler.cs` | Fetches one employee by id (404 when missing). |
| `ListEmployeesQuery.cs` / `ListEmployeesQueryHandler.cs` | Returns one page of employees (`PagedResult<EmployeeResponse>`) ordered by Arabic name. `Search` matches the Arabic name, the English name, and the employee number; `DepartmentId` restricts the page to one department and `IncludeSubDepartments` widens it to every descendant (resolved through `IDepartmentRepository.GetDescendantIdsAsync`); `Page`/`PageSize` are clamped by `PageRequest`. |

## Conventions / Notes

- Every action requires `EMPLOYEE_MANAGE`; the endpoints live on `EmployeesController`.
- Filtering, sorting, and paging run in `IEmployeeRepository.ListPagedAsync`, never in memory.
- Deactivation does **not** cascade to `User.EmployeeId`; existing accounts keep the link so history
  stays readable.

## Implementation status

| Item | State |
| --- | --- |
| Create / Update commands (+ validators) | ✅ |
| Deactivate / Activate commands | ✅ |
| `GetEmployeeByIdQuery` | ✅ |
| `ListEmployeesQuery` (paged + name/number search + department filter) | ✅ |

## Change Log

| Date | Author | Change |
| --- | --- | --- |
| 2026-10-28 | agent | `ListEmployeesQuery` gained `DepartmentId` + `IncludeSubDepartments`; the handler resolves the department and its descendants through `IDepartmentRepository` and passes the id set to the repository, so the department dialog can list a unit's roster. |

| 2026-10-22 | Buffy | Handlers now pass `IUnitOfWork.UtcNow` into `Employee.Create`/`Update`/`Activate`/`Deactivate` for deterministic timestamps. |
| 2026-10-22 | opencode | Documented the slice and made `ListEmployeesQuery` return `PagedResult<EmployeeResponse>` with `page`, `pageSize`, and a name/employee-number `search`; `EmployeeRepository.ListPagedAsync` applies the filter and `Skip`/`Take` in SQL. |
| 2026-10-18 | opencode | Added the slice (`CreateEmployeeCommand`, `UpdateEmployeeCommand`, `DeactivateEmployeeCommand`, `ActivateEmployeeCommand`, `ListEmployeesQuery`, `GetEmployeeByIdQuery`) together with the `EMPLOYEE_MANAGE` permission. |
