using Domain.Aggregates.LearnerProgress;
using Infrastructure.Data;
using System.Text.Json;

namespace Application.UnitTest;

public static class ToeicRepairPlanTests
{
    public static void TestToeicRepairPlan_InstantiatedAndPersisted()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var planId = "rp_123";
        var learnerId = "learner_abc";
        var plan = new ToeicRepairPlan(
            RepairPlanId: planId,
            SourceSessionId: "session_456",
            LearnerId: learnerId,
            Status: RepairPlanStatus.Generated,
            ReviewQuestionIds: new List<string> { "q1", "q2" },
            DrillQuestionIds: new List<string> { "q3", "q4" },
            Answers: new Dictionary<string, string> { { "q1", "A" } },
            CreatedAtUtc: DateTimeOffset.UtcNow,
            StartedAtUtc: null,
            SubmittedAtUtc: null,
            ExpiredAtUtc: null
        );

        repository.UpsertToeicRepairPlan(plan);

        var retrieved = repository.GetToeicRepairPlan(planId);
        if (retrieved == null) throw new Exception("Plan not retrieved");

        if (retrieved.RepairPlanId != planId) throw new Exception("ID mismatch");
        if (retrieved.LearnerId != learnerId) throw new Exception("Learner ID mismatch");
        if (retrieved.Status != RepairPlanStatus.Generated) throw new Exception("Status mismatch");
        if (retrieved.ReviewQuestionIds.Count != 2) throw new Exception("Review questions mismatch");
        if (retrieved.DrillQuestionIds.Count != 2) throw new Exception("Drill questions mismatch");
        if (retrieved.Answers.Count != 1) throw new Exception("Answers mismatch");

        var active = repository.GetActiveRepairPlan(learnerId);
        if (active == null || active.RepairPlanId != planId) throw new Exception("Active plan mismatch");
    }
}
