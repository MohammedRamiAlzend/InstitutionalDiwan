# Infrastructure / Time

> **Status:** Target design — **no code yet**.

## Purpose

Concrete implementation of the application `IDateTimeProvider`, keeping time a testable dependency
rather than a static call sprinkled through the code.

## Planned component

### `DateTimeProvider.cs` (`: IDateTimeProvider`)
- Returns `DateTime.UtcNow` for all database timestamps (`datetime2(3)` columns).
- Provides helpers to convert UTC to the institution's configured timezone
  (`INSTITUTION_SETTINGS.DefaultTimezone`, default `Asia/Damascus`) for:
  - **official numbering years** (`DocumentNumber.NumberingYear`), and
  - Arabic UI display of dates/times.

## Why it matters here

- `NumberingRegistry.AllocateNextSequence(numberingYear)` and `DocumentNumber.Issue(...)` take an
  explicit year, so the choice of calendar boundary must be consistent with the institution's timezone
  rather than the server's local time.
- Domain events stamp `OccurredOnUtc` from `DateTime.UtcNow` via `DomainEventBase` today; if you want
  fully deterministic domain tests you would later thread the provider into the aggregate or assert on
  the persisted timestamp instead.

## Implementation status

| Component | State |
| --- | --- |
| `DateTimeProvider` | ❌ Not implemented |

No time abstraction is registered in `Program.cs`; there is also no `IDateTimeProvider` interface yet
in [`../../InstitutionalDiwan.Application/Abstractions/`](../../InstitutionalDiwan.Application/Abstractions/README.md).
