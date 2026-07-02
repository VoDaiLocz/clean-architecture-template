# Score TOEIC Placement

## Purpose
P4.4 scores the submitted placement answers server-side, estimates the overall TOEIC score, and details the part and skill-tag weaknesses.

## Domain Model
- `PlacementResult`: represents the score summary of a diagnostic placement session.
  - `ResultId` (string, UUID)
  - `SessionId` (string, UUID)
  - `LearnerId` (string, UUID)
  - `CorrectCount` (int)
  - `TotalCount` (int)
  - `ScorePercent` (int, 0-100)
  - `EstimatedScore` (int, 10-990)
  - `CompletedAtUtc` (DateTimeOffset)
- `PlacementResultBreakdown`: details accuracy per part/tag.
  - `BreakdownId` (string, UUID)
  - `ResultId` (string, UUID)
  - `DimensionType` (Enum: `ToeicPart`, `SkillTag`)
  - `DimensionKey` (string, e.g., "Part1", "grammar_tense")
  - `CorrectCount` (int)
  - `TotalCount` (int)
  - `AccuracyPercent` (int)
  - `WeaknessSeverity` (Enum: `None`, `Low`, `Medium`, `High`)

## Repository Contract
- Interface: `IPlacementRepository`
  - `Task SavePlacementResultAsync(PlacementResult result, IEnumerable<PlacementResultBreakdown> breakdowns, CancellationToken cancellationToken);`
  - `Task<PlacementResult?> GetPlacementResultBySessionIdAsync(string sessionId, CancellationToken cancellationToken);`
- Table: `placement_results`
  ```sql
  CREATE TABLE placement_results (
      result_id TEXT PRIMARY KEY,
      session_id TEXT NOT NULL UNIQUE,
      learner_id TEXT NOT NULL,
      correct_count INTEGER NOT NULL,
      total_count INTEGER NOT NULL,
      score_percent INTEGER NOT NULL,
      estimated_score INTEGER NOT NULL,
      completed_at_utc TEXT NOT NULL,
      FOREIGN KEY (session_id) REFERENCES placement_sessions(session_id),
      FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
  );
  ```
- Table: `placement_result_breakdowns`
  ```sql
  CREATE TABLE placement_result_breakdowns (
      breakdown_id TEXT PRIMARY KEY,
      result_id TEXT NOT NULL,
      dimension_type TEXT NOT NULL,
      dimension_key TEXT NOT NULL,
      correct_count INTEGER NOT NULL,
      total_count INTEGER NOT NULL,
      accuracy_percent INTEGER NOT NULL,
      weakness_severity TEXT NOT NULL,
      FOREIGN KEY (result_id) REFERENCES placement_results(result_id)
  );
  ```

## Application Contract
- Handler: `ScorePlacementSessionHandler`
- Command: `ScorePlacementSessionCommand`
  - `SessionId` (string, UUID)
  - `Answers` (List of `SubmittedAnswer`)
- Response: `ScorePlacementSessionResponse`
  - `ResultId` (string)
  - `SessionId` (string)
  - `CorrectCount` (int)
  - `TotalCount` (int)
  - `ScorePercent` (int)
  - `EstimatedScore` (int)
  - `NextAction` (`LearnerNextAction`)

## API Contract
- Endpoint: `POST /api/learner/placement/{sessionId}/submit`
- Request JSON:
  ```json
  {
    "answers": [
      { "questionId": "q-1", "learnerAnswer": "A" },
      { "questionId": "q-2", "learnerAnswer": "C" }
    ]
  }
  ```
- Response JSON (200 OK):
  ```json
  {
    "resultId": "res-101",
    "sessionId": "sess-202",
    "correctCount": 18,
    "totalCount": 20,
    "scorePercent": 90,
    "estimatedScore": 750,
    "nextAction": {
      "actionCode": "GeneratePath",
      "route": "/api/learner/path/generate",
      "description": "Generate your learning path."
    }
  }
  ```

## Rules
1. Only `InProgress` placement sessions can be submitted. If status is `Completed`, return HTTP 400 with code `SESSION_ALREADY_COMPLETED`.
2. All assigned questions must receive answers or explicit skip states.
3. Repeating placement submission returns the cached result idempotently.
4. Scorings, part accuracies, and tag weaknesses are strictly backend-owned.
5. Estimated TOEIC score is calculated via: `EstimatedScore = Math.Clamp(Round(Percent * 9.9), 10, 990)`.
6. Weakness severity threshold: Accuracy < 50% => High; < 75% => Medium; < 90% => Low; else None.
7. Next action returned is always pointing to path generation.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "ScorePlacementSession|placement_results" backend/src backend/tests docs/product
```
