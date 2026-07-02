using System.Text.Json;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceExtraction;

public sealed record ParseToeicTranscriptsCommand(string AssetId);

public sealed record ParseToeicTranscriptsResult(int CreatedTranscriptSegmentCount);

public interface ITranscriptParser
{
    IReadOnlyList<TranscriptSegmentResult> Parse(SourceAsset asset);
}

public sealed record TranscriptSegmentResult(
    string TestGroupId,
    string LinkedAudioAssetId,
    string SpeakerLabel,
    string Text,
    int StartSecond,
    int EndSecond,
    decimal Confidence
);

public sealed class ParseToeicTranscriptsHandler(
    IKnowledgeRepository repository,
    ITranscriptParser parser
)
{
    public ParseToeicTranscriptsResult Handle(ParseToeicTranscriptsCommand command)
    {
        var asset = repository.GetSourceAsset(command.AssetId)
            ?? throw new InvalidOperationException($"Source asset not found: {command.AssetId}");

        if (asset.DetectedRole != SourceAssetRole.Transcript)
        {
            throw new InvalidOperationException("Only transcript source assets can be parsed by this handler.");
        }

        var count = 0;
        foreach (var segment in parser.Parse(asset))
        {
            count++;
            repository.UpsertDraftContentItem(new DraftContentItem(
                DraftId: $"draft-transcript-{asset.AssetId}-{count}",
                AssetId: asset.AssetId,
                MaterialClass: MaterialClass.TestBook,
                ToeicPart: null,
                ItemType: "TranscriptSegment",
                PayloadJson: JsonSerializer.Serialize(new
                {
                    testGroupId = segment.TestGroupId,
                    linkedAudioAssetId = segment.LinkedAudioAssetId,
                    speakerLabel = segment.SpeakerLabel,
                    text = segment.Text,
                    startSecond = segment.StartSecond,
                    endSecond = segment.EndSecond,
                }),
                SourceTraceJson: JsonSerializer.Serialize(new
                {
                    asset.AssetId,
                    asset.SourceId,
                    asset.ProviderUrl,
                }),
                ParserConfidence: segment.Confidence,
                Status: DraftContentStatus.PendingValidation
            ));
        }

        return new ParseToeicTranscriptsResult(count);
    }
}
