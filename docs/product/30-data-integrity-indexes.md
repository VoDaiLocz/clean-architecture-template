# Data Integrity And Indexes

## Purpose

P2.10 protects the production data foundation from orphan learner work and adds indexes for critical TOEIC platform queries.

This task closes P2 by proving invalid lifecycle rows are rejected and documenting the first production lookup indexes.

## Migration

PostgreSQL migration:

- `011_toeic_data_integrity`

Indexes:

- `idx_review_items_blocking_unlock`
- `idx_attempt_answers_question`
- `idx_published_questions_media`
- `idx_learner_attempts_learner_submitted`
- `idx_mastery_records_unlock_lookup`

## Integrity Rules

1. Learner assignments must reference existing learner profiles.
2. Learner attempts must reference existing activity sessions.
3. Repair attempts must reference existing review items.
4. Review blocker lookup must be indexed by learner, unit, blocking flag, and status.
5. Attempt answer analysis must be indexed by question and correctness.
6. Published question media/context validation must be indexed by TOEIC part and media/passage/group fields.
7. Learner attempt history must be indexed by learner and submitted timestamp.
8. Mastery unlock lookup must be indexed by learner, unit, unlock flag, and blocking review count.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "011_toeic_data_integrity|idx_review_items_blocking_unlock|RepositoryEnforcesToeicDataIntegrityAndIndexes" backend/src backend/tests docs/product
```
