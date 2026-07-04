using System.Text.RegularExpressions;
using Application.Common.Interfaces.Repositories;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public class ToeicTranscriptParser(IKnowledgeRepository repository) : ITranscriptParser
{
    private static readonly Regex SpeakerRegex = new Regex(@"^(M|W|Man|Woman)\s*:\s*(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<TranscriptSegmentResult> Parse(SourceAsset asset)
    {
        var blocks = repository.GetExtractedTextBlocks(asset.AssetId);
        var results = new List<TranscriptSegmentResult>();
        
        foreach (var block in blocks)
        {
            var lines = block.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var match = SpeakerRegex.Match(line.Trim());
                if (match.Success)
                {
                    results.Add(new TranscriptSegmentResult(
                        TestGroupId: "unknown-group",
                        LinkedAudioAssetId: "",
                        SpeakerLabel: match.Groups[1].Value.ToUpperInvariant(),
                        Text: match.Groups[2].Value.Trim(),
                        StartSecond: 0,
                        EndSecond: 0,
                        Confidence: 0.8m
                    ));
                }
            }
        }
        
        return results;
    }
}
