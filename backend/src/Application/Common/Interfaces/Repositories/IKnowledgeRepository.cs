using Domain.Aggregates.LearningItems;

namespace Application.Common.Interfaces.Repositories;

public interface IKnowledgeRepository
{
    void Initialize();

    void InsertRawSource(string sourceId, string title, string url, string status);

    ValidationResult Publish(DraftLearningItem item);

    int Count(string tableName);
}
