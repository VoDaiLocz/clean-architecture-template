# Score TOEIC Placement

## Task

P4.4 - Score TOEIC Placement

## Purpose

Score a completed diagnostic placement submission server-side, produce a diagnostic score band, and record part/tag weaknesses that drive the first learning path.

## Detailed Scope

- Add `ScorePlacementSessionHandler` and typed command/response.
- Persist one `PlacementResult` per placement session.
- Persist `PlacementResultBreakdown` rows for TOEIC part and skill-tag dimensions.
- Validate that submitted answers match the assigned placement question set.
- Support explicit skipped answers.
- Return a backend-owned `NextAction` for learning-path generation.
- Add idempotency handling for repeated submit attempts.

## Out Of Scope

- Official TOEIC scaled score certification.
- Full practice-test scoring tables.
- Frontend placement screen.
- Learning path generation.
- Human review of placement items.

## Data Contract

Tables: `placement_results`, `placement_result_breakdowns`, and `placement_submission_fingerprints`.
Required result fields: `result_id`, `session_id`, `learner_id`, `correct_count`, `total_count`, `score_percent`, `diagnostic_score_band`, `estimated_score_min`, `estimated_score_max`, `completed_at_utc`.
`estimated_score_min/max` are diagnostic bands only until P6 score-table work exists; do not expose them as official TOEIC scores.

## API Contract

`POST /api/learner/placement/{sessionId}/submit` accepts `{ answers: [{ questionId, learnerAnswer | skipped }] }`.
Success returns result id, counts, percent, diagnostic score band, part breakdown, skill-tag breakdown, and `NextAction = GenerateLearningPath`.
Errors: `PLACEMENT_SESSION_NOT_FOUND`, `PLACEMENT_NOT_IN_PROGRESS`, `PLACEMENT_ANSWER_SET_INCOMPLETE`, `PLACEMENT_QUESTION_MISMATCH`, `PLACEMENT_IDEMPOTENCY_CONFLICT`.

## UI Contract

UI submits the assigned answers only once per active session and renders returned diagnostic feedback. UI must not calculate placement score, weakness severity, or next route locally.

## Business Rules

1. Only `InProgress` placement sessions accept a first submission.
2. Repeating the same submission for the same session returns the cached result.
3. Repeating with different answers after completion returns `PLACEMENT_IDEMPOTENCY_CONFLICT`.
4. Completed sessions are immutable.
5. TOEIC placement estimate uses declared diagnostic bands, not `percent * 9.9`.
6. Weakness severity: `<50 High`, `<75 Medium`, `<90 Low`, otherwise `None`.
7. Listening and Reading breakdowns must remain separate.

## Edge Cases

- Missing learner profile.
- Session not found.
- Duplicate identical submit.
- Duplicate conflicting submit.
- Skipped answers.
- Question not assigned to session.
- Empty placement question set.
- All answers incorrect or all skipped.

## Required Tests

- Scores correct/skipped answers.
- Persists exactly one result per session.
- Same payload returns cached result.
- Different payload after completion rejects.
- Part and skill breakdowns are persisted.
- Diagnostic score band mapping is tested with boundary values.
- No frontend scoring logic exists.

## Acceptance Criteria

- Handler, repository methods, endpoint, and typed contracts exist.
- Diagnostic score result is explicitly labelled as non-official estimate.
- Idempotency behavior is unambiguous and tested.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ScorePlacementSession|placement_results|diagnostic_score_band|PLACEMENT_IDEMPOTENCY_CONFLICT" backend/src backend/tests docs/product
```

## Commit

`feat(p4.4): score TOEIC placement`

## Push

`git push origin main`
