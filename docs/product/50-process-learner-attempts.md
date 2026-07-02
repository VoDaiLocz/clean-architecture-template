# Process Learner Attempts

## Task

P4.8 - Process Learner Attempts

## Purpose

Score learner drill/mini-test submissions on the backend, persist answers, and produce attempt results for review and mastery workflows.

## Detailed Scope

- Add `SubmitAttemptHandler`.
- Persist `LearnerAttempt` and `AttemptAnswer`.
- Validate session is active and owned.
- Score against published answer keys server-side.
- Mark parent session complete when the attempt is final.
- Emit data needed by review queue and mastery recalculation.

## Out Of Scope

- Placement scoring.
- Full exam scoring.
- Frontend answer-sheet UI.
- Manual answer-key editing.

## Data Contract

Tables: `learner_attempts`, `attempt_answers`.
Answers store learner answer, correct answer, correctness, skip state, skill tags, and source content ids. Correct answers are stored server-side and returned only in result/review mode.

## API Contract

`POST /api/learner/sessions/{sessionId}/attempts` accepts submitted answers.
Success returns attempt id, score percent, per-question result summary, review item count, and next action.
Errors: `SESSION_NOT_ACTIVE`, `ATTEMPT_ALREADY_SUBMITTED`, `QUESTION_NOT_IN_SESSION`, `ANSWER_REQUIRED`.

## UI Contract

UI sends answers and displays returned result. UI must not access correct answers before submit.

## Business Rules

1. Backend owns scoring.
2. Only one final attempt per final activity session unless activity type explicitly allows retries.
3. Correct answers are hidden until submit/review mode.
4. Wrong answers must be available for review queue creation.
5. Submitted answers are immutable.

## Edge Cases

- Duplicate submit.
- Partial answers where skips are not explicit.
- Question removed from published set after session start.
- Session already completed.
- Mixed part payloads.
- All skipped answers.

## Required Tests

- Correct scoring.
- Duplicate submit cached/rejected according to idempotency rule.
- Correct answer not exposed before submit.
- Wrong answer event data is produced.
- Parent session completes.

## Acceptance Criteria

- Attempts and answers persist.
- Review/mastery workflows can consume result data.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "SubmitAttempt|learner_attempts|attempt_answers|ATTEMPT_ALREADY_SUBMITTED" backend/src backend/tests docs/product
```

## Commit

`feat(p4.8): process learner attempts`

## Push

`git push origin main`
