# Learner Work Lifecycle Data Model

## Purpose

P2.8 stores the learner work lifecycle: assigned work, activity sessions, submitted attempts, and answer rows.

This model is the audit trail for learner work. It does not create review items or mastery records; those are modeled in P2.9.

## Domain Model

Domain records:

- `LearnerAssignment`
- `ActivitySession`
- `LearnerAttempt`
- `AttemptAnswer`
- `LearnerWorkRules`

Enums:

- `LearnerAssignmentType`
- `LearnerAssignmentStatus`
- `ActivitySessionStatus`
- `LearnerAttemptStatus`

## Repository Contract

Repository methods:

- `UpsertLearnerAssignment`
- `GetLearnerAssignments`
- `UpsertActivitySession`
- `GetActivitySessions`
- `UpsertLearnerAttempt`
- `GetLearnerAttempts`
- `UpsertAttemptAnswer`
- `GetAttemptAnswers`

## Tables

SQLite local/test tables:

- `learner_assignments`
- `activity_sessions`
- `learner_attempts`
- `attempt_answers`

PostgreSQL migration:

- `009_learner_assignments_attempts`

Indexes:

- `idx_learner_assignments_learner_status`
- `idx_activity_sessions_assignment`
- `idx_learner_attempts_session`
- `idx_attempt_answers_attempt`

## Data Rules

1. Assignments belong to learner profiles.
2. Activity sessions belong to assignments and learner profiles.
3. Attempts belong to activity sessions and learner profiles.
4. Attempt answers belong to attempts.
5. Attempt total count must be positive.
6. Attempt correct count must be between zero and total count.
7. Attempt score percent must be 0-100.
8. Review creation and mastery updates are separate P2.9 responsibilities.

## Verification

```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "LearnerAssignment|ActivitySession|LearnerAttempt|AttemptAnswer|009_learner_assignments_attempts" backend/src backend/tests docs/product
```
