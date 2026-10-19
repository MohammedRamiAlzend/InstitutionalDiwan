# Add statistics endpoints

## Understanding
The repository contains multiple API controllers but currently no explicit endpoints that return application statistics (e.g., counts, dashboards, or aggregated metrics). The goal is to create a short, actionable implementation plan to add statistics endpoints under a new StatisticsController in the Server project.

## Assumptions
- Statistics will be read-only, served from server-side queries (no write operations).
- MediatR is used for request handling across the codebase; new endpoints should follow the same pattern (Controller → ISender → handlers).
- Aggregations will use existing application/infrastructure services and DbContext; exact metrics will be implemented later by the feature owner.

## Approach
Create a new thin controller in InstitutionalDiwan.Server.Controllers named `StatisticsController` exposing one or more GET endpoints (e.g., `/api/statistics/summary`). Each endpoint will forward a Query to MediatR (ISender). Implement corresponding Query records in the Application project and handlers that return a Statistics DTO. Start with minimal, safe surface area: a single `GET /api/statistics/summary` returning counts for documents, users, and departments.

## Key Files
- InstitutionalDiwan.Server/Controllers/StatisticsController.cs - new controller exposing endpoints
- InstitutionalDiwan.Application/Statistics/Queries/SummaryQuery.cs - MediatR query record
- InstitutionalDiwan.Application/Statistics/Handlers/SummaryHandler.cs - handler that queries the read model
- InstitutionalDiwan.Infrastructure/Statistics/StatisticsRepository.cs (optional) - small repository abstraction for data access
- InstitutionalDiwan.Server/Controllers/README.md - update to list the new controller and route

## Risks & Open Questions
- Authorization: which policy should guard statistics? Default to `[Authorize]` and ask product about a specific policy (e.g., `REPORT_VIEW`).
- Performance: some aggregates may be expensive; handlers should be implemented efficiently and consider caching if needed.
- Data model: exact metrics (which counts, date ranges, grouping) need product specification.

## Steps
1. step-1 - Create controller file InstitutionalDiwan.Server/Controllers/StatisticsController.cs with a `GET /api/statistics/summary` endpoint wired to ISender.
2. step-2 - Add Application layer Query record InstitutionalDiwan.Application/Statistics/Queries/SummaryQuery.cs and corresponding response DTO InstitutionalDiwan.Application/Statistics/Responses/SummaryResponse.cs.
3. step-3 - Implement handler InstitutionalDiwan.Application/Statistics/Handlers/SummaryHandler.cs that uses repositories/DbContext to build the SummaryResponse.
4. step-4 - (Optional) Add a lightweight repository in Infrastructure InstitutionalDiwan.Infrastructure/Statistics/StatisticsRepository.cs to encapsulate data access used by the handler.
5. step-5 - Add unit tests for the handler (and controller integration tests if project has test infra) under the existing test projects or a new test file.
6. step-6 - Update InstitutionalDiwan.Server/Controllers/README.md to document the new route and permission policy.
7. step-7 - Run `dotnet build` and run tests; fix compile/test issues.

Implementing the first minimal endpoint (summary counts) should take the work from design → code → tests in an isolated set of changes. Once this plan is approved I can start on step-1 and produce the controller skeleton.
