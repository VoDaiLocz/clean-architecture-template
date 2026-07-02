# Source Resolution

## Purpose

P3.3 resolves shortlinks, external web sources, and SharePoint URLs into durable source resolution records.

Resolution records preserve both the original audited URL and the final resolved URL so later content factory steps do not depend on fragile shortlinks.

## Application Contract

Handler:

- `ResolveToeicExternalSourcesHandler`

Resolver:

- `IExternalSourceResolver`

Result:

- `ResolvedCount`
- `FailedCount`

## Domain Model

Domain records:

- `SourceResolutionRecord`
- `SourceResolutionStatus`

## Tables

SQLite local/test table:

- `source_resolution_records`

PostgreSQL migration:

- `013_source_resolution_records`

Indexes:

- `idx_source_resolution_records_source_status`

## Data Rules

1. Only accessible shortlink, external web, and SharePoint sources are resolved in P3.3.
2. Original URL and resolved URL are both persisted.
3. HTTP status code and redirect count are persisted.
4. 2xx and 3xx resolver responses are marked `Resolved`.
5. Failed resolver responses are persisted as `Failed`.
6. Blocked sources are skipped by resolution.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ResolveToeicExternalSources|SourceResolutionRecord|013_source_resolution_records" backend/src backend/tests docs/product
```
