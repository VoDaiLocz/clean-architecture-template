# Typed API Contract Convention

## Purpose

Frontend and backend must not drift apart on route shape, audience, or response contract names.

P1.6 creates the first enforceable API contract catalog. It is not a full generated client yet; it is the production convention and testable source of truth that future generator work will consume.

## Source Of Truth

Backend catalog:

```text
Application.Common.ApiContracts.ApiContractCatalog
```

Catalog fields:

- HTTP method
- route
- audience
- response contract

Catalog version:

```text
2026-07-01
```

## Audience Rules

| Audience | Meaning |
| --- | --- |
| Learner | safe for learner-facing product UI |
| Admin | source/content/operator workflows only |
| Operations | health, metrics, release, and platform workflows |
| LegacyDemo | temporary legacy route retained until P4/P7 replacement |

Learner routes must not expose source, parser, draft, or validation internals.

## Change Rules

1. Any new API route must be added to `ApiContractCatalog`.
2. Any changed response contract name must update the catalog and tests.
3. Any route removal must update the catalog in the same commit.
4. Legacy demo routes must stay marked `LegacyDemo`.
5. Frontend code must consume typed contract names from generated or mirrored types as P1.6 evolves.
6. Future typed client generation must use this catalog or replace it with an equally testable OpenAPI source of truth.

## Current Enforcement

Tests verify:

- catalog is not empty
- version is explicit
- learner home route has typed response contract
- source manifest import route has typed response contract
- no duplicate method+route entries exist

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ApiContractCatalog|Typed API Contract Convention" backend/src backend/tests docs/product
```
