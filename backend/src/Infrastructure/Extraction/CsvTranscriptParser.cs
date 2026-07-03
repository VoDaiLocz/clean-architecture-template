using Application.Common.Interfaces.Storage;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public sealed class CsvTranscriptParser(IObjectStorage objectStorage) : ITranscriptParser
{
    public IReadOnlyList<TranscriptSegmentResult> Parse(SourceAsset asset)
    {
        var storedObject = objectStorage.Get(new ObjectKey(asset.ObjectKey))
            ?? throw new InvalidOperationException($"Object not found in storage: {asset.ObjectKey}");

        var contentString = System.Text.Encoding.UTF8.GetString(storedObject.Content);
        var lines = contentString.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var results = new List<TranscriptSegmentResult>();

        foreach (var line in lines)
        {
            if (line.Contains("TestGroupId", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split(',');
            if (parts.Length >= 6 && 
                int.TryParse(parts[4], out var startSecond) && 
                int.TryParse(parts[5], out var endSecond))
            {
                results.Add(new TranscriptSegmentResult(
                    TestGroupId: parts[0].Trim(),
                    LinkedAudioAssetId: parts[1].Trim(),
                    SpeakerLabel: parts[2].Trim(),
                    Text: parts[3].Trim(),
                    StartSecond: startSecond,
                    EndSecond: endSecond,
                    Confidence: 1.0m
                ));
            }
        }

        return results;
    }
}
