# Infrastructure / Time

> **Status:** Implemented — `SystemClock` implements `IClock`, and `DateTimeProvider` adds the
> institution-timezone helpers.

## Purpose

Concrete implementations of the application clock abstractions, keeping time a testable dependency
rather than a static call sprinkled through the code.

## Components

### `SystemClock.cs` (`: IClock`)
- The single production source of "now"; `UtcNow` returns `DateTime.UtcNow` on every call.
- Registered as a singleton, so the whole running application shares one clock.

### `DateTimeProvider.cs` (`: IDateTimeProvider`)
- `UtcNow` delegates to `IClock`.
- `ToInstitutionLocal(utc)` converts UTC to `Asia/Damascus` for display and calendar-sensitive work.
- `NumberingYear(utc)` returns the local-calendar year, which is what official numbering uses.
- Resolves the IANA id `Asia/Damascus` first, then `Syria Standard Time` for Windows hosts, and falls
  back to UTC if neither resolves instead of throwing at startup.

## Why it matters here

- `NumberingRegistry.AllocateNextSequence(numberingYear)` and `DocumentNumber.Issue(...)` take an
  explicit year, so the choice of calendar boundary must be consistent with the institution's timezone
  rather than the server's local time.
- `AuditLog.Record` normalizes `OccurredAtUtc` to UTC with millisecond precision before computing the
  SHA-256 hash, so the value must still match `datetime2(3)` after a database round-trip.

## Registration

`AddInfrastructure` registers `IClock → SystemClock` and `IDateTimeProvider → DateTimeProvider` as
singletons, alongside the audit repository, interceptor, request context, and transaction manager.
