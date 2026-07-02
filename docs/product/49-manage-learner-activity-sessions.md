# Manage Learner Activity Sessions

## Purpose
P4.7 handles starting, resuming, and completing individual activity sessions.

## Domain Model
- `ActivitySession`
  - `SessionId` (string, UUID)
  - `AssignmentId` (string, UUID)
  - `LearnerId` (string, UUID)
  - `ActivityType` (Enum: `Lesson`, `Drill`, `MiniTest`)
  - `Status` (Enum: `InProgress`, `Completed`, `Abandoned`)
  - `StartedAtUtc` (DateTimeOffset)
  - `CompletedAtUtc` (DateTimeOffset, nullable)

## Repository Contract
- Interface: `IActivitySessionRepository`
  - `Task SaveActivitySessionAsync(ActivitySession session, CancellationToken cancellationToken);`
  - `Task<ActivitySession?> GetActivitySessionAsync(string sessionId, CancellationToken cancellationToken);`
- Table: `activity_sessions`
  ```sql
  CREATE TABLE activity_sessions (
      session_id TEXT PRIMARY KEY,
      assignment_id TEXT NOT NULL,
      learner_id TEXT NOT NULL,
      activity_type TEXT NOT NULL,
      status TEXT NOT NULL,
      started_at_utc TEXT NOT NULL,
      completed_at_utc TEXT,
      FOREIGN KEY (assignment_id) REFERENCES learner_assignments(assignment_id),
      FOREIGN KEY (learner_id) REFERENCES learner_profiles(learner_id)
  );
  ```

## Application Contract
- Handler: `StartActivitySessionHandler`, `CompleteActivitySessionHandler`
- Command: `StartActivitySessionCommand`, `CompleteActivitySessionCommand`
- Response: `StartActivitySessionResponse`, `CompleteActivitySessionResponse`

## API Contract
- Endpoints:
  - `POST /api/learner/assignments/{assignmentId}/sessions/start`
  - `POST /api/learner/sessions/{sessionId}/complete`

## Rules
1. Only the owner of the assignment can start or complete the session.
2. Allowed state transition is InProgress to Completed or Abandoned.
3. Starting an active in-progress session returns the active session idempotently.

## Verification
```bash
dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj
dotnet build backend/ToeicSystem.sln
rg -n "StartActivitySession|activity_sessions" backend/src backend/tests docs/product
```
