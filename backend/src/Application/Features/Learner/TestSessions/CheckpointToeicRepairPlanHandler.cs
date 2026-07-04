using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.TestSessions;

public sealed record CheckpointToeicRepairPlanCommand(
    string PlanId, 
    string LearnerId, 
    IReadOnlyDictionary<string, string> Answers
);

public sealed class CheckpointToeicRepairPlanHandler
{
    private readonly IKnowledgeRepository _repository;

    public CheckpointToeicRepairPlanHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task<ToeicRepairPlan?> Handle(CheckpointToeicRepairPlanCommand command)
    {
        var plan = _repository.GetToeicRepairPlan(command.PlanId);
        
        if (plan == null || plan.LearnerId != command.LearnerId)
        {
            return Task.FromResult<ToeicRepairPlan?>(null); // Unauthorized or not found
        }

        if (plan.Status != RepairPlanStatus.Started)
        {
            return Task.FromResult<ToeicRepairPlan?>(plan); // Can only checkpoint started plans
        }

        // Merge answers
        var updatedAnswers = new Dictionary<string, string>(plan.Answers);
        foreach (var (qId, ans) in command.Answers)
        {
            updatedAnswers[qId] = ans;
        }

        var updatedPlan = plan with { Answers = updatedAnswers };

        _repository.UpsertToeicRepairPlan(updatedPlan);
        return Task.FromResult<ToeicRepairPlan?>(updatedPlan);
    }
}
