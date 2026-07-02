# Enforce Mastery Unlocks

## Task

P4.10 - Enforce Mastery Unlocks

## Purpose

Enforce progression gates so learners complete the required lesson, practice, mini-test, and repair work before the next unit unlocks.

## Detailed Scope

- Add mastery calculation service.
- Persist mastery records and unlock blocker state.
- Recalculate after lesson completion, attempt submit, mini-test completion, and review resolution.
- Enforce unlock checks in Today plan and part overview APIs.
- Return explicit locked reasons.

## Out Of Scope

- Adaptive ML scoring.
- Manual admin overrides.
- Frontend visual roadmap.
- Full practice-test repair plan logic.

## Data Contract

Tables: `mastery_records`, `unlock_blockers`.
Mastery stores unit completion percent, gate states, blocking review count, last recalculated timestamp, and next unlock candidate.

## API Contract

`GET /api/learner/units/{unitId}/mastery` returns gate state and locked reasons. Internal use cases expose `CanUnlockUnit` and `RecalculateMastery`.
Errors: `UNIT_NOT_IN_PATH`, `MASTERY_RECALCULATION_FAILED`.

## UI Contract

UI displays locked reasons returned by API. UI must not unlock units from client-side progress math.

## Business Rules

1. Unit unlock requires prerequisite unit completed.
2. Current unit completion requires lesson complete, drill threshold met, mini-test threshold met, and zero blocking reviews.
3. Review blockers outrank score progress.
4. Unlock state is derived server-side and persisted for audit.
5. Recalculation must be idempotent.

## Edge Cases

- Missing mastery record.
- Prerequisite not completed.
- Mini-test passed but blocking review remains.
- Review resolved after unit was locked.
- Duplicate recalculation events.
- Unit removed from catalog.

## Required Tests

- Each gate blocks correctly.
- Resolving blocker triggers unlock recalculation.
- Duplicate recalculation is idempotent.
- Today plan respects locked state.
- Part overview gets same locked reason.

## Acceptance Criteria

- Unlock behavior is backend-owned and deterministic.
- Locked reason is visible through APIs.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "Mastery|unlock_blockers|CanUnlockUnit|MASTERY" backend/src backend/tests docs/product
```

## Commit

`feat(p4.10): enforce mastery unlocks`

## Push

`git push origin main`
