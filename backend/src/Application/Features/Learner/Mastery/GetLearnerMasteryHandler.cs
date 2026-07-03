using Application.Common.Interfaces.Repositories;

namespace Application.Features.Learner.Mastery;

public sealed record GetLearnerMasteryQuery(string LearnerId, string UnitId);

public sealed record LearnerMasteryResponse(
    bool IsUnlocked,
    int MasteryPercent,
    int BlockingReviewCount,
    IReadOnlyList<string> LockedReasons
);

public sealed class GetLearnerMasteryHandler(IKnowledgeRepository repository)
{
    public LearnerMasteryResponse Handle(GetLearnerMasteryQuery query)
    {
        var path = repository.GetActiveLearningPath(query.LearnerId);
        if (path == null) throw new ArgumentException("UNIT_NOT_IN_PATH");

        var pathUnits = repository.GetLearningPathUnits(path.PathId);
        if (!pathUnits.Any(u => u.UnitId == query.UnitId))
        {
            throw new ArgumentException("UNIT_NOT_IN_PATH");
        }

        var record = repository.GetMasteryRecord(query.LearnerId, query.UnitId);
        if (record == null)
        {
            var service = new MasteryCalculationService(repository);
            service.RecalculateMastery(query.LearnerId, query.UnitId);
            record = repository.GetMasteryRecord(query.LearnerId, query.UnitId);
            
            if (record == null)
            {
                return new LearnerMasteryResponse(false, 0, 0, new List<string> { "MASTERY_NOT_CALCULATED" });
            }
        }

        var blockers = repository.GetUnlockBlockers(query.LearnerId, query.UnitId);

        return new LearnerMasteryResponse(
            record.IsUnlocked,
            record.MasteryPercent,
            record.BlockingReviewCount,
            blockers.Select(b => b.Reason).ToList()
        );
    }
}
