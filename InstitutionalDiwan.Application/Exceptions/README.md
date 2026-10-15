# Application / Exceptions

> **Status:** Target design — **no exception types implemented yet**.

## Purpose

Expected business-rule violations are modelled with the Result/Error pattern
(`InstitutionalDiwan.Domain.Common.Result`) and **never thrown**. This folder is reserved for the
small set of technical/pipeline exceptions that represent unexpected infrastructure or contract
failures, which bubble up to the API middleware and are translated into RFC 7807 ProblemDetails.

## Planned exceptions

| Exception | When | API mapping |
| --- | --- | --- |
| `ValidationException` | A non-Result pipeline consumer fails validation. Carries a collection of `ValidationError(string PropertyName, string ErrorMessage)`. | `400 Bad Request` |
| `ConcurrencyException` | Optimistic concurrency conflict — a `RowVersion` mismatch on `DOCUMENTS`, `NUMBERING_COUNTERS`, or `DOCUMENT_MOVEMENTS` (the three aggregates exposing `byte[] RowVersion`). | `409 Conflict` |

## Design notes

- Keep these thin: they are carriers for structured data, not behavior.
- The domain's own `DomainException` (`InstitutionalDiwan.Domain.Common`) is separate — it signals an
  invariant breach and also surfaces to the middleware but is not one of these pipeline exceptions.
- Middleware translation lives in [`../../InstitutionalDiwan.Server/Middleware/`](../../InstitutionalDiwan.Server/Middleware/README.md).

## Implementation status

| Type | State |
| --- | --- |
| `ValidationException` | ❌ Not implemented |
| `ConcurrencyException` | ❌ Not implemented |

The `RowVersion` columns these exceptions relate to **are** already configured on
`Document`, `NumberingCounter`, and `DocumentMovement`.
