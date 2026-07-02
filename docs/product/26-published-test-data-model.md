# Published Test Data Model

## Purpose

P2.6 stores TOEIC practice test structures after questions have been published.

Published tests define the containers for mini tests, part tests, skill tests, and full TOEIC practice tests. They do not store learner attempts or scoring results.

## Domain Model

Domain records:

- `PublishedTest`
- `PublishedTestSection`
- `PublishedTestItem`
- `PublishedTestRules`

Enums:

- `PublishedTestMode`
- `ToeicTestSectionType`

## Repository Contract

Repository methods:

- `UpsertPublishedTest`
- `GetPublishedTests`
- `UpsertPublishedTestSection`
- `GetPublishedTestSections`
- `UpsertPublishedTestItem`
- `GetPublishedTestItems`

Upserts are idempotent by test, section, and item id.

## Tables

SQLite local/test tables:

- `published_tests`
- `published_test_sections`
- `published_test_items`

PostgreSQL migration:

- `007_published_tests`

Indexes:

- `idx_published_tests_mode_status`
- `idx_published_test_sections_test_order`
- `idx_published_test_items_section_order`

## Data Rules

1. A published test has mode, title, target question count, duration, source trace, and status.
2. A test section belongs to a test and is rendered by display order.
3. A test item belongs to a section and is rendered by display order.
4. Full TOEIC tests must represent 200 questions.
5. Counts, durations, item order, and score weight must be positive.
6. Test items must reference TOEIC part 1-7.
7. Learner attempts and answers are not stored in this model.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "PublishedTest|007_published_tests|published_test" backend/src backend/tests docs/product
```
