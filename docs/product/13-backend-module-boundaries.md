# Backend Module Boundaries

## Purpose

This document defines the backend module boundary target for the TOEIC platform.

The repository currently has broad Clean Architecture projects: `Domain`, `Application`, `Infrastructure`, and `Api`. P1 moves these projects toward explicit bounded-context ownership without breaking the existing app in one large rewrite.

## Dependency Direction

Allowed direction:

```text
Api -> Application -> Domain
Infrastructure -> Application abstractions
Infrastructure -> Domain only for persistence mapping/value use
Domain -> no Application, Infrastructure, or Api reference
```

Rules:

1. `Domain` owns business rules and cannot reference outer layers.
2. `Application` owns use cases and can reference `Domain`.
3. `Infrastructure` implements application abstractions and external technology.
4. `Api` maps HTTP contracts to application use cases.
5. Frontend calls API contracts only and never owns TOEIC business rules.

## Context Catalog

The production backend context catalog is:

- `ContentFactory`
- `LearningContent`
- `LearnerJourney`
- `AttemptReview`
- `AnalyticsOperations`

The catalog is represented in code by:

- `Domain.ModuleBoundaries.DomainContextCatalog`
- `Application.ModuleBoundaries.ApplicationContextCatalog`

Tests enforce that both catalogs match and that `Domain` does not reference `Application`, `Infrastructure`, or `Api`.

## Target Namespace Layout

```text
backend/src/Domain/ContentFactory
backend/src/Domain/LearningContent
backend/src/Domain/LearnerJourney
backend/src/Domain/AttemptReview
backend/src/Domain/AnalyticsOperations
backend/src/Application/ContentFactory
backend/src/Application/LearningContent
backend/src/Application/LearnerJourney
backend/src/Application/AttemptReview
backend/src/Application/AnalyticsOperations
backend/src/Infrastructure
backend/src/Api
```

Existing folders can remain during migration. New production work should use the target context vocabulary unless a task explicitly refactors old code.

## Enforcement

P1.1 enforcement:

- `Application.UnitTest` verifies the context catalogs match.
- `Application.UnitTest` verifies `Domain` has no forbidden outer-layer references.
- `dotnet build backend/ToeicSystem.sln` verifies project references remain valid.

Future P1/P2 tasks should add stronger analyzers or CI checks when module boundaries become physically separated by namespace and folder.
