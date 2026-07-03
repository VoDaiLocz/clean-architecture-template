using Application.Common.Interfaces.Storage;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public sealed class CsvAnswerKeyParser(IObjectStorage objectStorage) : IAnswerKeyParser
{
    public IReadOnlyList<AnswerKeyMappingResult> Parse(SourceAsset asset)
    {
        var storedObject = objectStorage.Get(new ObjectKey(asset.ObjectKey))
            ?? throw new InvalidOperationException($"Object not found in storage: {asset.ObjectKey}");

        var contentString = System.Text.Encoding.UTF8.GetString(storedObject.Content);
        var lines = contentString.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        var results = new List<AnswerKeyMappingResult>();

        foreach (var line in lines)
        {
            if (line.Contains("TestId", StringComparison.OrdinalIgnoreCase))
                continue;

            var parts = line.Split(',');
            if (parts.Length >= 3 && int.TryParse(parts[1], out var qn))
            {
                results.Add(new AnswerKeyMappingResult(
                    TestId: parts[0].Trim(),
                    QuestionNumber: qn,
                    CorrectAnswer: parts[2].Trim(),
                    Confidence: 1.0m
                ));
            }
        }

        return results;
    }
}
