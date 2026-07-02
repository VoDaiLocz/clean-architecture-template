# Review And Mastery Data Model

## Purpose

P2.9 stores durable review work and mastery state.

This model makes wrong answers auditable, repairable, and usable for unlock decisions. It does not calculate mastery automatically; later application services will update these records from attempts and review outcomes.

## Domain Model

Domain records:

- `ReviewItem`
- `RepairAttempt`
- `MasteryRecord`
- `ReviewMasteryRules`

Enum:

- `ReviewItemStatus`

## Repository Contract

Repository methods:

- `UpsertReviewItem`
- `GetReviewItems`
- `UpsertRepairAttempt`
- `GetRepairAttempts`
- `UpsertMasteryRecord`
- `GetMasteryRecord`

## Tables

SQLite local/test tables:

- `review_items`
- `repair_attempts`
- `mastery_records`

PostgreSQL migration:

- `010_review_mastery_records`

Indexes:

- `idx_review_items_learner_status`
- `idx_repair_attempts_review`
- `idx_mastery_records_learner_unit`

## Data Rules

1. Review items belong to learner profiles.
2. Review items preserve source attempt trace, question id, unit id, error tag, learner answer, and correct answer.
3. Blocking review items prevent gated unlocks until resolved.
4. Repair attempts belong to review items.
5. Mastery records are unique by learner and unit.
6. Mastery percent must be 0-100.
7. Blocking review count cannot be negative.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ReviewItem|RepairAttempt|MasteryRecord|010_review_mastery_records" backend/src backend/tests docs/product
```
