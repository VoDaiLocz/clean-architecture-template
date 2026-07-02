# Enforce Mastery Unlocks

## Purpose
P4.10 enforces locks and progress blocks based on learning unit completion and review status.

## Domain Model
- `MasteryRecord`
  - `MasteryRecordId` (string, UUID)
  - `LearnerId` (string, UUID)
  - `UnitId` (string)
  - `MasteryPercent` (int)
  - `IsUnlocked` (bool)
  - `BlockingReviewCount` (int)

## Repository Contract
- Interface: `IMasteryRepository`
  - `Task SaveMasteryRecordAsync(MasteryRecord record, CancellationToken cancellationToken);`
- Table: `mastery_records`
  ```sql
  CREATE TABLE mastery_records (
      mastery_record_id TEXT PRIMARY KEY,
      learner_id TEXT NOT NULL,
      unit_id TEXT NOT NULL,
      mastery_percent INTEGER NOT NULL,
      is_unlocked INTEGER NOT NULL,
      blocking_review_count INTEGER NOT NULL,
      updated_at_utc TEXT NOT NULL,
      FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
  );
  ```

## Application Contract
- Handler: `CheckMasteryUnlockHandler`
- Query: `CheckMasteryUnlockQuery`
- Response: `MasteryUnlockResponse`

## Rules
1. A unit cannot unlock if its prerequisite is not Completed.
2. Completion requires Lesson complete, Drill complete, MiniTest score >= threshold, and zero blocking reviews.
3. Resolving mistakes in repair sessions triggers unlock recalculations.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "CheckMasteryUnlock|mastery_records" backend/src backend/tests docs/product
```
