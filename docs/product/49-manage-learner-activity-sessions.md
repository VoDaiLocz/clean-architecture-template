# Manage Learner Activity Sessions

## Task

P4.7 - Manage Learner Activity Sessions

## Purpose

Start, resume, complete, or abandon learner activity sessions with a controlled lifecycle so attempts and mastery always attach to durable work state.

## Detailed Scope

- Add session lifecycle commands and queries.
- Persist `ActivitySession` state.
- Enforce assignment ownership and status transitions.
- Return session payload required by lesson/drill/mini-test UI.
- Add resume behavior for active sessions.

## Out Of Scope

- Scoring attempts.
- Review item creation.
- Mini-test timer engine.
- Full frontend session UI.

## Data Contract

Table: `activity_sessions`.
Fields: `session_id`, `assignment_id`, `learner_id`, `activity_type`, `status`, `started_at_utc`, `last_seen_at_utc`, `completed_at_utc`, `abandoned_at_utc`.

## API Contract

Endpoints: `POST /api/learner/assignments/{assignmentId}/sessions/start`, `GET /api/learner/sessions/{sessionId}`, `POST /api/learner/sessions/{sessionId}/complete`, `POST /api/learner/sessions/{sessionId}/abandon`.
Errors: `ASSIGNMENT_NOT_FOUND`, `SESSION_NOT_OWNED`, `INVALID_SESSION_TRANSITION`.

## UI Contract

UI uses session status to render resume/complete states and does not create local fake sessions.

## Business Rules

1. Only assignment owner can start or complete a session.
2. Starting an assignment with an active session resumes it.
3. Completed and abandoned sessions are immutable except audit metadata.
4. Completion is allowed only when required activity evidence exists.
5. State transitions must be explicit and tested.

## Edge Cases

- Duplicate start.
- Complete already completed session.
- Abandon then resume.
- Assignment locked.
- Assignment belongs to another learner.
- Repository restart.

## Required Tests

- Start creates session.
- Duplicate start resumes.
- Invalid transitions reject.
- Completion persists timestamp.
- Ownership is enforced.
- Restart preserves session state.

## Acceptance Criteria

- Lifecycle state machine is durable and tested.
- API returns typed session contracts.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ActivitySession|StartActivitySession|INVALID_SESSION_TRANSITION" backend/src backend/tests docs/product
```

## Commit

`feat(p4.7): manage learner activity sessions`

## Push

`git push origin main`
