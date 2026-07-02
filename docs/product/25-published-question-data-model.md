# Published Question Data Model

## Purpose

P2.5 stores learner-ready TOEIC questions after content has passed validation/review.

Published questions are the canonical question source for drills, mini tests, and future TOEIC practice modes. They are separate from parser drafts and extracted PDF blocks.

## Domain Model

Domain records:

- `PublishedQuestion`
- `PublishedQuestionType`
- `PublishedQuestionRules`

Question types:

- `SingleQuestion`
- `ConversationSet`
- `TalkSet`
- `PassageSet`

## Repository Contract

Repository methods:

- `UpsertPublishedQuestion`
- `GetPublishedQuestions`

Upserts are idempotent by question id.

## Tables

SQLite local/test table:

- `published_questions`

PostgreSQL migration:

- `006_published_questions`

Indexes:

- `idx_published_questions_part_status`
- `idx_published_questions_lesson`

## Data Rules

1. A published question belongs to a published lesson.
2. TOEIC part must be between 1 and 7.
3. Every published question requires prompt, options, correct answer, explanation, evidence, source trace, skill tags, and status.
4. Part 1 questions require media.
5. Part 3 and Part 4 questions require a group relationship.
6. Part 6 and Part 7 questions require passage context.
7. Learner-facing practice must read from published questions, not draft content.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "PublishedQuestion|006_published_questions|published_questions" backend/src backend/tests docs/product
```
