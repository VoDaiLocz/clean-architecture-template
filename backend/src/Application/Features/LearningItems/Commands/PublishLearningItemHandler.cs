using Application.Common.Interfaces.Repositories;

namespace Application.Features.LearningItems.Commands;

public sealed class PublishLearningItemHandler(IKnowledgeRepository repository)
{
    public PublishLearningItemResponse Handle(PublishLearningItemCommand command)
    {
        var result = repository.Publish(command.Item);

        return new PublishLearningItemResponse(
            result.CanPublish,
            result.NeedsReview,
            result.Issues.Select(issue => issue.Code).ToArray()
        );
    }
}
