# P6.8 Toeic Test Repair Plans Implementation Plan

## Task 1: Domain Models & Repository (Database)
- **Modify/Create `backend/src/Domain/Aggregates/LearnerProgress/ToeicRepairPlan.cs`**
  - Add enum `RepairPlanStatus` (Generated, Started, Submitted, Expired).
  - Add `ToeicRepairPlan` aggregate.
- **Modify `backend/src/Application/Common/Interfaces/Repositories/IKnowledgeRepository.cs`**
  - Add `UpsertToeicRepairPlan(ToeicRepairPlan plan)`
  - Add `GetToeicRepairPlan(string planId)`
  - Add `GetActiveRepairPlan(string learnerId)`
- **Modify `backend/src/Infrastructure/Data/SqliteKnowledgeRepository.cs`**
  - Implement repository methods via in-memory list for now.
- **Test in `backend/tests/Application.UnitTest/Program.cs`**
  - Verify models can be instantiated and persisted.

## Task 2: Generate Repair Plan Command
- **Create `backend/src/Application/Features/Learner/TestSessions/GenerateToeicRepairPlanHandler.cs`**
  - Accept `GenerateToeicRepairPlanCommand(string SessionId, string LearnerId)`
  - Load the `ToeicTestSessionState` associated with `SessionId`.
  - Gather all incorrect questions into `ReviewQuestionIds`.
  - Extract `SkillTags` from them.
  - Search `repository.GetPublishedQuestions()` for unseen questions with the same tags. Limit to 3-5 items to populate `DrillQuestionIds`.
  - Ensure any Part 3/4/6/7 questions bring their `GroupId` or `PassageId`.
  - Save `ToeicRepairPlan` with `Status = Generated`.
- **Test in `Application.UnitTest/CalculateToeicScoreBreakdownTests.cs` (or create a new test file)**

## Task 3: Session Flow (Start, Checkpoint, Submit)
- **Create `backend/src/Application/Features/Learner/TestSessions/StartToeicRepairPlanHandler.cs`**
  - Update `Status` to `Started` and set `StartedAtUtc`.
- **Create `backend/src/Application/Features/Learner/TestSessions/CheckpointToeicRepairPlanHandler.cs`**
  - Updates `Answers` map.
- **Create `backend/src/Application/Features/Learner/TestSessions/SubmitToeicRepairPlanHandler.cs`**
  - Updates `Status` to `Submitted`, sets `SubmittedAtUtc`.
- **Test the flow.**

## Task 4: Integration with Today Plan
- **Modify `backend/src/Application/Features/Learner/Work/GetLearnerTodayPlanHandler.cs`**
  - If `repository.GetActiveRepairPlan(learnerId)` returns a plan, set `ReadyForNewAssignment = false` and attach it as the `PrimaryAssignment`.
