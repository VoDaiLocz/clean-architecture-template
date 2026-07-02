using System.Text.Json;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceExtraction;

public sealed record ParseToeicAnswerKeysCommand(string AssetId);

public sealed record ParseToeicAnswerKeysResult(int CreatedDraftMappingCount);

public interface IAnswerKeyParser
{
    IReadOnlyList<AnswerKeyMappingResult> Parse(SourceAsset asset);
}

public sealed record AnswerKeyMappingResult(
    string TestId,
    int QuestionNumber,
    string CorrectAnswer,
    decimal Confidence
);

public sealed class ParseToeicAnswerKeysHandler(
    IKnowledgeRepository repository,
    IAnswerKeyParser parser
)
{
    public ParseToeicAnswerKeysResult Handle(ParseToeicAnswerKeysCommand command)
    {
        var asset = repository.GetSourceAsset(command.AssetId)
            ?? throw new InvalidOperationException($"Source asset not found: {command.AssetId}");

        if (asset.DetectedRole != SourceAssetRole.AnswerKey)
        {
            throw new InvalidOperationException("Only answer-key source assets can be parsed by this handler.");
        }

        var count = 0;
        foreach (var mapping in parser.Parse(asset))
        {
            repository.UpsertDraftContentItem(new DraftContentItem(
                DraftId: $"draft-answer-key-{asset.AssetId}-{mapping.TestId}-{mapping.QuestionNumber}",
                AssetId: asset.AssetId,
                MaterialClass: MaterialClass.TestBook,
                ToeicPart: null,
                ItemType: "AnswerKeyMapping",
                PayloadJson: JsonSerializer.Serialize(new
                {
                    testId = mapping.TestId,
                    questionNumber = mapping.QuestionNumber,
                    correctAnswer = mapping.CorrectAnswer,
                }),
                SourceTraceJson: JsonSerializer.Serialize(new
                {
                    asset.AssetId,
                    asset.SourceId,
                    asset.ProviderUrl,
                }),
                ParserConfidence: mapping.Confidence,
                Status: DraftContentStatus.PendingValidation
            ));
            count++;
        }

        return new ParseToeicAnswerKeysResult(count);
    }
}
