using Application.Features.Learner.TestSessions;
using Domain.Aggregates.LearnerProgress;
using Infrastructure.Data;

namespace Application.UnitTest;

public static class ToeicRepairPlanFlowTests
{
    public static async Task TestRepairPlanFlow_StartCheckpointSubmit()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = "learner_1";
        var planId = "plan_1";

        // Setup a generated plan
        var plan = new ToeicRepairPlan(
            RepairPlanId: planId,
            LearnerId: learnerId,
            SourceSessionId: "session_1",
            Status: RepairPlanStatus.Generated,
            ReviewQuestionIds: new List<string> { "q1" },
            DrillQuestionIds: new List<string> { "q2" },
            Answers: new Dictionary<string, string>(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            StartedAtUtc: null,
            SubmittedAtUtc: null,
            ExpiredAtUtc: DateTimeOffset.UtcNow.AddMinutes(30)
        );
        repository.UpsertToeicRepairPlan(plan);

        // 1. Start the plan
        var startHandler = new StartToeicRepairPlanHandler(repository);
        var startedPlan = await startHandler.Handle(new StartToeicRepairPlanCommand(planId, learnerId));

        if (startedPlan == null) throw new Exception("Started plan is null");
        if (startedPlan.Status != RepairPlanStatus.Started) throw new Exception("Status should be Started");
        if (startedPlan.StartedAtUtc == null) throw new Exception("StartedAtUtc should be set");

        // 2. Checkpoint the plan
        var checkpointHandler = new CheckpointToeicRepairPlanHandler(repository);
        var checkpointedPlan = await checkpointHandler.Handle(new CheckpointToeicRepairPlanCommand(
            planId, learnerId, new Dictionary<string, string> { { "q1", "A" } }
        ));

        if (checkpointedPlan == null) throw new Exception("Checkpointed plan is null");
        if (checkpointedPlan.Answers.Count != 1 || checkpointedPlan.Answers["q1"] != "A") throw new Exception("Answers not updated");

        // 3. Submit the plan
        var submitHandler = new SubmitToeicRepairPlanHandler(repository);
        var submittedPlan = await submitHandler.Handle(new SubmitToeicRepairPlanCommand(
            planId, learnerId, new Dictionary<string, string> { { "q2", "B" } }
        ));

        if (submittedPlan == null) throw new Exception("Submitted plan is null");
        if (submittedPlan.Status != RepairPlanStatus.Submitted) throw new Exception("Status should be Submitted");
        if (submittedPlan.SubmittedAtUtc == null) throw new Exception("SubmittedAtUtc should be set");
        if (submittedPlan.Answers.Count != 2 || submittedPlan.Answers["q1"] != "A" || submittedPlan.Answers["q2"] != "B") 
            throw new Exception("Answers not merged correctly");
    }
}
