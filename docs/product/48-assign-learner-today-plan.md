# Assign Learner Today Plan

## Purpose
P4.6 determines the next primary activity assignment for the learner based on review priorities and path progress.

## Application Contract
- Handler: `GetLearnerTodayPlanHandler`
- Query: `GetLearnerTodayPlanQuery`
  - `LearnerId` (string)
- Response: `LearnerTodayPlanResponse`
  - `LearnerId` (string)
  - `PrimaryAssignment` (`LearnerAssignment`)
  - `Blockers` (List of `AssignmentBlocker`)
  - `Progress` (`PathProgress`)

## API Contract
- Endpoint: `GET /api/learner/today?learnerId={learnerId}`
- Response JSON (200 OK):
  ```json
  {
    "learnerId": "learner-101",
    "primaryAssignment": {
      "assignmentId": "assign-909",
      "assignmentType": "Review",
      "title": "Review mistakes in Word Forms",
      "contentRefId": "part5-word-form"
    },
    "blockers": [
      {
        "code": "BlockingReviewOpen",
        "message": "You must resolve open review items in this unit."
      }
    ],
    "progress": {
      "completedUnits": 2,
      "totalUnits": 10,
      "percentComplete": 20
    }
  }
  ```

## Rules
1. Active review items with `is_blocking = true` outrank new lessons.
2. Ongoing assignments in `Started` status are returned to resume.
3. Progression sequence: Lesson -> Guided Examples -> Focus Drills -> Mini Test.
4. If the path is empty, return a next action code `ContentUnavailable`.
5. Display progress metrics correctly using persisted path statistics.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "GetLearnerTodayPlan|/api/learner/today" backend/src backend/tests docs/product
```
