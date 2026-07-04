# TOEIC Test Repair Plans Design (Phase P6.8)

## 1. Purpose
Convert practice-test mistakes into targeted repair assignments (Repair Plans) before the learner can continue new progression. This ensures that learners address their weaknesses immediately instead of ignoring them. This acts as a **Hard Blocker** in the Learner Today Plan.

## 2. Architecture & Data Models

A new model `ToeicRepairPlan` is introduced to manage the state and context of the repair task.

```csharp
public enum RepairPlanStatus
{
    Generated,
    Started,
    Submitted,
    Expired
}

public sealed record ToeicRepairPlan(
    string RepairPlanId,
    string SourceSessionId,
    string LearnerId,
    RepairPlanStatus Status,
    IReadOnlyList<string> ReviewQuestionIds,
    IReadOnlyList<string> DrillQuestionIds,
    IReadOnlyDictionary<string, string> Answers,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset? ExpiredAtUtc
);
```

### 2.1 Storage
- Add `UpsertToeicRepairPlan(plan)` and `GetToeicRepairPlan(planId)` to `IKnowledgeRepository`.
- Map to a `ToeicRepairPlans` table in the database schema.

## 3. Algorithm: Review + Micro-drills

When generating a Repair Plan, the algorithm will:
1. Load the submitted `ToeicTestSessionState` or `ToeicScoreBreakdown`.
2. Extract all incorrectly answered `QuestionId`s and store them in `ReviewQuestionIds`.
3. Extract the `SkillTags` from these incorrect questions.
4. Query the `PublishedQuestions` repository to find 3-5 new, unseen questions that match the extracted `SkillTags`.
5. Ensure structural integrity: If a selected drill question belongs to a Group (Part 3/4) or Passage (Part 6/7), the backend must pull the entire group/passage context so the learner has the necessary audio/text to answer it.
6. Store the newly discovered IDs in `DrillQuestionIds`.

## 4. API Endpoints

Following the CQS pattern and RESTful URL structure tied to the source session:

1. **Generate Plan**
   `POST /api/learner/practice-tests/sessions/{sessionId}/repair-plan`
   Handler: `GenerateToeicRepairPlanHandler`
   Returns the generated `RepairPlanId`.

2. **Start Plan**
   `POST /api/learner/practice-tests/sessions/{sessionId}/repair-plan/start`
   Handler: `StartToeicRepairPlanHandler`
   Sets `StartedAtUtc` and timer constraints.

3. **Checkpoint (Draft Answers)**
   `POST /api/learner/practice-tests/sessions/{sessionId}/repair-plan/checkpoint`
   Handler: `CheckpointToeicRepairPlanHandler`
   Saves partial answers to prevent data loss.

4. **Submit Plan**
   `POST /api/learner/practice-tests/sessions/{sessionId}/repair-plan/submit`
   Handler: `SubmitToeicRepairPlanHandler`
   Finalizes the plan, recalculates mastery, and removes the hard blocker from the Today Plan.

## 5. Integration with Today Plan
Modify `GetLearnerTodayPlanHandler`:
If `IKnowledgeRepository` returns an active (non-submitted) `ToeicRepairPlan` for the learner, it MUST be set as the `PrimaryAssignment` with `ReadyForNewAssignment = false`. The learner cannot proceed until it is submitted.

## 6. Edge Cases
- **Insufficient published content:** If the DB lacks new questions for a specific skill tag, the algorithm gracefully falls back to just the `ReviewQuestionIds`.
- **Test Session Not Owned/Expired:** Handled by standard validation guards throwing `InvalidOperationException`.
- **Idempotency:** Re-calling the Generate endpoint on an already generated repair plan returns the existing `RepairPlanId`.
