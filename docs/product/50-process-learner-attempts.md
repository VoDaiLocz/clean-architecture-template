# Process Learner Attempts

## Purpose
P4.8 scores learner submissions and persists attempt answers.

## Domain Model
- `LearnerAttempt`
  - `AttemptId` (string, UUID)
  - `SessionId` (string, UUID)
  - `LearnerId` (string, UUID)
  - `CorrectCount` (int)
  - `TotalCount` (int)
  - `ScorePercent` (int)
  - `SubmittedAtUtc` (DateTimeOffset)
- `AttemptAnswer`
  - `AnswerId` (string, UUID)
  - `AttemptId` (string, UUID)
  - `QuestionId` (string, UUID)
  - `LearnerAnswer` (string)
  - `CorrectAnswer` (string)
  - `IsCorrect` (bool)

## Repository Contract
- Interface: `IAttemptRepository`
  - `Task SaveAttemptAsync(LearnerAttempt attempt, IEnumerable<AttemptAnswer> answers, CancellationToken cancellationToken);`
- Table: `learner_attempts`
  ```sql
  CREATE TABLE learner_attempts (
      attempt_id TEXT PRIMARY KEY,
      session_id TEXT NOT NULL,
      learner_id TEXT NOT NULL,
      correct_count INTEGER NOT NULL,
      total_count INTEGER NOT NULL,
      score_percent INTEGER NOT NULL,
      submitted_at_utc TEXT NOT NULL,
      FOREIGN KEY (session_id) REFERENCES activity_sessions(session_id),
      FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
  );
  ```
- Table: `attempt_answers`
  ```sql
  CREATE TABLE attempt_answers (
      answer_id TEXT PRIMARY KEY,
      attempt_id TEXT NOT NULL,
      question_id TEXT NOT NULL,
      learner_answer TEXT NOT NULL,
      correct_answer TEXT NOT NULL,
      is_correct INTEGER NOT NULL,
      FOREIGN KEY (attempt_id) REFERENCES learner_attempts(attempt_id)
  );
  ```

## Application Contract
- Handler: `SubmitAttemptHandler`
- Command: `SubmitAttemptCommand`
- Response: `SubmitAttemptResponse`

## API Contract
- Endpoint: `POST /api/learner/sessions/{sessionId}/attempts`

## Rules
1. Scoring and accuracy calculations are strictly backend-owned.
2. Submitting an attempt marks the parent session Completed.
3. Duplicate submissions for the same session ID return the cached attempt result.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "SubmitAttempt|learner_attempts" backend/src backend/tests docs/product
```
