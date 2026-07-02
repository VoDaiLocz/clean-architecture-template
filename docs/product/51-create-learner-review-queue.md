# Create Learner Review Queue

## Task

P4.9 - Create Learner Review Queue

## Purpose

Turn wrong answers into durable repair work so the learner cannot ignore core mistakes that block TOEIC progress.

## Detailed Scope

- Add review item creation service called after attempts/tests.
- Add `GetLearnerReviewQueueHandler`.
- Persist review item status, source attempt, question, skill tag, explanation reference, and blocking flag.
- Support resolving a review item through repair attempts.
- Deduplicate repeated mistakes while preserving latest evidence.

## Out Of Scope

- Spaced repetition algorithm beyond first blocking repair.
- Teacher comments.
- Frontend review workspace.
- Full mastery recalculation implementation.

## Data Contract

Table: `review_items`.
Fields: `review_item_id`, `learner_id`, `source_attempt_id`, `question_id`, `unit_id`, `skill_tag`, `learner_answer`, `correct_answer`, `status`, `is_blocking`, `created_at_utc`, `resolved_at_utc`.

## API Contract

`GET /api/learner/review?learnerId=...` returns open review items grouped by unit and severity.
`POST /api/learner/review/{reviewItemId}/resolve` records repair result.
Errors: `REVIEW_ITEM_NOT_FOUND`, `REVIEW_ITEM_NOT_OWNED`, `REPAIR_NOT_PASSED`.

## UI Contract

UI shows review queue from API and sends repair answers. UI must not locally mark blockers resolved.

## Business Rules

1. Every wrong graded answer creates or updates one review item.
2. Mini-test and practice-test mistakes are blocking by default.
3. Drill mistakes are non-blocking unless the unit rule says otherwise.
4. Blocking review items prevent gated unit unlock.
5. Resolution requires a passed repair attempt, not a button click.

## Edge Cases

- Same question missed multiple times.
- Source attempt deleted or archived.
- Review item for unpublished question.
- Learner attempts another learner's review item.
- Repair fails.
- Already resolved item.

## Required Tests

- Wrong answers create review items.
- Duplicate mistake updates existing item.
- Blocking flags match activity type.
- Resolve requires passed repair.
- Queue groups and orders items correctly.

## Acceptance Criteria

- Review queue exists as durable backend state.
- Unlock engine can query blockers.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ReviewItem|GetLearnerReviewQueue|REPAIR_NOT_PASSED|is_blocking" backend/src backend/tests docs/product
```

## Commit

`feat(p4.9): create learner review queue`

## Push

`git push origin main`
