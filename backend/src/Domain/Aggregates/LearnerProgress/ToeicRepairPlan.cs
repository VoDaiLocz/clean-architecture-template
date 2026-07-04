namespace Domain.Aggregates.LearnerProgress;

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
