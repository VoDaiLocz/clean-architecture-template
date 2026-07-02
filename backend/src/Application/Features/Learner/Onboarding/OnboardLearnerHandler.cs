using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Onboarding;

public sealed record OnboardLearnerCommand(
    string LearnerId,
    string DisplayName,
    string Email,
    int TargetScore,
    int CurrentEstimatedScore,
    int DailyStudyMinutes,
    string TimeZoneId
);

public sealed record LearnerNextAction(string Code, string ApiRoute, string Reason);

public sealed record OnboardLearnerResponse(
    string LearnerId,
    int TargetScore,
    int CurrentEstimatedScore,
    int DailyStudyMinutes,
    string TimeZoneId,
    LearnerNextAction NextAction
);

public sealed class OnboardLearnerHandler(IKnowledgeRepository repository)
{
    public OnboardLearnerResponse Handle(OnboardLearnerCommand command)
    {
        var existing = repository.GetLearnerProfile(command.LearnerId);
        var now = DateTimeOffset.UtcNow;
        var profile = new LearnerProfile(
            LearnerId: command.LearnerId,
            DisplayName: command.DisplayName,
            Email: command.Email,
            TargetScore: command.TargetScore,
            CurrentEstimatedScore: command.CurrentEstimatedScore,
            DailyStudyMinutes: command.DailyStudyMinutes,
            TimeZoneId: command.TimeZoneId,
            Status: LearnerProfileStatus.Active,
            CreatedAtUtc: existing?.CreatedAtUtc ?? now,
            UpdatedAtUtc: now
        );

        repository.UpsertLearnerProfile(profile);

        return new OnboardLearnerResponse(
            profile.LearnerId,
            profile.TargetScore,
            profile.CurrentEstimatedScore,
            profile.DailyStudyMinutes,
            profile.TimeZoneId,
            new LearnerNextAction(
                "StartPlacement",
                "/api/learner/placement/start",
                "Placement is required before the system can generate a personalized TOEIC path."
            )
        );
    }
}
