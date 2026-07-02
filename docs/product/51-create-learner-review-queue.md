# Create Learner Review Queue

## Purpose
P4.9 records mistakes into the learner's review queue as review items.

## Domain Model
- `ReviewItem`
  - `ReviewItemId` (string, UUID)
  - `LearnerId` (string, UUID)
  - `SourceAttemptId` (string, UUID)
  - `QuestionId` (string, UUID)
  - `UnitId` (string)
  - `ErrorTag` (string)
  - `LearnerAnswer` (string)
  - `CorrectAnswer` (string)
  - `Status` (Enum: `Open`, `Resolved`)
  - `IsBlocking` (bool)

## Repository Contract
- Interface: `IReviewRepository`
  - `Task SaveReviewItemAsync(ReviewItem item, CancellationToken cancellationToken);`
  - `Task<IEnumerable<ReviewItem>> GetOpenReviewItemsAsync(string learnerId, CancellationToken cancellationToken);`
- Table: `review_items`
  ```sql
  CREATE TABLE review_items (
      review_item_id TEXT PRIMARY KEY,
      learner_id TEXT NOT NULL,
      source_attempt_id TEXT NOT NULL,
      question_id TEXT NOT NULL,
      unit_id TEXT NOT NULL,
      error_tag TEXT NOT NULL,
      learner_answer TEXT NOT NULL,
      correct_answer TEXT NOT NULL,
      status TEXT NOT NULL,
      is_blocking INTEGER NOT NULL,
      created_at_utc TEXT NOT NULL,
      resolved_at_utc TEXT,
      FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
  );
  ```

## Application Contract
- Handler: `GetLearnerReviewQueueHandler`
- Query: `GetLearnerReviewQueueQuery`
- Response: `LearnerReviewQueueResponse`

## API Contract
- Endpoint: `GET /api/learner/review?learnerId={learnerId}`

## Rules
1. Every wrong answer in graded activities creates an open ReviewItem.
2. Mistakes in MiniTests and official practice exams are set as `is_blocking = true`.
3. Mistakes in focus drills are set as `is_blocking = false`.
4. Duplicate errors for the same question update the attempt reference.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "GetLearnerReviewQueue|review_items" backend/src backend/tests docs/product
```
