using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;
using System.Text.RegularExpressions;

namespace Application.Features.ContentCoverage;

public sealed record ContentCoverageSnapshot(
    SourceCoverage Source,
    AssetCoverage Assets,
    ExtractionCoverage Extraction,
    DraftCoverage Draft,
    ValidationCoverage Validation,
    PublishedCoverage Published,
    IReadOnlyList<ToeicPartCoverage> ToeicParts,
    CorpusReadinessAudit CorpusAudit
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

public sealed record CorpusReadinessAudit(
    int TotalAssets,
    int HtmlPlaceholderAssets,
    int ExtractedTextBlocks,
    IReadOnlyList<AssetRoleCount> AssetRoles,
    IReadOnlyList<ExtractedAssetBlockCount> TopExtractedAssets,
    FirstPublishSliceCandidate FirstPublishSlice,
    IReadOnlyList<ToeicPartCorpusReadiness> ToeicPartReadiness,
    IReadOnlyList<ProductionWarning> ProductionWarnings
);

public sealed record AssetRoleCount(string Role, int Count);

public sealed record ExtractedAssetBlockCount(
    string AssetId,
    string FileName,
    SourceAssetRole Role,
    int TextBlockCount
);

public sealed record FirstPublishSliceCandidate(
    string CandidateKey,
    string DisplayName,
    bool IsReadyForDraftParsing,
    string Reason
);

public sealed record ProductionWarning(string Code, string Message);

public sealed record ToeicPartCorpusReadiness(
    int ToeicPart,
    string Name,
    string QuestionRange,
    string RequiredEvidence,
    int EvidenceAssetCount,
    int EvidenceTextBlockCount,
    int DraftItemCount,
    int PublishedQuestionCount,
    bool CanParseDrafts,
    bool CanPublish,
    IReadOnlyList<string> BlockerCodes
);

public sealed partial class GetContentCoverageHandler(IKnowledgeRepository repository)
{
    public ContentCoverageSnapshot Handle()
    {
        var sourceManifest = repository.GetSourceManifestSummary();
        var assets = repository.GetAllSourceAssets();
        var extractedCounts = assets
            .Select(asset => new ExtractedAssetBlockCount(
                asset.AssetId,
                asset.FileName,
                asset.DetectedRole,
                repository.GetExtractedTextBlocks(asset.AssetId).Count
            ))
            .ToArray();
        var publishedLessons = repository.Count("published_lessons");
        var publishedQuestions = repository.Count("published_questions");
        var publishedTests = repository.Count("published_tests");

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
                publishedLessons,
                repository.Count("guided_examples"),
                publishedQuestions,
                publishedTests,
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
                .ToArray(),
            CorpusAudit: BuildCorpusAudit(repository, assets, extractedCounts, publishedLessons, publishedQuestions, publishedTests)
        );
    }

    private static CorpusReadinessAudit BuildCorpusAudit(
        IKnowledgeRepository repository,
        IReadOnlyList<SourceAsset> assets,
        IReadOnlyList<ExtractedAssetBlockCount> extractedCounts,
        int publishedLessons,
        int publishedQuestions,
        int publishedTests
    )
    {
        var roleCounts = assets
            .GroupBy(asset => asset.DetectedRole)
            .OrderBy(group => group.Key.ToString(), StringComparer.Ordinal)
            .Select(group => new AssetRoleCount(group.Key.ToString(), group.Count()))
            .ToArray();

        var htmlPlaceholderAssets = assets.Count(asset =>
            string.Equals(asset.MimeType, "text/html", StringComparison.OrdinalIgnoreCase)
            || (
                string.Equals(asset.Extension, ".pdf", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(asset.MimeType, "application/pdf", StringComparison.OrdinalIgnoreCase)
            )
        );

        var topExtractedAssets = extractedCounts
            .Where(asset => asset.TextBlockCount > 0)
            .OrderByDescending(asset => asset.TextBlockCount)
            .ThenBy(asset => AssetRolePriority(asset.Role))
            .ThenBy(asset => asset.AssetId, StringComparer.Ordinal)
            .Take(10)
            .ToArray();

        var totalExtractedTextBlocks = extractedCounts.Sum(asset => asset.TextBlockCount);
        var partReadiness = BuildPartReadiness(repository, assets);
        var hasPdfText = extractedCounts.Any(asset =>
            asset.Role == SourceAssetRole.Pdf && asset.TextBlockCount > 0
        );
        var hasAnswerKeyEvidence = assets.Any(asset => asset.DetectedRole == SourceAssetRole.AnswerKey)
            || extractedCounts.Any(asset =>
                asset.TextBlockCount > 0
                && asset.FileName.Contains("answer", StringComparison.OrdinalIgnoreCase)
            );
        var part5Ready = hasPdfText && hasAnswerKeyEvidence;

        var warnings = new List<ProductionWarning>();
        if (publishedLessons == 0 && publishedQuestions == 0 && publishedTests == 0)
        {
            warnings.Add(new ProductionWarning(
                "NO_PUBLISHED_CONTENT",
                "Runtime corpus has source/extraction evidence but no learner-ready published lessons, questions, or tests."
            ));
        }

        if (htmlPlaceholderAssets > 0)
        {
            warnings.Add(new ProductionWarning(
                "HTML_PLACEHOLDER_ASSETS",
                "Some downloaded files are HTML placeholders or failed downloads and cannot be used as TOEIC source assets."
            ));
        }

        if (!part5Ready)
        {
            warnings.Add(new ProductionWarning(
                "FIRST_SLICE_NOT_READY",
                "Part 5 reading cannot be draft-parsed until PDF text and answer-key evidence are both available."
            ));
        }

        return new CorpusReadinessAudit(
            TotalAssets: assets.Count,
            HtmlPlaceholderAssets: htmlPlaceholderAssets,
            ExtractedTextBlocks: totalExtractedTextBlocks,
            AssetRoles: roleCounts,
            TopExtractedAssets: topExtractedAssets,
            FirstPublishSlice: new FirstPublishSliceCandidate(
                "Part5Reading",
                "Part 5 reading questions from extracted PDF text and answer key evidence",
                part5Ready,
                part5Ready
                    ? "PDF text blocks and answer-key evidence exist; this is the safest first real draft parser slice."
                    : "Missing PDF text blocks or answer-key evidence."
            ),
            ToeicPartReadiness: partReadiness,
            ProductionWarnings: warnings
        );
    }

    private static IReadOnlyList<ToeicPartCorpusReadiness> BuildPartReadiness(
        IKnowledgeRepository repository,
        IReadOnlyList<SourceAsset> assets
    )
    {
        return ToeicPartReadinessRules.All
            .Select(rule =>
            {
                var evidence = CountQuestionRangeEvidence(repository, assets, rule.QuestionStart, rule.QuestionEnd);
                var hasPdfText = evidence.TextBlockCount > 0;
                var hasAudio = assets.Any(asset => asset.DetectedRole == SourceAssetRole.Audio);
                var hasImage = assets.Any(asset => asset.DetectedRole == SourceAssetRole.Image);
                var hasAnswerKey = HasAnswerKeyEvidence(repository, assets);
                var hasTranscript = assets.Any(asset => asset.DetectedRole == SourceAssetRole.Transcript);
                var hasPassageEvidence = rule.Part is 6 or 7 && hasPdfText;
                var draftCount = repository.CountDraftContentItems(rule.Part);
                var publishedQuestionCount = repository.CountPublishedQuestions(rule.Part);

                var blockers = new List<string>();
                if (!hasPdfText)
                {
                    blockers.Add("MISSING_EXTRACTED_TEXT_RANGE");
                }

                if (rule.RequiresAudio && !hasAudio)
                {
                    blockers.Add("MISSING_AUDIO_ASSET");
                }

                if (rule.RequiresImage && !hasImage)
                {
                    blockers.Add("MISSING_IMAGE_ASSET");
                }

                if (rule.RequiresTranscript && !hasTranscript)
                {
                    blockers.Add("MISSING_TRANSCRIPT_ASSET");
                }

                if (rule.RequiresPassage && !hasPassageEvidence)
                {
                    blockers.Add("MISSING_PASSAGE_CONTEXT");
                }

                if (!hasAnswerKey)
                {
                    blockers.Add("MISSING_ANSWER_KEY_ASSET");
                }

                if (!rule.ParserImplemented)
                {
                    blockers.Add("PARSER_NOT_IMPLEMENTED");
                }

                if (draftCount == 0)
                {
                    blockers.Add("NO_DRAFT_ITEMS");
                }

                if (publishedQuestionCount == 0)
                {
                    blockers.Add("NO_PUBLISHED_QUESTIONS");
                }

                var canParseDrafts = blockers.All(code =>
                    code is "NO_DRAFT_ITEMS" or "NO_PUBLISHED_QUESTIONS"
                );
                var canPublish = blockers.Count == 0;

                return new ToeicPartCorpusReadiness(
                    ToeicPart: rule.Part,
                    Name: rule.Name,
                    QuestionRange: $"{rule.QuestionStart}-{rule.QuestionEnd}",
                    RequiredEvidence: rule.RequiredEvidence,
                    EvidenceAssetCount: evidence.AssetCount,
                    EvidenceTextBlockCount: evidence.TextBlockCount,
                    DraftItemCount: draftCount,
                    PublishedQuestionCount: publishedQuestionCount,
                    CanParseDrafts: canParseDrafts,
                    CanPublish: canPublish,
                    BlockerCodes: blockers
                );
            })
            .ToArray();
    }

    private static QuestionRangeEvidence CountQuestionRangeEvidence(
        IKnowledgeRepository repository,
        IReadOnlyList<SourceAsset> assets,
        int questionStart,
        int questionEnd
    )
    {
        var assetCount = 0;
        var textBlockCount = 0;

        foreach (var asset in assets)
        {
            var assetHasEvidence = false;
            foreach (var block in repository.GetExtractedTextBlocks(asset.AssetId))
            {
                if (!ContainsQuestionNumber(block.Text, questionStart, questionEnd))
                {
                    continue;
                }

                textBlockCount++;
                assetHasEvidence = true;
            }

            if (assetHasEvidence)
            {
                assetCount++;
            }
        }

        return new QuestionRangeEvidence(assetCount, textBlockCount);
    }

    private static bool HasAnswerKeyEvidence(IKnowledgeRepository repository, IReadOnlyList<SourceAsset> assets)
    {
        foreach (var asset in assets)
        {
            if (asset.DetectedRole == SourceAssetRole.AnswerKey
                || asset.FileName.Contains("answer", StringComparison.OrdinalIgnoreCase)
                || asset.FileName.Contains("key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (repository.GetExtractedTextBlocks(asset.AssetId).Any(block =>
                block.Text.Contains("answer", StringComparison.OrdinalIgnoreCase)
                || block.Text.Contains("đáp", StringComparison.OrdinalIgnoreCase)
                || block.Text.Contains("dap", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsQuestionNumber(string text, int questionStart, int questionEnd)
    {
        foreach (Match match in QuestionNumberPattern().Matches(text))
        {
            if (int.TryParse(match.Groups["number"].Value, out var number)
                && number >= questionStart
                && number <= questionEnd)
            {
                return true;
            }
        }

        return false;
    }

    [GeneratedRegex(@"(?<!\d)(?<number>\d{1,3})\s*[\.)]", RegexOptions.CultureInvariant)]
    private static partial Regex QuestionNumberPattern();

    private static int AssetRolePriority(SourceAssetRole role) => role switch
    {
        SourceAssetRole.Pdf => 0,
        SourceAssetRole.Document => 1,
        SourceAssetRole.WebPage => 2,
        SourceAssetRole.Transcript => 3,
        SourceAssetRole.AnswerKey => 4,
        SourceAssetRole.Audio => 5,
        SourceAssetRole.Image => 6,
        _ => 7,
    };
}

internal sealed record ToeicPartReadinessRule(
    int Part,
    string Name,
    int QuestionStart,
    int QuestionEnd,
    string RequiredEvidence,
    bool RequiresAudio,
    bool RequiresImage,
    bool RequiresTranscript,
    bool RequiresPassage,
    bool ParserImplemented
);

internal sealed record QuestionRangeEvidence(int AssetCount, int TextBlockCount);

internal static class ToeicPartReadinessRules
{
    public static readonly IReadOnlyList<ToeicPartReadinessRule> All = new ToeicPartReadinessRule[]
    {
        new(1, "Photographs", 1, 6, "image + audio + answer key", RequiresAudio: true, RequiresImage: true, RequiresTranscript: false, RequiresPassage: false, ParserImplemented: false),
        new(2, "Question-Response", 7, 31, "audio + answer key", RequiresAudio: true, RequiresImage: false, RequiresTranscript: true, RequiresPassage: false, ParserImplemented: false),
        new(3, "Conversations", 32, 70, "audio + transcript + grouped questions + answer key", RequiresAudio: true, RequiresImage: false, RequiresTranscript: true, RequiresPassage: false, ParserImplemented: false),
        new(4, "Talks", 71, 100, "audio + transcript + grouped questions + answer key", RequiresAudio: true, RequiresImage: false, RequiresTranscript: true, RequiresPassage: false, ParserImplemented: false),
        new(5, "Incomplete Sentences", 101, 130, "question text + options + answer key", RequiresAudio: false, RequiresImage: false, RequiresTranscript: false, RequiresPassage: false, ParserImplemented: true),
        new(6, "Text Completion", 131, 146, "passage + grouped questions + answer key", RequiresAudio: false, RequiresImage: false, RequiresTranscript: false, RequiresPassage: true, ParserImplemented: true),
        new(7, "Reading Comprehension", 147, 200, "passage + grouped questions + answer key", RequiresAudio: false, RequiresImage: false, RequiresTranscript: false, RequiresPassage: true, ParserImplemented: true),
    };
}
