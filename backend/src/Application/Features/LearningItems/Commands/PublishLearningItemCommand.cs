using Domain.Aggregates.LearningItems;

namespace Application.Features.LearningItems.Commands;

public sealed record PublishLearningItemCommand(DraftLearningItem Item);

public sealed record PublishLearningItemResponse(
    bool CanPublish,
    bool NeedsReview,
    IReadOnlyList<string> IssueCodes
);
