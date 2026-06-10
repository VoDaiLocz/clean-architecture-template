using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearningItems;

namespace Application.Common.Interfaces.Repositories;

public interface IKnowledgeRepository
{
    void Initialize();

    void InsertRawSource(string sourceId, string title, string url, string status);

    void UpsertSourceManifestEntry(SourceManifestEntry entry);

    IReadOnlyList<SourceManifestEntry> GetSourceManifestEntries();

    SourceManifestSummary GetSourceManifestSummary();

    void UpsertCorpusManifest(CorpusManifest manifest);

    void UpsertNormalizationStage(NormalizationStageSnapshot stage);

    CorpusManifest GetCorpusManifest();

    IReadOnlyList<NormalizationStageSnapshot> GetNormalizationStages();

    ValidationResult Publish(DraftLearningItem item);

    int Count(string tableName);
}
