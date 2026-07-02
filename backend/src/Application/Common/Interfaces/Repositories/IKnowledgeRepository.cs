using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearnerProgress;
using Domain.Aggregates.LearningItems;

namespace Application.Common.Interfaces.Repositories;

public interface IKnowledgeRepository
{
    void Initialize();

    void InsertRawSource(string sourceId, string title, string url, string status);

    void UpsertSourceManifestEntry(SourceManifestEntry entry);

    IReadOnlyList<SourceManifestEntry> GetSourceManifestEntries();

    SourceManifestSummary GetSourceManifestSummary();

    void UpsertSourceContainer(SourceContainer container);

    IReadOnlyList<SourceContainer> GetSourceContainers(string sourceId);

    void UpsertSourceAsset(SourceAsset asset);

    IReadOnlyList<SourceAsset> GetSourceAssets(string containerId);

    void UpsertExtractedPage(ExtractedPage page);

    IReadOnlyList<ExtractedPage> GetExtractedPages(string assetId);

    void UpsertExtractedTextBlock(ExtractedTextBlock block);

    IReadOnlyList<ExtractedTextBlock> GetExtractedTextBlocks(string assetId);

    void UpsertDraftContentItem(DraftContentItem draft);

    IReadOnlyList<DraftContentItem> GetDraftContentItems(string assetId);

    void UpsertPublishedLesson(PublishedLesson lesson);

    IReadOnlyList<PublishedLesson> GetPublishedLessons(string unitId);

    void UpsertGuidedExample(GuidedExample example);

    IReadOnlyList<GuidedExample> GetGuidedExamples(string lessonId);

    void UpsertPublishedQuestion(PublishedQuestion question);

    IReadOnlyList<PublishedQuestion> GetPublishedQuestions(int toeicPart);

    void UpsertPublishedTest(PublishedTest test);

    IReadOnlyList<PublishedTest> GetPublishedTests(PublishedTestMode testMode);

    void UpsertPublishedTestSection(PublishedTestSection section);

    IReadOnlyList<PublishedTestSection> GetPublishedTestSections(string testId);

    void UpsertPublishedTestItem(PublishedTestItem item);

    IReadOnlyList<PublishedTestItem> GetPublishedTestItems(string sectionId);

    void UpsertLearnerProfile(LearnerProfile profile);

    LearnerProfile? GetLearnerProfile(string learnerId);

    void UpsertCorpusManifest(CorpusManifest manifest);

    void UpsertNormalizationStage(NormalizationStageSnapshot stage);

    CorpusManifest GetCorpusManifest();

    IReadOnlyList<NormalizationStageSnapshot> GetNormalizationStages();

    ValidationResult Publish(DraftLearningItem item);

    int Count(string tableName);
}
