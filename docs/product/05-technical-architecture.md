# Technical Architecture

## Architecture Standard

The backend follows Clean Architecture:

```text
Api -> Application -> Domain
Infrastructure -> Application abstractions
Domain has no dependency on Application, Infrastructure, or Api
```

Frontend must consume typed API contracts. It must not contain learner content, unlock logic, scoring logic, or fake production fallback data.

## Runtime Components

- React TypeScript web app
- ASP.NET Core API
- Application use-case layer
- Domain model and policies
- PostgreSQL production database
- Object storage for source and media assets
- Background worker for extraction and parsing
- Queue for long-running jobs when needed
- Observability stack for logs, metrics, traces

## Environment Strategy

Local:

- SQLite allowed for fast development tests
- local object storage emulator allowed

Staging:

- PostgreSQL
- object storage
- background worker enabled
- seeded representative content

Production:

- PostgreSQL
- managed object storage
- secret manager
- health checks
- structured logging
- backup and migration strategy

## Boundary Rules

- `ContentFactory` can read source and extraction tables.
- `LearnerJourney` can read only published content and learner state.
- `AttemptReview` can create attempts, review items, and mastery records.
- `Analytics` reads from stable read models or replicas.
- Admin APIs require admin authorization.
- Learner APIs cannot expose admin/source data.

