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
        var existingDraftIds = repository.GetDraftContentItems(asset.AssetId)
            .Select(draft => draft.DraftId)
            .ToHashSet(StringComparer.Ordinal);
        var count = 0;
        var created = 0;
        foreach (var question in parser.Parse(asset, blocks))
        {
            count++;
            var draftId = $"draft-reading-{asset.AssetId}-{count}";
            if (existingDraftIds.Contains(draftId))
            {
                continue;
            }

            repository.UpsertDraftContentItem(new DraftContentItem(
                DraftId: draftId,
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
            existingDraftIds.Add(draftId);
            created++;
        }

        return new ParseToeicReadingDraftsResult(created);
    }

    private static string MergePayload(ReadingDraftQuestionResult question) =>
        DraftPayloadEnvelope.Serialize("ReadingQuestion", new
        {
            questionType = question.QuestionType,
            prompt = question.Prompt,
            skillTags = question.SkillTags,
            parserPayload = JsonSerializer.Deserialize<object>(question.PayloadJson),
        });
}
