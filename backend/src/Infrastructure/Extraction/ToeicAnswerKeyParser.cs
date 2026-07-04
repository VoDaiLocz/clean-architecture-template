using System.Text.RegularExpressions;
using Application.Common.Interfaces.Repositories;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public class ToeicAnswerKeyParser(IKnowledgeRepository repository) : IAnswerKeyParser
{
    private static readonly Regex AnswerRegex = new Regex(@"(\d{1,3})\s*[.:-]?\s*([A-D])", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<AnswerKeyMappingResult> Parse(SourceAsset asset)
    {
        var blocks = repository.GetExtractedTextBlocks(asset.AssetId);
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
        
        // Mastery check: if total is exactly 100 or 200, boost confidence
        decimal finalConfidence = (results.Count == 100 || results.Count == 200) ? 0.9m : 0.5m;
        
        return results.Select(r => r with { Confidence = finalConfidence }).ToList();
    }
}
