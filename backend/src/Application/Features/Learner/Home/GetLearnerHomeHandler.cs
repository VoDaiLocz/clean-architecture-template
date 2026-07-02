using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Home;

public sealed record GetLearnerHomeQuery(string LearnerId);

public sealed class GetLearnerHomeHandler(IKnowledgeRepository repository)
{
    public LearnerHomeResponse Handle(GetLearnerHomeQuery query)
    {
        var profile = repository.GetLearnerProfile(query.LearnerId);
        if (profile is null)
        {
            return new LearnerHomeResponse(
                query.LearnerId,
                CurrentPart: 0,
                CurrentUnitId: "onboarding",
                CurrentUnitTitle: "Complete onboarding",
                NextActivity: new LearnerActivitySummaryResponse(
                    "learner-onboarding",
                    "Onboarding",
                    "Complete TOEIC profile",
                    "Set your target score, current level, daily study time, and timezone."
                ),
                ReviewCount: 0,
                LockedNextUnit: null
            );
        }

        return new LearnerHomeResponse(
            profile.LearnerId,
            CurrentPart: 0,
            CurrentUnitId: "placement",
            CurrentUnitTitle: $"TOEIC placement for {profile.TargetScore}+ goal",
            NextActivity: new LearnerActivitySummaryResponse(
                "toeic-placement-start",
                "Placement",
                "Start TOEIC placement",
                $"Diagnose your current level from {profile.CurrentEstimatedScore} toward {profile.TargetScore}."
            ),
            ReviewCount: repository.GetReviewItems(profile.LearnerId).Count(item => item.Status == ReviewItemStatus.Open),
            LockedNextUnit: null
        );
    }
}
