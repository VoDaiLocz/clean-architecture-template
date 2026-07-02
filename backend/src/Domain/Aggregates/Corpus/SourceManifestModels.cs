namespace Domain.Aggregates.Corpus;

public enum SourceProvider
{
    GoogleDrive,
    GoogleDocs,
    SharePoint,
    Shortlink,
    ExternalWeb,
    Unknown,
}

public enum SourceType
{
    DriveFile,
    DriveFolder,
    GoogleSheet,
    GoogleDoc,
    SharePoint,
    Shortlink,
    ExternalWeb,
    Other,
}

public enum SourceAccessStatus
{
    Accessible,
    AccessBlocked,
}

public enum MaterialClass
{
    TestBook,
    SkillBook,
    Vocabulary,
    Roadmap,
    SpeakingWriting,
    GrammarReference,
    ExternalReference,
    Unknown,
}

public sealed record SourceEvidenceFlags(
    bool HasPdf,
    bool HasAudio,
    bool HasImage,
    bool HasTranscript,
    bool HasAnswerKey
);

public sealed record SourceManifestEntry(
    string SourceId,
    int SheetRowNumber,
    string Title,
    string Url,
    SourceProvider Provider,
    SourceType SourceType,
    MaterialClass PrimaryMaterialClass,
    SourceAccessStatus AccessStatus,
    SourceEvidenceFlags Evidence,
    string AuditNotes
);

public sealed record SourceManifestSummary(
    int TotalSources,
    int AccessibleSources,
    int BlockedSources,
    int DriveFiles,
    int DriveFolders,
    int GoogleSheets,
    int GoogleDocs,
    int SharePointSources,
    int Shortlinks,
    int ExternalWebSources,
    int TestBooks,
    int SkillBooks,
    int VocabularySources,
    int RoadmapSources,
    int SpeakingWritingSources,
    int GrammarReferenceSources,
    int SourcesWithPdf,
    int SourcesWithAudio,
    int SourcesWithImage,
    int SourcesWithTranscript,
    int SourcesWithAnswerKey
);

public enum SourceAssetRole
{
    Pdf,
    Audio,
    Image,
    Transcript,
    AnswerKey,
    Document,
    WebPage,
    Unknown,
}

public sealed record SourceContainer(
    string ContainerId,
    string SourceId,
    SourceProvider Provider,
    string ExternalId,
    string Title,
    SourceAccessStatus AccessStatus,
    DateTimeOffset DiscoveredAtUtc
);

public sealed record SourceAsset(
    string AssetId,
    string ContainerId,
    string SourceId,
    string FileName,
    string MimeType,
    string Extension,
    long SizeBytes,
    SourceAssetRole DetectedRole,
    string ProviderUrl,
    string ObjectKey,
    string Checksum
);

public enum ExtractedBlockType
{
    Heading,
    Paragraph,
    Question,
    AnswerOption,
    Table,
    Caption,
    Unknown,
}

public sealed record ExtractedPage(
    string PageId,
    string AssetId,
    int PageNumber,
    int Width,
    int Height,
    DateTimeOffset ExtractedAtUtc
);

public sealed record ExtractedTextBlock(
    string BlockId,
    string AssetId,
    string PageId,
    int PageNumber,
    ExtractedBlockType BlockType,
    string Text,
    decimal Confidence,
    string CoordinatesJson
);

public enum DraftContentStatus
{
    PendingValidation,
    ValidationFailed,
    ReadyForReview,
    Approved,
    Rejected,
    Published,
}

public sealed record DraftContentItem(
    string DraftId,
    string AssetId,
    MaterialClass MaterialClass,
    int? ToeicPart,
    string ItemType,
    string PayloadJson,
    string SourceTraceJson,
    decimal ParserConfidence,
    DraftContentStatus Status
);

public enum PublishedContentStatus
{
    Published,
    Archived,
}

public sealed record PublishedLesson(
    string LessonId,
    string UnitId,
    int ToeicPart,
    string Title,
    string Objective,
    string SkillTags,
    string SourceTraceJson,
    PublishedContentStatus Status
);

public sealed record GuidedExample(
    string ExampleId,
    string LessonId,
    string Prompt,
    string Explanation,
    int DisplayOrder
);

public enum PublishedQuestionType
{
    SingleQuestion,
    ConversationSet,
    TalkSet,
    PassageSet,
}

public sealed record PublishedQuestion(
    string QuestionId,
    string LessonId,
    int ToeicPart,
    PublishedQuestionType QuestionType,
    string Prompt,
    string OptionsJson,
    string CorrectAnswer,
    string Explanation,
    string? MediaAssetId,
    string? PassageId,
    string? GroupId,
    string EvidenceJson,
    string SkillTags,
    string SourceTraceJson,
    PublishedContentStatus Status
);

public static class PublishedQuestionRules
{
    public static void EnsureValid(PublishedQuestion question)
    {
        if (question.ToeicPart is < 1 or > 7)
        {
            throw new InvalidOperationException("Published TOEIC question part must be between 1 and 7.");
        }

        RequireText(question.QuestionId, "Published question id is required.");
        RequireText(question.Prompt, "Published question prompt is required.");
        RequireText(question.OptionsJson, "Published question options are required.");
        RequireText(question.CorrectAnswer, "Published question correct answer is required.");
        RequireText(question.Explanation, "Published question explanation is required.");
        RequireText(question.EvidenceJson, "Published question evidence is required.");
        RequireText(question.SourceTraceJson, "Published question source trace is required.");

        if (question.ToeicPart == 1 && string.IsNullOrWhiteSpace(question.MediaAssetId))
        {
            throw new InvalidOperationException("Part 1 questions require image/audio media.");
        }

        if (question.ToeicPart is 3 or 4 && string.IsNullOrWhiteSpace(question.GroupId))
        {
            throw new InvalidOperationException("Part 3 and Part 4 questions require a group relationship.");
        }

        if (question.ToeicPart is 6 or 7 && string.IsNullOrWhiteSpace(question.PassageId))
        {
            throw new InvalidOperationException("Part 6 and Part 7 questions require passage context.");
        }
    }

    private static void RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }
    }
}

public enum PublishedTestMode
{
    Mini,
    Part,
    Skill,
    Full,
}

public enum ToeicTestSectionType
{
    Listening,
    Reading,
}

public sealed record PublishedTest(
    string TestId,
    PublishedTestMode TestMode,
    string Title,
    int TargetQuestionCount,
    int DurationMinutes,
    string SourceTraceJson,
    PublishedContentStatus Status
);

public sealed record PublishedTestSection(
    string SectionId,
    string TestId,
    ToeicTestSectionType SectionType,
    int DisplayOrder,
    int TargetQuestionCount,
    int DurationMinutes
);

public sealed record PublishedTestItem(
    string TestItemId,
    string SectionId,
    string QuestionId,
    int ToeicPart,
    int DisplayOrder,
    decimal ScoreWeight
);

public static class PublishedTestRules
{
    public static void EnsureValid(PublishedTest test)
    {
        RequireText(test.TestId, "Published test id is required.");
        RequireText(test.Title, "Published test title is required.");
        RequireText(test.SourceTraceJson, "Published test source trace is required.");

        if (test.TargetQuestionCount <= 0)
        {
            throw new InvalidOperationException("Published test question count must be positive.");
        }

        if (test.DurationMinutes <= 0)
        {
            throw new InvalidOperationException("Published test duration must be positive.");
        }

        if (test.TestMode == PublishedTestMode.Full && test.TargetQuestionCount != 200)
        {
            throw new InvalidOperationException("Full TOEIC tests must represent 200 questions.");
        }
    }

    public static void EnsureValid(PublishedTestSection section)
    {
        RequireText(section.SectionId, "Published test section id is required.");
        RequireText(section.TestId, "Published test section test id is required.");

        if (section.DisplayOrder <= 0 || section.TargetQuestionCount <= 0 || section.DurationMinutes <= 0)
        {
            throw new InvalidOperationException("Published test section order, question count, and duration must be positive.");
        }
    }

    public static void EnsureValid(PublishedTestItem item)
    {
        RequireText(item.TestItemId, "Published test item id is required.");
        RequireText(item.SectionId, "Published test item section id is required.");
        RequireText(item.QuestionId, "Published test item question id is required.");

        if (item.ToeicPart is < 1 or > 7)
        {
            throw new InvalidOperationException("Published test item TOEIC part must be between 1 and 7.");
        }

        if (item.DisplayOrder <= 0 || item.ScoreWeight <= 0)
        {
            throw new InvalidOperationException("Published test item order and score weight must be positive.");
        }
    }

    private static void RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }
    }
}
