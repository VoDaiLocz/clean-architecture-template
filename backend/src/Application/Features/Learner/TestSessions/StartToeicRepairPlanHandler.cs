using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.TestSessions;

public sealed record StartToeicRepairPlanCommand(string PlanId, string LearnerId);

public sealed class StartToeicRepairPlanHandler
{
    private readonly IKnowledgeRepository _repository;

    public StartToeicRepairPlanHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task<ToeicRepairPlan?> Handle(StartToeicRepairPlanCommand command)
    {
        var plan = _repository.GetToeicRepairPlan(command.PlanId);
        
        if (plan == null || plan.LearnerId != command.LearnerId)
        {
            return Task.FromResult<ToeicRepairPlan?>(null); // Unauthorized or not found
        }

        if (plan.Status != RepairPlanStatus.Generated)
        {
            return Task.FromResult<ToeicRepairPlan?>(plan); // Already started or submitted
        }

        var startedPlan = plan with
        {
            Status = RepairPlanStatus.Started,
            StartedAtUtc = DateTimeOffset.UtcNow
        };

        _repository.UpsertToeicRepairPlan(startedPlan);
        return Task.FromResult<ToeicRepairPlan?>(startedPlan);
    }
}
