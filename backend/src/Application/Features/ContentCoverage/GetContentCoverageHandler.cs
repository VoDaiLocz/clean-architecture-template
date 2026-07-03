using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.ContentCoverage;

public sealed record ContentCoverageSnapshot(
    SourceCoverage Source,
    AssetCoverage Assets,
    ExtractionCoverage Extraction,
    DraftCoverage Draft,
    ValidationCoverage Validation,
    PublishedCoverage Published,
    IReadOnlyList<ToeicPartCoverage> ToeicParts
);

public sealed record SourceCoverage(
    int TotalSources,
    int AccessibleSources,
    int BlockedSources,
    int SourcesWithPdf,
    int SourcesWithAudio,
    int SourcesWithImage,
    int SourcesWithTranscript,
    int SourcesWithAnswerKey
);

public sealed record AssetCoverage(
    int Containers,
    int Assets,
    int DiscoveryIssues,
    int ResolutionRecords
);

public sealed record ExtractionCoverage(
    int AudioMetadataRows,
    int ExtractedPages,
    int ExtractedTextBlocks
);

public sealed record DraftCoverage(
    int TotalDraftItems,
    int PendingValidationDraftItems,
    int ReadyForReviewDraftItems,
    int ValidationFailedDraftItems,
    int PublishedDraftItems,
    int RejectedDraftItems
);

public sealed record ValidationCoverage(
    int ValidationIssueCount,
    IReadOnlyList<ValidationIssueBreakdown> IssueBreakdown
);

public sealed record ValidationIssueBreakdown(string Code, int Count);

public sealed record PublishedCoverage(
    int PublishedLessons,
    int GuidedExamples,
    int PublishedQuestions,
    int PublishedTests,
    int PublishedTestSections,
    int PublishedTestItems
);

public sealed record ToeicPartCoverage(
    int ToeicPart,
    int TotalDraftItems,
    int PendingValidationDraftItems,
    int ReadyForReviewDraftItems,
    int ValidationFailedDraftItems,
    int PublishedDraftItems,
    int RejectedDraftItems,
    int PublishedLessons,
    int PublishedQuestions
);

public sealed class GetContentCoverageHandler(IKnowledgeRepository repository)
{
    public ContentCoverageSnapshot Handle()
    {
        var sourceManifest = repository.GetSourceManifestSummary();
        return new ContentCoverageSnapshot(
            Source: new SourceCoverage(
                sourceManifest.TotalSources,
                sourceManifest.AccessibleSources,
                sourceManifest.BlockedSources,
                sourceManifest.SourcesWithPdf,
                sourceManifest.SourcesWithAudio,
                sourceManifest.SourcesWithImage,
                sourceManifest.SourcesWithTranscript,
                sourceManifest.SourcesWithAnswerKey
            ),
            Assets: new AssetCoverage(
                repository.Count("source_containers"),
                repository.Count("source_assets"),
                repository.Count("source_discovery_issues"),
                repository.Count("source_resolution_records")
            ),
            Extraction: new ExtractionCoverage(
                repository.Count("source_audio_metadata"),
                repository.Count("extracted_pages"),
                repository.Count("extracted_text_blocks")
            ),
            Draft: new DraftCoverage(
                repository.Count("draft_content_items"),
                repository.CountDraftContentItems(DraftContentStatus.PendingValidation),
                repository.CountDraftContentItems(DraftContentStatus.ReadyForReview),
                repository.CountDraftContentItems(DraftContentStatus.ValidationFailed),
                repository.CountDraftContentItems(DraftContentStatus.Published),
                repository.CountDraftContentItems(DraftContentStatus.Rejected)
            ),
            Validation: new ValidationCoverage(
                repository.Count("validation_issues"),
                repository.CountValidationIssuesByCode()
                    .Select(issue => new ValidationIssueBreakdown(issue.Code, issue.Count))
                    .ToArray()
            ),
            Published: new PublishedCoverage(
                repository.Count("published_lessons"),
                repository.Count("guided_examples"),
                repository.Count("published_questions"),
                repository.Count("published_tests"),
                repository.Count("published_test_sections"),
                repository.Count("published_test_items")
            ),
            ToeicParts: Enumerable.Range(1, 7)
                .Select(part => new ToeicPartCoverage(
                    part,
                    repository.CountDraftContentItems(part),
                    repository.CountDraftContentItems(part, DraftContentStatus.PendingValidation),
                    repository.CountDraftContentItems(part, DraftContentStatus.ReadyForReview),
                    repository.CountDraftContentItems(part, DraftContentStatus.ValidationFailed),
                    repository.CountDraftContentItems(part, DraftContentStatus.Published),
                    repository.CountDraftContentItems(part, DraftContentStatus.Rejected),
                    repository.CountPublishedLessons(part),
                    repository.CountPublishedQuestions(part)
                ))
                .ToArray()
        );
    }
}
