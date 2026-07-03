# P4.10 Mastery Logic Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix the business logic for learner progression so that failed tests block progression, completed units correctly unlock the next unit, the Today Plan logically sequences work, and production database migrations/API contracts are complete.

**Architecture:** Pure business logic fixes. We will encapsulate passing thresholds into a new `MasteryPolicy`, adjust the calculation logic in `MasteryCalculationService` to honor the policy, correctly compute the "Unlocked" state for subsequent units, and update the Today Plan generator to suggest Drill/MiniTest based on progressive completion. No architectural refactoring to Domain Events yet.

**Tech Stack:** C#, ASP.NET Core, SQLite (local tests), PostgreSQL (migrations).

---

### Task 1: Create MasteryPolicy

**Files:**
- Create: `backend/src/Domain/Policies/MasteryPolicy.cs`

- [ ] **Step 1: Write the failing test or create the file**
Create the static policy class that defines passing rules.

```csharp
namespace Domain.Policies;

using Domain.Aggregates.LearnerProgress;

public static class MasteryPolicy
{
    public static bool IsPassed(LearnerAssignmentType type, int scorePercent)
    {
        return type switch
        {
            LearnerAssignmentType.Lesson => true, // Lessons don't have scores, just completion
            LearnerAssignmentType.Drill => scorePercent >= 80,
            LearnerAssignmentType.MiniTest => scorePercent >= 80,
            LearnerAssignmentType.PartTest => scorePercent >= 80,
            LearnerAssignmentType.FullTest => scorePercent >= 80,
            _ => false
        };
    }
}
```

- [ ] **Step 2: Commit**

```bash
git add backend/src/Domain/Policies/MasteryPolicy.cs
git commit -m "feat(domain): add MasteryPolicy with passing thresholds"
```

### Task 2: Fix MasteryCalculationService (Issue 1 & Issue 2)

**Files:**
- Modify: `backend/src/Application/Features/Learner/Mastery/MasteryCalculationService.cs`

- [ ] **Step 1: Update calculation logic to use MasteryPolicy and unlock next unit**
In `MasteryCalculationService`, locate the code calculating if an assignment meets the requirement. Update it to check the latest attempt score against `MasteryPolicy.IsPassed()`. Also, if the current unit is fully completed (no blockers), find the next unit in the path and mark it `Unlocked`.

- [ ] **Step 2: Run existing tests**
Run `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj` to ensure nothing is fundamentally broken yet (though some old tests might need adjustment).

- [ ] **Step 3: Commit**

```bash
git add backend/src/Application/Features/Learner/Mastery/MasteryCalculationService.cs
git commit -m "fix(mastery): enforce thresholds and unlock next unit"
```

### Task 3: Fix GetLearnerMasteryHandler (Issue 6)

**Files:**
- Modify: `backend/src/Application/Features/Learner/Mastery/GetLearnerMasteryHandler.cs`

- [ ] **Step 1: Fix 404 behavior**
Change the logic so that if the mastery record is null, the handler calls `MasteryCalculationService.RecalculateMastery(query.LearnerId, query.UnitId)` and returns the freshly calculated state, rather than throwing an error.

- [ ] **Step 2: Commit**

```bash
git add backend/src/Application/Features/Learner/Mastery/GetLearnerMasteryHandler.cs
git commit -m "fix(mastery): recalculate on read if mastery record missing"
```

### Task 4: Fix GetLearnerTodayPlanHandler (Issue 3)

**Files:**
- Modify: `backend/src/Application/Features/Learner/Work/GetLearnerTodayPlanHandler.cs`

- [ ] **Step 1: Implement progressive priority logic**
Modify `Handle` to sequence work:
1. Blocking Review Items (if any).
2. Active (In Progress) Assignments.
3. If no Lesson exists or is complete -> create/return Lesson.
4. If Lesson complete but Drill not passed -> create/return Drill.
5. If Drill passed but MiniTest not passed -> create/return MiniTest.
6. Only unlock next unit if MiniTest passed.

- [ ] **Step 2: Commit**

```bash
git add backend/src/Application/Features/Learner/Work/GetLearnerTodayPlanHandler.cs
git commit -m "fix(learning): sequence Today Plan by progressive logic"
```

### Task 5: Add PostgreSQL Migration for unlock_blockers (Issue 5)

**Files:**
- Modify: `backend/src/DatabaseMigrations/PostgresMigrationCatalog.cs`

- [ ] **Step 1: Add `unlock_blockers` DDL**
Find the migration that sets up mastery or learner progress, and append the `unlock_blockers` table creation script.

```sql
CREATE TABLE IF NOT EXISTS unlock_blockers (
    blocker_id varchar(160) PRIMARY KEY,
    learner_id varchar(160) NOT NULL REFERENCES learner_profiles(learner_id),
    unit_id varchar(160) NOT NULL,
    reason varchar(160) NOT NULL,
    created_at_utc timestamptz NOT NULL
);

CREATE INDEX IF NOT EXISTS idx_unlock_blockers_learner_unit
    ON unlock_blockers(learner_id, unit_id);
```

- [ ] **Step 2: Commit**

```bash
git add backend/src/DatabaseMigrations/PostgresMigrationCatalog.cs
git commit -m "feat(db): add PostgreSQL migration for unlock_blockers"
```

### Task 6: Add API Contract for Mastery (Issue 4)

**Files:**
- Modify: `backend/src/Application/Common/ApiContracts/ApiContractCatalog.cs`

- [ ] **Step 1: Add the route definition**
Inside `GetLearnerRoutes()` or similar section, add:
`"GET /api/learner/units/{unitId}/mastery" -> "LearnerMasteryResponse"` (adapt to match existing syntax).

- [ ] **Step 2: Verify the API test passes**
Run the unit test `ApiContractCatalogDefinesStableTypedRoutes`.
Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add backend/src/Application/Common/ApiContracts/ApiContractCatalog.cs
git commit -m "fix(api): document mastery API contract"
```

### Task 7: End-to-End User Flow Tests

**Files:**
- Modify: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write failed Mini-Test flow**
Create a test method `FailedMiniTestKeepsNextUnitLocked()` that provisions a Learner, completes Lesson & Drill, submits a MiniTest with 70% score, and asserts that Unit 2 remains Locked and `MINI_TEST_NOT_PASSED` blocker exists.

- [ ] **Step 2: Write passed Mini-Test flow**
Create a test method `PassedMiniTestUnlocksNextUnit()` that provisions a Learner, completes Lesson & Drill, submits a MiniTest with 85% score, resolves reviews, and asserts that Unit 1 is Completed and Unit 2 is Unlocked.

- [ ] **Step 3: Run the test suite**
Run: `dotnet run --project backend/tests/Application.UnitTest/Application.UnitTest.csproj`
Expected: PASS for all tests.

- [ ] **Step 4: Commit**

```bash
git add backend/tests/Application.UnitTest/Program.cs
git commit -m "test: add E2E tests for mastery logic thresholds and unlocks"
```
