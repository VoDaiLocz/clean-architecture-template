# Codebase Architecture Refactoring Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Resolve architectural friction in the codebase: Fix CQS violations in Today Plan, shift Mastery Recalculation from Read to Write-side (performance), and deepen the Extraction Parser with a Strategy profile.

**Architecture:** 
1. `GetLearnerTodayPlanHandler` becomes Read-Only. `GenerateNextAssignmentHandler` handles DB mutating logic.
2. `MasteryCalculationService` is invoked only as a side-effect after Attempt Submit and Review Resolution.
3. `ToeicAnswerKeyParser` takes an injected list of `IToeicParserProfile`s to replace hardcoded regex.

**Tech Stack:** C# 13, .NET 9, SQLite.

---

### Task 1: Fix CQS Violation in Today Plan

**Files:**
- Modify: `backend/src/Application/Features/Learner/Work/GetLearnerTodayPlanHandler.cs`
- Create: `backend/src/Application/Features/Learner/Work/GenerateNextAssignmentHandler.cs`
- Modify: `backend/tests/Application.UnitTest/Program.cs`

- [ ] **Step 1: Write the failing test**

Add to `ApplicationTests` in `backend/tests/Application.UnitTest/Program.cs`:
```csharp
public static void TodayPlanReadDoesNotMutateDb()
{
    using var repository = SqliteKnowledgeRepository.InMemory();
    repository.Initialize();
    var learnerId = "l1";
    // Setup basic profile/path
    repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "abc", DateTimeOffset.UtcNow));
    repository.UpsertLearningPath(new LearningPath("p1", learnerId, "target", DateTimeOffset.UtcNow));
    repository.UpsertLearningPathUnit(new LearningPathUnit("p1", "u1", "unit", 1, LearningPathUnitStatus.Unlocked));
    
    // Test GET (should not create assignment)
    var getHandler = new Application.Features.Learner.Work.GetLearnerTodayPlanHandler(repository);
    var res1 = getHandler.Handle(new Application.Features.Learner.Work.GetLearnerTodayPlanQuery(learnerId));
    Assert.True(res1.ReadyForNewAssignment);
    Assert.Null(res1.PrimaryAssignment);
    
    // Test POST (should create assignment)
    var postHandler = new Application.Features.Learner.Work.GenerateNextAssignmentHandler(repository);
    var res2 = postHandler.Handle(new Application.Features.Learner.Work.GenerateNextAssignmentCommand(learnerId));
    Assert.NotNull(res2.PrimaryAssignment);
}
```
Add `("Today plan read does not mutate DB", ApplicationTests.TodayPlanReadDoesNotMutateDb),` to tests array.

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Write minimal implementation**

Update `LearnerTodayPlanResponse` in `GetLearnerTodayPlanHandler.cs`:
```csharp
public sealed record LearnerTodayPlanResponse(
    LearnerAssignmentResponse? PrimaryAssignment,
    bool ReadyForNewAssignment,
    IReadOnlyList<string> Blockers,
    LearnerPathProgressResponse PathProgress,
    int ReviewCount
);
```
In `GetLearnerTodayPlanHandler.cs`, remove `MasteryCalculationService` calls completely.
Remove the `UpsertLearnerAssignment` code. 
If no active assignments exist and `currentUnit != null`, check blockers. If no hard prerequisite blockers (`PREREQUISITE_NOT_COMPLETED`), return `ReadyForNewAssignment = true`. Else `ReadyForNewAssignment = false`.

Create `backend/src/Application/Features/Learner/Work/GenerateNextAssignmentHandler.cs`:
```csharp
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;
namespace Application.Features.Learner.Work;

public sealed record GenerateNextAssignmentCommand(string LearnerId);

public sealed class GenerateNextAssignmentHandler(IKnowledgeRepository repository)
{
    public LearnerTodayPlanResponse Handle(GenerateNextAssignmentCommand command)
    {
        // Move the logic from the old Get handler here:
        // 1. check current unit
        // 2. evaluate blockers
        // 3. determine nextActivity
        // 4. UpsertLearnerAssignment
        // 5. Return new LearnerTodayPlanResponse
        // (You may reuse the GetLearnerTodayPlanHandler internally to return the final view, or reconstruct it).
        throw new NotImplementedException("Implement full assignment generation here");
    }
}
```
*(Implementer: Ensure you fully migrate the `nextActivity` evaluation logic from the old handler into the new one).*

- [ ] **Step 4: Run test to verify it passes**
- [ ] **Step 5: Commit**
```bash
git commit -m "refactor(arch): split TodayPlan into Query and Command to fix CQS violation"
```

---

### Task 2: Shift Mastery Recalculation for Performance

**Files:**
- Modify: `backend/src/Application/Features/Learner/Work/SubmitAttemptHandler.cs`
- Modify: `backend/src/Application/Features/Learner/Review/ResolveReviewItemHandler.cs`
- Modify: `backend/src/Application/Features/Learner/Work/GetLearnerTodayPlanHandler.cs`

- [ ] **Step 1: Write the failing test**

```csharp
public static void MasteryRecalculatesOnAttemptSubmit()
{
    // Write a test showing that submitting a passing minitest attempt 
    // causes the MasteryRecord to update to 100% and unlocks the next unit, 
    // without ever explicitly calling Recalculate in the GET handler.
}
```

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Write minimal implementation**

In `SubmitAttemptHandler.cs`:
After `repository.UpsertLearnerAttempt(attempt);`, add:
```csharp
var masteryService = new Application.Features.Learner.Mastery.MasteryCalculationService(repository);
masteryService.RecalculateMastery(assignment.LearnerId, assignment.ContentRefId);
```

In `ResolveReviewItemHandler.cs`:
After `repository.UpsertReviewItem(updated);`, add:
```csharp
var masteryService = new Application.Features.Learner.Mastery.MasteryCalculationService(repository);
masteryService.RecalculateMastery(item.LearnerId, item.UnitId);
```

Ensure `GetLearnerTodayPlanHandler` has ABSOLUTELY NO references to `MasteryCalculationService`. It should just read from `GetLearningPathUnits` and `GetUnlockBlockers` directly.

- [ ] **Step 4: Run test to verify it passes**
- [ ] **Step 5: Commit**
```bash
git commit -m "refactor(arch): shift mastery recalculation to write operations"
```

---

### Task 3: Deepen Parser Architecture with Profiles

**Files:**
- Create: `backend/src/Application/Features/SourceExtraction/IToeicParserProfile.cs`
- Modify: `backend/src/Infrastructure/Extraction/ToeicAnswerKeyParser.cs`

- [ ] **Step 1: Write the failing test**

```csharp
public static void ToeicAnswerKeyParserUsesProfiles()
{
    // Mock a profile that returns 100% confidence, and one that returns 10%
    // Ensure the parser uses the results of the 100% confidence profile.
}
```

- [ ] **Step 2: Run test to verify it fails**

- [ ] **Step 3: Write minimal implementation**

Create `backend/src/Application/Features/SourceExtraction/IToeicParserProfile.cs`:
```csharp
using Domain.Aggregates.Corpus;
namespace Application.Features.SourceExtraction;

public interface IToeicParserProfile
{
    bool CanParse(SourceAsset asset);
    IReadOnlyList<AnswerKeyMappingResult> ParseAnswerKeys(IReadOnlyList<ExtractedTextBlock> blocks);
}
```

Create `backend/src/Infrastructure/Extraction/Profiles/DefaultToeicParserProfile.cs`:
```csharp
// Move the static Regex AnswerRegex from ToeicAnswerKeyParser here.
// Implement ParseAnswerKeys() using the Regex.
```

Modify `ToeicAnswerKeyParser.cs`:
```csharp
public class ToeicAnswerKeyParser(IKnowledgeRepository repository, IEnumerable<IToeicParserProfile> profiles) : IAnswerKeyParser
{
    public IReadOnlyList<AnswerKeyMappingResult> Parse(SourceAsset asset)
    {
        var blocks = repository.GetExtractedTextBlocks(asset.AssetId);
        var applicableProfiles = profiles.Where(p => p.CanParse(asset)).ToList();
        if (!applicableProfiles.Any()) return []; // or fallback to default
        
        var bestResult = applicableProfiles
            .Select(p => p.ParseAnswerKeys(blocks))
            .OrderByDescending(r => r.FirstOrDefault()?.Confidence ?? 0)
            .FirstOrDefault();
            
        return bestResult ?? [];
    }
}
```

- [ ] **Step 4: Run test to verify it passes**
- [ ] **Step 5: Commit**
```bash
git commit -m "refactor(arch): deepen answer key parser with profile strategy"
```
