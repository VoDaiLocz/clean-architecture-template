# Generate Learner Path From Placement

## Purpose
P4.5 generates the active learning path of units ordered by the learner's weaknesses identified in placement.

## Domain Model
- `LearningPath`
  - `PathId` (string, UUID)
  - `LearnerId` (string, UUID)
  - `Status` (Enum: `Active`, `Archived`)
  - `CreatedAtUtc` (DateTimeOffset)
- `LearningPathUnit`
  - `UnitId` (string, UUID)
  - `PathId` (string, UUID)
  - `UnitKey` (string)
  - `DisplayOrder` (int)
  - `Status` (Enum: `Locked`, `Unlocked`, `Completed`)

## Repository Contract
- Interface: `ILearningPathRepository`
  - `Task SaveLearningPathAsync(LearningPath path, IEnumerable<LearningPathUnit> units, CancellationToken cancellationToken);`
  - `Task<LearningPath?> GetActiveLearningPathAsync(string learnerId, CancellationToken cancellationToken);`
- Table: `learning_paths`
  ```sql
  CREATE TABLE learning_paths (
      path_id TEXT PRIMARY KEY,
      learner_id TEXT NOT NULL,
      status TEXT NOT NULL,
      created_at_utc TEXT NOT NULL,
      FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
  );
  ```
- Table: `learning_path_units`
  ```sql
  CREATE TABLE learning_path_units (
      unit_id TEXT PRIMARY KEY,
      path_id TEXT NOT NULL,
      unit_key TEXT NOT NULL,
      display_order INTEGER NOT NULL,
      status TEXT NOT NULL,
      unlocked_at_utc TEXT,
      completed_at_utc TEXT,
      FOREIGN KEY (path_id) REFERENCES learning_paths(path_id)
  );
  ```

## Application Contract
- Handler: `GenerateLearningPathHandler`
- Command: `GenerateLearningPathCommand`
  - `LearnerId` (string)
- Response: `GenerateLearningPathResponse`
  - `PathId` (string)
  - `LearnerId` (string)
  - `FirstUnitId` (string)

## API Contract
- Endpoint: `POST /api/learner/path/generate`
- Request JSON:
  ```json
  { "learnerId": "learner-101" }
  ```
- Response JSON (200 OK):
  ```json
  {
    "pathId": "path-202",
    "learnerId": "learner-101",
    "status": "Active",
    "firstUnit": {
      "unitId": "unit-303",
      "unitKey": "part5-word-form",
      "title": "Part 5 Word Form"
    }
  }
  ```

## Rules
1. Generating a learning path requires a completed placement result.
2. Creating a path automatically archives any prior active path for that learner.
3. Units corresponding to High and Medium weakness tags are placed first.
4. Prerequisite rules in the catalog must be honored.
5. The first unit status is initialized as Unlocked; all others default to Locked.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "GenerateLearningPath|learning_paths" backend/src backend/tests docs/product
```
