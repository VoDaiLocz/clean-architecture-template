using System.Text.Json;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceExtraction;

public sealed record ParseToeicReadingDraftsCommand(string AssetId);

public sealed record ParseToeicReadingDraftsResult(int CreatedReadingDraftCount);

public interface IReadingDraftParser
{
    IReadOnlyList<ReadingDraftQuestionResult> Parse(SourceAsset asset, IReadOnlyList<ExtractedTextBlock> blocks);
}

public sealed record ReadingDraftQuestionResult(
    int ToeicPart,
    string QuestionType,
    string Prompt,
    IReadOnlyList<string> SkillTags,
    string PayloadJson,
    string SourceBlockId,
    decimal Confidence
);

public sealed class ParseToeicReadingDraftsHandler(
    IKnowledgeRepository repository,
    IReadingDraftParser parser
)
{
    public ParseToeicReadingDraftsResult Handle(ParseToeicReadingDraftsCommand command)
    {
        var asset = repository.GetSourceAsset(command.AssetId)
            ?? throw new InvalidOperationException($"Source asset not found: {command.AssetId}");

        if (asset.DetectedRole != SourceAssetRole.Pdf)
        {
            throw new InvalidOperationException("Reading drafts require a PDF source asset.");
        }

        var blocks = repository.GetExtractedTextBlocks(asset.AssetId);
        var count = 0;
        foreach (var question in parser.Parse(asset, blocks))
        {
            count++;
            repository.UpsertDraftContentItem(new DraftContentItem(
                DraftId: $"draft-reading-{asset.AssetId}-{count}",
                AssetId: asset.AssetId,
                MaterialClass: MaterialClass.TestBook,
                ToeicPart: question.ToeicPart,
                ItemType: "ReadingQuestion",
                PayloadJson: MergePayload(question),
                SourceTraceJson: JsonSerializer.Serialize(new
                {
                    asset.AssetId,
                    asset.SourceId,
                    question.SourceBlockId,
                }),
                ParserConfidence: question.Confidence,
                Status: DraftContentStatus.PendingValidation
            ));
        }

        return new ParseToeicReadingDraftsResult(count);
    }

    private static string MergePayload(ReadingDraftQuestionResult question) =>
        JsonSerializer.Serialize(new
        {
            questionType = question.QuestionType,
            prompt = question.Prompt,
            skillTags = question.SkillTags,
            parserPayload = JsonSerializer.Deserialize<object>(question.PayloadJson),
        });
}
