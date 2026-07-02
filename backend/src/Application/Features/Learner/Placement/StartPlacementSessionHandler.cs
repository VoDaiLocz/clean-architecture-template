using Application.Common.Interfaces.Repositories;
using Application.Features.Learner.Onboarding;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Placement;

public sealed record StartPlacementSessionCommand(string LearnerId);

public sealed record StartPlacementSessionResponse(
    string SessionId,
    string LearnerId,
    PlacementSessionStatus Status,
    LearnerNextAction NextAction
);

public sealed class StartPlacementSessionHandler(IKnowledgeRepository repository)
{
    public StartPlacementSessionResponse Handle(StartPlacementSessionCommand command)
    {
        var profile = repository.GetLearnerProfile(command.LearnerId)
            ?? throw new InvalidOperationException("Learner profile is required before placement.");
        var activeSession = repository.GetPlacementSessions(profile.LearnerId)
            .FirstOrDefault(session => session.Status == PlacementSessionStatus.InProgress);

        if (activeSession is not null)
        {
            return ToResponse(activeSession, "ResumePlacement");
        }

        var session = new PlacementSession(
            SessionId: $"placement-{profile.LearnerId}",
            LearnerId: profile.LearnerId,
            Status: PlacementSessionStatus.InProgress,
            StartedAtUtc: DateTimeOffset.UtcNow,
            CompletedAtUtc: null
        );
        repository.UpsertPlacementSession(session);

        return ToResponse(session, "StartPlacement");
    }

    private static StartPlacementSessionResponse ToResponse(PlacementSession session, string actionCode) =>
        new(
            session.SessionId,
            session.LearnerId,
            session.Status,
            new LearnerNextAction(
                actionCode,
                $"/api/learner/placement/{session.SessionId}",
                "Continue the TOEIC placement diagnosis before the learning path can be generated."
            )
        );
}
