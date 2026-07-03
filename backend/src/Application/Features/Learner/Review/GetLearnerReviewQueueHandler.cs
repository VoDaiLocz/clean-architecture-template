using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Review;

public sealed record GetLearnerReviewQueueQuery(string LearnerId);

public sealed record ReviewQueueItemDto(
    string ReviewItemId,
    string QuestionId,
    string ErrorTag,
    string LearnerAnswer,
    string CorrectAnswer,
    bool IsBlocking,
    DateTimeOffset CreatedAtUtc
);

public sealed record ReviewQueueUnitGroupDto(
    string UnitId,
    List<ReviewQueueItemDto> Items
);

public sealed record GetLearnerReviewQueueResponse(
    List<ReviewQueueUnitGroupDto> Groups
);

public sealed class GetLearnerReviewQueueHandler(IKnowledgeRepository repository)
{
    public GetLearnerReviewQueueResponse Handle(GetLearnerReviewQueueQuery query)
    {
        var items = repository.GetReviewItems(query.LearnerId)
            .Where(r => r.Status == ReviewItemStatus.Open)
            .ToList();

        var groups = items
            .GroupBy(r => r.UnitId)
            .Select(g => new ReviewQueueUnitGroupDto(
                g.Key,
                g.Select(r => new ReviewQueueItemDto(
                    r.ReviewItemId,
                    r.QuestionId,
                    r.ErrorTag,
                    r.LearnerAnswer,
                    r.CorrectAnswer,
                    r.IsBlocking,
                    r.CreatedAtUtc
                ))
                // Order by severity (blocking first) then by creation time
                .OrderByDescending(r => r.IsBlocking)
                .ThenBy(r => r.CreatedAtUtc)
                .ToList()
            ))
            .ToList();

        return new GetLearnerReviewQueueResponse(groups);
    }
}
