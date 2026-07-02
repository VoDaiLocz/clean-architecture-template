# Learner Profile Data Model

## Purpose

P2.7 stores durable learner profile data for personalization and future learner journey decisions.

Learner profiles are not authentication accounts. They are the learning product profile: TOEIC goal, estimated score, study settings, and lifecycle status.

## Domain Model

Domain records:

- `LearnerProfile`
- `LearnerProfileStatus`

Statuses:

- `Active`
- `Suspended`
- `Deleted`

## Repository Contract

Repository methods:

- `UpsertLearnerProfile`
- `GetLearnerProfile`

Upserts are idempotent by learner id.

## Tables

SQLite local/test table:

- `learner_profiles`

PostgreSQL migration:

- `008_learner_profiles`

Indexes:

- `idx_learner_profiles_status`

## Data Rules

1. Learner id, display name, email, and timezone are required.
2. Target TOEIC score must be within TOEIC score bounds.
3. Current estimated TOEIC score must be within TOEIC score bounds.
4. Daily study minutes must be positive.
5. A learner profile must survive repository restart.
6. Authentication, sessions, placement results, assignments, and mastery are separate models.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "LearnerProfile|008_learner_profiles|learner_profiles" backend/src backend/tests docs/product
```
