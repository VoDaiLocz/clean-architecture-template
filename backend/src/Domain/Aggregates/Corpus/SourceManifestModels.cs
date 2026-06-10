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
