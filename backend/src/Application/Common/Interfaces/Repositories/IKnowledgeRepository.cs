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

    SourceAsset? GetSourceAsset(string assetId);

    IReadOnlyList<SourceAsset> GetSourceAssets(string containerId);

    void UpsertRejectedLocalSourceFile(RejectedLocalSourceFile file);

    IReadOnlyList<RejectedLocalSourceFile> GetRejectedLocalSourceFiles();

    DuplicateAssetReport GetDuplicateAssets();

    void UpsertSourceDiscoveryIssue(SourceDiscoveryIssue issue);

    IReadOnlyList<SourceDiscoveryIssue> GetSourceDiscoveryIssues(string sourceId);

    void UpsertSourceResolutionRecord(SourceResolutionRecord record);

    IReadOnlyList<SourceResolutionRecord> GetSourceResolutionRecords();

    void UpsertSourceAudioMetadata(SourceAudioMetadata metadata);

    SourceAudioMetadata? GetSourceAudioMetadata(string assetId);

    void UpsertExtractedPage(ExtractedPage page);

    IReadOnlyList<ExtractedPage> GetExtractedPages(string assetId);

    void UpsertExtractedTextBlock(ExtractedTextBlock block);

    IReadOnlyList<ExtractedTextBlock> GetExtractedTextBlocks(string assetId);

    void UpsertDraftContentItem(DraftContentItem draft);

    IReadOnlyList<DraftContentItem> GetDraftContentItems(string assetId);

    void UpsertPublishedLesson(PublishedLesson lesson);

    IReadOnlyList<PublishedLesson> GetPublishedLessons(string unitId);
    PublishedLesson? GetPublishedLesson(string lessonId);

    void UpsertGuidedExample(GuidedExample example);

    IReadOnlyList<GuidedExample> GetGuidedExamples(string lessonId);

    void UpsertPublishedQuestion(PublishedQuestion question);

    IReadOnlyList<PublishedQuestion> GetPublishedQuestions(int toeicPart);
    PublishedQuestion? GetPublishedQuestion(string questionId);

    int CountDraftContentItems(int toeicPart);

    int CountDraftContentItems(DraftContentStatus status);

    int CountDraftContentItems(int toeicPart, DraftContentStatus status);

    int CountPublishedLessons(int toeicPart);

    int CountPublishedQuestions(int toeicPart);

    IReadOnlyList<ValidationIssueCodeCount> CountValidationIssuesByCode();

    void UpsertPublishedTest(PublishedTest test);

    IReadOnlyList<PublishedTest> GetPublishedTests(PublishedTestMode testMode);

    void UpsertPublishedTestSection(PublishedTestSection section);

    IReadOnlyList<PublishedTestSection> GetPublishedTestSections(string testId);

    void UpsertPublishedTestItem(PublishedTestItem item);

    IReadOnlyList<PublishedTestItem> GetPublishedTestItems(string sectionId);

    void UpsertLearnerProfile(LearnerProfile profile);

    LearnerProfile? GetLearnerProfile(string learnerId);

    void UpsertPlacementSession(PlacementSession session);
    PlacementSession? GetPlacementSessionById(string sessionId);
    IReadOnlyList<PlacementSession> GetPlacementSessions(string learnerId);
    void InsertPlacementSessionQuestions(string sessionId, IReadOnlyList<string> questionIds);
    IReadOnlyList<string> GetPlacementSessionAssignedQuestions(string sessionId);
    void InsertPlacementResult(PlacementResult result, IReadOnlyList<PlacementResultBreakdown> breakdowns);
    PlacementResult? GetPlacementResultBySessionId(string sessionId);
    IReadOnlyList<PlacementResultBreakdown> GetPlacementResultBreakdowns(string resultId);

    void UpsertLearnerAssignment(LearnerAssignment assignment);

    IReadOnlyList<LearnerAssignment> GetLearnerAssignments(string learnerId);

    void UpsertActivitySession(ActivitySession session);
    ActivitySession? GetActivitySession(string sessionId);

    IReadOnlyList<ActivitySession> GetActivitySessions(string assignmentId);

    void UpsertLearnerAttempt(LearnerAttempt attempt);

    IReadOnlyList<LearnerAttempt> GetLearnerAttempts(string sessionId);

    void UpsertAttemptAnswer(AttemptAnswer answer);

    IReadOnlyList<AttemptAnswer> GetAttemptAnswers(string attemptId);

    void UpsertReviewItem(ReviewItem item);
    ReviewItem? GetReviewItem(string reviewItemId);
    IReadOnlyList<ReviewItem> GetReviewItems(string learnerId);

    void UpsertRepairAttempt(RepairAttempt attempt);

    IReadOnlyList<RepairAttempt> GetRepairAttempts(string reviewItemId);

    void UpsertMasteryRecord(MasteryRecord record);

    MasteryRecord? GetMasteryRecord(string learnerId, string unitId);

    void DeleteUnlockBlockers(string learnerId, string unitId);

    void UpsertUnlockBlocker(UnlockBlocker blocker);

    IReadOnlyList<UnlockBlocker> GetUnlockBlockers(string learnerId, string unitId);

    void UpsertCorpusManifest(CorpusManifest manifest);

    void UpsertNormalizationStage(NormalizationStageSnapshot stage);

    CorpusManifest GetCorpusManifest();

    IReadOnlyList<NormalizationStageSnapshot> GetNormalizationStages();

    ValidationResult Publish(DraftLearningItem item);

    void RecordValidationIssue(ValidationIssue issue, string itemType, string? sourceId);

    void UpsertLearningPath(LearningPath path);
    LearningPath? GetActiveLearningPath(string learnerId);
    
    void UpsertLearningPathUnit(LearningPathUnit unit);
    IReadOnlyList<LearningPathUnit> GetLearningPathUnits(string pathId);

    void UpsertLearnerPathGenerationRun(LearnerPathGenerationRun run);

    int Count(string tableName);
}
