# Placement Session Start

## Purpose

P4.3 creates the backend-owned start/resume point for TOEIC placement diagnosis.

This task does not score placement or assign placement questions yet. It establishes durable session state and duplicate active-session behavior.

## Domain Model

- `PlacementSession`
- `PlacementSessionStatus`

Statuses:

- `InProgress`
- `Completed`
- `Cancelled`

## Repository Contract

- `UpsertPlacementSession`
- `GetPlacementSessions`

Table:

- `placement_sessions`

## Application Contract

Handler:

- `StartPlacementSessionHandler`

Command:

- `StartPlacementSessionCommand`

Response:

- `StartPlacementSessionResponse`

## API Contract

Endpoint:

- `POST /api/learner/placement/start`

Rules:

1. Learner profile is required before placement.
2. First start creates an `InProgress` placement session.
3. Duplicate start while an active session exists returns the same session.
4. Duplicate active start returns next action `ResumePlacement`.
5. UI must use the returned session id and must not create local placement state.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "StartPlacementSession|placement_sessions|/api/learner/placement/start" backend/src backend/tests docs/product
```
