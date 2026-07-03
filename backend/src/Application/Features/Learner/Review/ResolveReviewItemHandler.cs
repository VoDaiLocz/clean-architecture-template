using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Review;

public sealed record ResolveReviewItemCommand(
    string LearnerId,
    string ReviewItemId,
    string Answer
);

public sealed record ResolveReviewItemResponse(
    bool IsCorrect,
    string Status
);

public sealed class ResolveReviewItemHandler(IKnowledgeRepository repository)
{
    public ResolveReviewItemResponse Handle(ResolveReviewItemCommand command)
    {
        var item = repository.GetReviewItem(command.ReviewItemId);
        if (item is null)
        {
            throw new ArgumentException("REVIEW_ITEM_NOT_FOUND");
        }

        if (item.LearnerId != command.LearnerId)
        {
            throw new ArgumentException("REVIEW_ITEM_NOT_OWNED");
        }

        if (item.Status == ReviewItemStatus.Resolved)
        {
            throw new ArgumentException("ALREADY_RESOLVED");
        }

        var isCorrect = item.CorrectAnswer == command.Answer;

        var repair = new RepairAttempt(
            Guid.NewGuid().ToString(),
            item.ReviewItemId,
            command.LearnerId,
            command.Answer,
            isCorrect,
            DateTimeOffset.UtcNow
        );

        repository.UpsertRepairAttempt(repair);

        if (!isCorrect)
        {
            throw new ArgumentException("REPAIR_NOT_PASSED");
        }

        var updatedItem = item with 
        { 
            Status = ReviewItemStatus.Resolved, 
            ResolvedAtUtc = DateTimeOffset.UtcNow 
        };
        repository.UpsertReviewItem(updatedItem);

        var masteryService = new Application.Features.Learner.Mastery.MasteryCalculationService(repository);
        masteryService.RecalculateMastery(command.LearnerId, item.UnitId);

        return new ResolveReviewItemResponse(isCorrect, "Resolved");
    }
}
