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
