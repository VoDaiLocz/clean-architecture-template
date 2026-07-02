# Generate TOEIC Test Repair Plans

## Task

P6.8 - Generate TOEIC Test Repair Plans

## Purpose

Convert practice-test mistakes into targeted repair assignments before the learner continues new progression.

## Detailed Scope

- Add ToeicRepairPlan domain/read model support.
- Compose eligible published content from DB, not source files.
- Start/resume/submit sessions through typed commands.
- Persist answer state, timing state, and final result references.
- Emit events for weakness tagging, review queue, and mastery/repair flows where applicable.

## Out Of Scope

- Frontend exam UI unless this task explicitly says UX.
- Official certificate scoring.
- Raw source extraction.
- Admin content approval.

## Data Contract

Tables/read models must store session id, learner id, test type, content blueprint/version, status, started/submitted/expired timestamps, question assignments, answer state, and result id. `ToeicRepairPlan` is durable and survives restart.

## API Contract

Primary endpoint: `POST /api/learner/practice-tests/sessions/{sessionId}/repair-plan`. Related session endpoints must use `/api/learner/practice-tests/sessions/{sessionId}/...`. Errors include `TEST_CONTENT_UNAVAILABLE`, `TEST_SESSION_NOT_OWNED`, `TEST_SESSION_EXPIRED`, `TEST_ALREADY_SUBMITTED`.

## UI Contract

UI consumes session/result APIs and renders exam constraints returned by backend. UI must not own timer authority, scoring, or repair-plan creation.

## Business Rules

1. Repair plan generation uses persisted test result breakdown and review items.
2. Only published, validated content can be assigned.
3. Question assignment is frozen at session start.
4. Final submit is immutable.
5. Result data feeds weakness/review/repair flows.
6. Expiration behavior is explicit and tested.

## Edge Cases

- Insufficient published content.
- Missing required audio/passage.
- Resume after refresh.
- Submit after expiration.
- Duplicate final submit.
- Learner accesses another learner's session.
- Content unpublished after session start.

## Required Tests

- Starts session with correct blueprint.
- Rejects insufficient content.
- Persists assignments and answers.
- Resumes after repository restart.
- Enforces expiration/final submit.
- Emits downstream review/weakness data where applicable.

## Acceptance Criteria

- Practice-test behavior is deterministic, durable, and backend-owned.
- Tests and build pass.
- No raw source/PDF link is exposed as the learner experience.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ToeicRepairPlan|practice-tests|TEST_SESSION" backend/src backend/tests docs/product
```

## Commit

`feat(p6.8): generate TOEIC test repair plans`

## Push

`git push origin main`
