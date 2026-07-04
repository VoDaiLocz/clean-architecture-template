using System.Text.RegularExpressions;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction.Profiles;

public class DefaultToeicParserProfile : IToeicParserProfile
{
    private static readonly Regex AnswerRegex = new Regex(@"(\d{1,3})\s*[.:-]?\s*([A-D])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool CanParse(SourceAsset asset) => true; // Default fallback

    public IReadOnlyList<AnswerKeyMappingResult> ParseAnswerKeys(IReadOnlyList<ExtractedTextBlock> blocks)
    {
        var results = new List<AnswerKeyMappingResult>();
        foreach (var block in blocks)
        {
            var matches = AnswerRegex.Matches(block.Text);
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int qNum))
                {
                    results.Add(new AnswerKeyMappingResult("unknown-test", qNum, match.Groups[2].Value.ToUpperInvariant(), 0.5m));
                }
            }
        }
        decimal finalConfidence = (results.Count == 100 || results.Count == 200) ? 0.9m : 0.5m;
        return results.Select(r => r with { Confidence = finalConfidence }).ToList();
    }
}
