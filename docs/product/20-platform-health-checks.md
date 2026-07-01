# Platform Health Checks

## Purpose

Production deployment must expose dependency readiness for the API, database, object storage, and background job queue.

P1.8 adds the first platform health service and API endpoint.

## Endpoint

```text
GET /api/health
```

Audience:

```text
Operations
```

Response contract:

```text
PlatformHealthSnapshot
```

## Dependencies Checked

| Dependency | Check |
| --- | --- |
| database | query repository count from known table |
| object-storage | list a health prefix |
| background-jobs | lease check on empty queue |

## Status Values

- `Healthy`
- `Unhealthy`

Overall status is `Unhealthy` if any dependency is unhealthy.

## Implementation

Application contract:

```text
Application.Common.Health.IPlatformHealthService
```

Infrastructure implementation:

```text
Infrastructure.Health.PlatformHealthService
```

API registration:

```text
GET /api/health
```

API contract catalog:

```text
GET /api/health -> PlatformHealthSnapshot
```

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "IPlatformHealthService|PlatformHealthService|/api/health|Platform Health Checks" backend/src backend/tests docs/product
```
