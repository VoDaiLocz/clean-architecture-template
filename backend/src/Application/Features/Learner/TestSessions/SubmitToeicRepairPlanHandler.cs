using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.TestSessions;

public sealed record SubmitToeicRepairPlanCommand(
    string PlanId, 
    string LearnerId, 
    IReadOnlyDictionary<string, string> Answers
);

public sealed class SubmitToeicRepairPlanHandler
{
    private readonly IKnowledgeRepository _repository;

    public SubmitToeicRepairPlanHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task<ToeicRepairPlan?> Handle(SubmitToeicRepairPlanCommand command)
    {
        var plan = _repository.GetToeicRepairPlan(command.PlanId);
        
        if (plan == null || plan.LearnerId != command.LearnerId)
        {
            return Task.FromResult<ToeicRepairPlan?>(null); // Unauthorized or not found
        }

        if (plan.Status == RepairPlanStatus.Submitted)
        {
            return Task.FromResult<ToeicRepairPlan?>(plan); // Already submitted
        }

        // Merge final answers
        var updatedAnswers = new Dictionary<string, string>(plan.Answers);
        foreach (var (qId, ans) in command.Answers)
        {
            updatedAnswers[qId] = ans;
        }

        var submittedPlan = plan with 
        { 
            Answers = updatedAnswers,
            Status = RepairPlanStatus.Submitted,
            SubmittedAtUtc = DateTimeOffset.UtcNow
        };

        _repository.UpsertToeicRepairPlan(submittedPlan);
        return Task.FromResult<ToeicRepairPlan?>(submittedPlan);
    }
}
