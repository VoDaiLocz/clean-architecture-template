# Define TOEIC Item Contracts

## Task

P5.1 - Define TOEIC Item Contracts

## Purpose

Define learner-safe TOEIC item contracts so every part engine returns consistent payloads without leaking answer keys or admin-only source data.

## Detailed Scope

- Create separate contracts: `ToeicPlayableItem`, `ToeicResultItem`, `ToeicReviewItem`, and `AdminPublishedItem`.
- Add shared fields for part, prompt, choices, skill tags, media refs, group refs, and passage refs.
- Add part-specific extension payloads for Parts 1-7.
- Register typed API contracts.
- Add serialization tests and answer-leak tests.

## Out Of Scope

- Implementing individual part engines.
- Frontend rendering.
- Admin editor UI.
- Raw source extraction.

## Data Contract

Read models: `published_questions`, `published_question_groups`, `published_passages`, `published_media_assets`.
`ToeicPlayableItem` must not contain `CorrectAnswer`, explanation, admin notes, source row id, or raw Drive/PDF links.
`ToeicReviewItem` may include correct answer and explanation after an attempt result authorizes it.

## API Contract

Learner item payloads are retrieved through activity/session routes, e.g. `GET /api/learner/sessions/{sessionId}/items`.
Do not expose `GET /api/published-questions/{questionId}` as a learner play endpoint.
Errors: `ITEM_NOT_IN_SESSION`, `ITEM_NOT_PUBLISHED`, `ITEM_PAYLOAD_INVALID`.

## UI Contract

UI renders the payload returned by the learner activity/session API. UI must not infer hidden answers, media eligibility, or group relationships.

## Business Rules

1. Play mode never includes correct answer or explanation.
2. Review/result mode includes answer details only after the learner has submitted.
3. Parts 1-4 require audio/media refs according to part rules.
4. Parts 3/4 require group metadata.
5. Parts 6/7 require passage metadata.
6. Learner APIs never expose raw source manifest rows or source URLs.

## Edge Cases

- Direct question id lookup by learner.
- Attempt to request item outside session.
- Published question missing media.
- Review payload requested before submit.
- Admin payload accidentally serialized through learner endpoint.

## Required Tests

- Contract serialization tests.
- No-answer-leak play-mode test.
- Review payload includes answer after submit.
- Session scoping test.
- Part extension payload tests.

## Acceptance Criteria

- Item contracts are split by use case.
- Learner routes are session-scoped.
- Tests and build pass.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ToeicPlayableItem|ToeicReviewItem|ITEM_NOT_IN_SESSION|CorrectAnswer" backend/src backend/tests docs/product
```

## Commit

`feat(p5.1): define TOEIC item contracts`

## Push

`git push origin main`
