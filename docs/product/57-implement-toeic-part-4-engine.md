# Implement TOEIC Part 4 Engine

## Task

P5.5 - Implement TOEIC Part 4 Engine

## Purpose

Implement short-talk group behavior with shared talk audio and grouped child questions.

## Detailed Scope

- Add part engine interfaces and validators.
- Load published content through read models, not raw source files.
- Validate media/passage/group requirements before learner payload creation.
- Return learner-safe payloads for play mode and separate result/review payloads after submit.
- Add contract tests per part.

## Out Of Scope

- Parsing raw PDFs/audio from source materials.
- Admin draft review.
- Frontend rendering implementation.
- Official test scoring.

## Data Contract

Published read models must contain all media, passage, group, answer key, explanation, and skill-tag fields required by this part. Learner play payload excludes hidden answer fields.

## API Contract

Payload is returned through learner activity/session item APIs defined by P5.1. Part validation failures return stable content-invalid errors and are not silently skipped.

## UI Contract

UI renders the payload returned by the learner activity/session API. UI must not infer hidden answers, media eligibility, or group relationships.

## Business Rules

1. Part 4 group requires talk audio.
2. Each talk group has three child questions unless catalog explicitly marks a legacy exception.
3. Talk evidence and transcript align to group id.
4. Missing group relation blocks publish.

## Edge Cases

- Missing required media/passage/group relation.
- Invalid number of choices.
- Draft/admin content accidentally requested.
- Review mode requested before submit.
- Legacy source with incomplete extraction evidence.

## Required Tests

- Valid payload is produced for the part.
- Missing required media/passage/group relation fails.
- Play-mode payload hides correct answer.
- Review/result payload includes explanations only after submit.
- Contract serialization remains stable.

## Acceptance Criteria

- Part engine validates content requirements.
- Learner payload is safe and complete.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "Part4Engine|TalkGroup|published_question_groups" backend/src backend/tests docs/product
```

## Commit

`feat(p5.5): implement TOEIC Part 4 engine`

## Push

`git push origin main`
