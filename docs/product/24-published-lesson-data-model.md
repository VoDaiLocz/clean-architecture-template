# Published Lesson Data Model

## Purpose

P2.4 stores learner-ready lessons and guided examples after content has passed validation/review.

Published lessons are the first learner curriculum data model. They are separate from draft content and parser output.

## Domain Model

Domain records:

- `PublishedLesson`
- `GuidedExample`
- `PublishedContentStatus`

Statuses:

- `Published`
- `Archived`

## Repository Contract

Repository methods:

- `UpsertPublishedLesson`
- `GetPublishedLessons`
- `UpsertGuidedExample`
- `GetGuidedExamples`

Upserts are idempotent by lesson/example id.

## Tables

SQLite local/test tables:

- `published_lessons`
- `guided_examples`

PostgreSQL migration:

- `005_published_lessons`

Indexes:

- `idx_published_lessons_unit_status`
- `idx_guided_examples_lesson_order`

## Data Rules

1. A published lesson belongs to a learning unit id.
2. A lesson contains TOEIC part, title, objective, skill tags, source trace, and status.
3. A guided example belongs to a lesson.
4. Guided examples are ordered by display order.
5. Learner-facing curriculum must read from published lesson content, not draft content.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "PublishedLesson|GuidedExample|005_published_lessons|published_lessons" backend/src backend/tests docs/product
```
