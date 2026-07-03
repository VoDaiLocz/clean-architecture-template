using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure.Extraction;

public sealed class RegexReadingDraftParser : IReadingDraftParser
{
    private static readonly Regex QuestionRegex = new(@"^\s*(1[0-9]{2}|200)\.\s+(.*)", RegexOptions.Compiled);

    public IReadOnlyList<ReadingDraftQuestionResult> Parse(SourceAsset asset, IReadOnlyList<ExtractedTextBlock> blocks)
    {
        var results = new List<ReadingDraftQuestionResult>();

        foreach (var block in blocks)
        {
            var lines = block.Text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var match = QuestionRegex.Match(line);
                if (match.Success)
                {
                    int qNum = int.Parse(match.Groups[1].Value);
                    int part = (qNum >= 101 && qNum <= 130) ? 5 : ((qNum >= 131 && qNum <= 146) ? 6 : 7);
                    
                    results.Add(new ReadingDraftQuestionResult(
                        ToeicPart: part,
                        QuestionType: "MultipleChoice",
                        Prompt: match.Groups[2].Value.Trim(),
                        SkillTags: Array.Empty<string>(),
                        PayloadJson: JsonSerializer.Serialize(new { extractedNumber = qNum }),
                        SourceBlockId: block.BlockId,
                        Confidence: 0.8m
                    ));
                }
            }
        }
        
        return results;
    }
}
