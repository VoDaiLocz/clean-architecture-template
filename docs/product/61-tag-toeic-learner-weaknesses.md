# Tag TOEIC Learner Weaknesses

## Task

P5.9 - Tag TOEIC Learner Weaknesses

## Purpose

Convert attempts and test results into stable learner weakness signals that drive review, Today plan priority, and future learning-path adjustments.

## Detailed Scope

- Add weakness tagging service.
- Aggregate errors by TOEIC part, skill tag, question type, and source unit.
- Persist current weakness profile and historical events.
- Weight recent and blocking mistakes more heavily.
- Expose backend-owned weakness summary read model.

## Out Of Scope

- ML personalization.
- Teacher-authored notes.
- Frontend charts.
- Official score conversion.

## Data Contract

Tables: `learner_weakness_events`, `learner_weakness_summaries`.
Events store attempt/test source, part, skill tag, weight, correctness, and timestamp. Summaries store current severity and evidence count.

## API Contract

Internal event consumers handle attempt/test completion. `GET /api/learner/weaknesses?learnerId=...` returns summaries for progress UI.
Errors: `LEARNER_PROFILE_REQUIRED`.

## UI Contract

UI displays weakness summaries only. UI must not compute severity locally.

## Business Rules

1. Incorrect answers increase weakness weight.
2. Correct repair attempts decrease severity but do not delete history.
3. Skill tags must come from published question metadata.
4. Severity thresholds are deterministic and versioned.
5. Listening and Reading part dimensions remain separate.

## Edge Cases

- Missing skill tag.
- Duplicate attempt event.
- Repair correct after repeated mistakes.
- Question metadata changed after attempt.
- Empty weakness profile.

## Required Tests

- Attempt event creates weakness summary.
- Duplicate event is idempotent.
- Repair reduces severity.
- Missing tag records validation issue.
- Summary endpoint returns stable ordering.

## Acceptance Criteria

- Weakness profile is durable and deterministic.
- Today/path/test repair flows can consume it.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "Weakness|learner_weakness|TagLearnerWeakness" backend/src backend/tests docs/product
```

## Commit

`feat(p5.9): tag TOEIC learner weaknesses`

## Push

`git push origin main`
