using System.Text.Json;
using System.Text.RegularExpressions;
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
        var existingDrafts = repository.GetDraftContentItems(asset.AssetId);
        var existingDraftIds = existingDrafts
            .Select(draft => draft.DraftId)
            .ToHashSet(StringComparer.Ordinal);
        var existingSemanticKeys = existingDrafts
            .Select(TryBuildSemanticKey)
            .Where(key => key is not null)
            .Select(key => key!)
            .ToHashSet();
        var created = 0;
        foreach (var question in parser.Parse(asset, blocks))
        {
            var semanticKey = BuildSemanticKey(question);
            if (existingSemanticKeys.Contains(semanticKey))
            {
                continue;
            }

            var draftId = BuildDraftId(asset.AssetId, semanticKey);
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
            existingSemanticKeys.Add(semanticKey);
            created++;
        }

        return new ParseToeicReadingDraftsResult(created);
    }

    private sealed record ReadingDraftSemanticKey(int ToeicPart, string SourceBlockId, int? ExtractedNumber);

    private static ReadingDraftSemanticKey BuildSemanticKey(ReadingDraftQuestionResult question) =>
        new(question.ToeicPart, question.SourceBlockId, TryReadExtractedNumber(question.PayloadJson));

    private static ReadingDraftSemanticKey? TryBuildSemanticKey(DraftContentItem draft)
    {
        var sourceBlockId = TryReadSourceBlockId(draft.SourceTraceJson);
        if (string.IsNullOrWhiteSpace(sourceBlockId) || draft.ToeicPart is null)
        {
            return null;
        }

        return new ReadingDraftSemanticKey(
            draft.ToeicPart.Value,
            sourceBlockId,
            TryReadExtractedNumber(draft.PayloadJson)
        );
    }

    private static string BuildDraftId(string assetId, ReadingDraftSemanticKey semanticKey)
    {
        var questionNumber = semanticKey.ExtractedNumber?.ToString() ?? "unknown";
        return $"draft-reading-{SanitizeId(assetId)}-part{semanticKey.ToeicPart}-{questionNumber}-{SanitizeId(semanticKey.SourceBlockId)}";
    }

    private static int? TryReadExtractedNumber(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            var root = document.RootElement;
            if (TryReadInt(root, "extractedNumber", out var rootNumber))
            {
                return rootNumber;
            }

            if (root.TryGetProperty("data", out var data))
            {
                if (TryReadInt(data, "extractedNumber", out var dataNumber))
                {
                    return dataNumber;
                }

                if (data.TryGetProperty("parserPayload", out var parserPayload)
                    && TryReadInt(parserPayload, "extractedNumber", out var parserNumber))
                {
                    return parserNumber;
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static string? TryReadSourceBlockId(string sourceTraceJson)
    {
        try
        {
            using var document = JsonDocument.Parse(sourceTraceJson);
            var root = document.RootElement;
            foreach (var propertyName in new[] { "sourceBlockId", "SourceBlockId" })
            {
                if (root.TryGetProperty(propertyName, out var value)
                    && value.ValueKind == JsonValueKind.String)
                {
                    return value.GetString();
                }
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static bool TryReadInt(JsonElement element, string propertyName, out int value)
    {
        value = 0;
        return element.TryGetProperty(propertyName, out var number)
            && number.ValueKind == JsonValueKind.Number
            && number.TryGetInt32(out value);
    }

    private static string SanitizeId(string value) =>
        Regex.Replace(value, @"[^A-Za-z0-9_-]+", "-").Trim('-');

    private static string MergePayload(ReadingDraftQuestionResult question) =>
        DraftPayloadEnvelope.Serialize("ReadingQuestion", new
        {
            questionType = question.QuestionType,
            prompt = question.Prompt,
            skillTags = question.SkillTags,
            parserPayload = JsonSerializer.Deserialize<object>(question.PayloadJson),
        });
}
