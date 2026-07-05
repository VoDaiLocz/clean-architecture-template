using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure.Extraction;

public sealed class RegexReadingDraftParser : IReadingDraftParser
{
    private static readonly Regex QuestionRegex = new(
        @"(?<number>1[0-9]{2}|200)\.\s*(?<prompt>.+?)\s*(?:\(?A\)|A[.)])\s*(?<a>.+?)\s*(?:\(?B\)|B[.)])\s*(?<b>.+?)\s*(?:\(?C\)|C[.)])\s*(?<c>.+?)\s*(?:\(?D\)|D[.)])\s*(?<d>.+?)(?=(?:\s+1[0-9]{2}\.|\s+200\.|$))",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex AnswerKeyRegex = new(
        @"\b(?<number>1[0-9]{2}|200)\s*[\).:-]?\s*(?<answer>[ABCD])\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex QuestionNumberRegex = new(
        @"\b(?<number>1[0-9]{2}|200)\.",
        RegexOptions.Compiled);

    private static readonly Regex VietnameseAnswerRegex = new(
        @"(?:(?:chọn\s+)?(?:đáp\s*án|dap\s*an)\s*(?:là)?|chọn)\s*[""“”']?(?<answer>[ABCD])\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<ReadingDraftQuestionResult> Parse(SourceAsset asset, IReadOnlyList<ExtractedTextBlock> blocks)
    {
        var results = new List<ReadingDraftQuestionResult>();
        var orderedBlocks = blocks
            .OrderBy(block => block.PageNumber)
            .ThenBy(block => ExtractBlockOrdinal(block.BlockId))
            .ThenBy(block => block.BlockId, StringComparer.Ordinal)
            .ToArray();

        for (var blockIndex = 0; blockIndex < orderedBlocks.Length; blockIndex++)
        {
            var block = orderedBlocks[blockIndex];
            var normalized = NormalizeWhitespace(block.Text);
            foreach (Match match in QuestionRegex.Matches(normalized))
            {
                var questionNumber = int.Parse(match.Groups["number"].Value);
                var toeicPart = GetReadingPart(questionNumber);
                if (toeicPart is null)
                {
                    continue;
                }

                var answerEvidence = FindNearbyAnswer(questionNumber, blockIndex, orderedBlocks);
                if (answerEvidence is null)
                {
                    continue;
                }

                var options = new Dictionary<string, string>
                {
                    ["A"] = CleanOption(match.Groups["a"].Value),
                    ["B"] = CleanOption(match.Groups["b"].Value),
                    ["C"] = CleanOption(match.Groups["c"].Value),
                    ["D"] = CleanOption(match.Groups["d"].Value),
                };

                if (options.Values.Any(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                var prompt = CleanPrompt(match.Groups["prompt"].Value);
                var passageEvidence = FindNearbyPassage(questionNumber, blockIndex, orderedBlocks);
                if (toeicPart is 6 or 7 && passageEvidence is null)
                {
                    continue;
                }

                results.Add(new ReadingDraftQuestionResult(
                    ToeicPart: toeicPart.Value,
                    QuestionType: GetQuestionType(toeicPart.Value),
                    Prompt: prompt,
                    SkillTags: InferSkillTags(toeicPart.Value, prompt, options),
                    PayloadJson: SerializeParserPayload(
                        asset,
                        questionNumber,
                        options,
                        answerEvidence,
                        passageEvidence
                    ),
                    SourceBlockId: block.BlockId,
                    Confidence: Math.Min(0.95m, Math.Max(0.9m, block.Confidence))
                ));
            }
        }
        
        return results;
    }

    private sealed record AnswerEvidence(string Answer, string Explanation);

    private sealed record PassageEvidence(string PassageId, string PassageText);

    private static AnswerEvidence? FindNearbyAnswer(
        int questionNumber,
        int questionBlockIndex,
        IReadOnlyList<ExtractedTextBlock> blocks
    )
    {
        var questionPage = blocks[questionBlockIndex].PageNumber;
        for (var index = questionBlockIndex; index < blocks.Count && index <= questionBlockIndex + 6; index++)
        {
            var block = blocks[index];
            if (block.PageNumber > questionPage + 2)
            {
                break;
            }

            var text = NormalizeWhitespace(block.Text);
            if (index > questionBlockIndex
                && StartsDifferentQuestion(text, questionNumber))
            {
                break;
            }

            if (ContainsAnswerKeyMarker(text))
            {
                foreach (Match match in AnswerKeyRegex.Matches(text))
                {
                    if (int.Parse(match.Groups["number"].Value) == questionNumber)
                    {
                        return new AnswerEvidence(
                            match.Groups["answer"].Value.ToUpperInvariant(),
                            text
                        );
                    }
                }
            }

            var vietnameseAnswer = VietnameseAnswerRegex.Match(text);
            if (vietnameseAnswer.Success)
            {
                return new AnswerEvidence(
                    vietnameseAnswer.Groups["answer"].Value.ToUpperInvariant(),
                    text
                );
            }
        }

        return null;
    }

    private static PassageEvidence? FindNearbyPassage(
        int questionNumber,
        int questionBlockIndex,
        IReadOnlyList<ExtractedTextBlock> blocks
    )
    {
        for (var index = questionBlockIndex - 1; index >= 0 && index >= questionBlockIndex - 6; index--)
        {
            var block = blocks[index];
            var text = NormalizeWhitespace(block.Text);
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }

            if (StartsDifferentQuestion(text, questionNumber)
                || ContainsAnswerKeyMarker(text))
            {
                continue;
            }

            if (!LooksLikePassage(text, questionNumber))
            {
                continue;
            }

            return new PassageEvidence(
                PassageId: $"passage-{SanitizeId(block.AssetId)}-{SanitizeId(block.BlockId)}",
                PassageText: text
            );
        }

        return null;
    }

    private static bool StartsDifferentQuestion(string text, int questionNumber)
    {
        var match = QuestionNumberRegex.Match(text);
        return match.Success && int.Parse(match.Groups["number"].Value) != questionNumber;
    }

    private static bool ContainsAnswerKeyMarker(string text) =>
        text.Contains("answer", StringComparison.OrdinalIgnoreCase)
        || text.Contains("key", StringComparison.OrdinalIgnoreCase)
        || text.Contains("đáp", StringComparison.OrdinalIgnoreCase)
        || text.Contains("dap", StringComparison.OrdinalIgnoreCase);

    private static int? GetReadingPart(int questionNumber) => questionNumber switch
    {
        >= 101 and <= 130 => 5,
        >= 131 and <= 146 => 6,
        >= 147 and <= 200 => 7,
        _ => null,
    };

    private static string GetQuestionType(int toeicPart) => toeicPart switch
    {
        5 => "IncompleteSentence",
        6 => "TextCompletion",
        7 => "ReadingComprehension",
        _ => throw new ArgumentOutOfRangeException(nameof(toeicPart), toeicPart, "Unsupported TOEIC reading part."),
    };

    private static string SerializeParserPayload(
        SourceAsset asset,
        int questionNumber,
        IReadOnlyDictionary<string, string> options,
        AnswerEvidence answerEvidence,
        PassageEvidence? passageEvidence
    )
    {
        if (passageEvidence is null)
        {
            return JsonSerializer.Serialize(new
            {
                extractedNumber = questionNumber,
                options,
                correctAnswer = answerEvidence.Answer,
                explanation = answerEvidence.Explanation,
            });
        }

        return JsonSerializer.Serialize(new
        {
            extractedNumber = questionNumber,
            passageId = passageEvidence.PassageId,
            passageText = passageEvidence.PassageText,
            options,
            correctAnswer = answerEvidence.Answer,
            explanation = answerEvidence.Explanation,
            sourceAssetId = asset.AssetId,
        });
    }

    private static bool LooksLikePassage(string text, int questionNumber)
    {
        if (text.Length < 40)
        {
            return false;
        }

        if (text.Contains("refer to the following", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var questionRange = Regex.Match(text, @"Questions?\s+(?<start>\d{1,3})\s*[-–]\s*(?<end>\d{1,3})", RegexOptions.IgnoreCase);
        return questionRange.Success
            && int.TryParse(questionRange.Groups["start"].Value, out var start)
            && int.TryParse(questionRange.Groups["end"].Value, out var end)
            && questionNumber >= start
            && questionNumber <= end;
    }

    private static string SanitizeId(string value) =>
        Regex.Replace(value, @"[^A-Za-z0-9_-]+", "-").Trim('-');

    private static string NormalizeWhitespace(string value) =>
        Regex.Replace(value, @"\s+", " ").Trim();

    private static string CleanPrompt(string value) =>
        NormalizeWhitespace(value);

    private static string CleanOption(string value)
    {
        var cleaned = NormalizeWhitespace(value);
        return Regex.Replace(cleaned, @"\s+$", string.Empty);
    }

    private static IReadOnlyList<string> InferSkillTags(int toeicPart, string prompt, IReadOnlyDictionary<string, string> options)
    {
        var tags = new List<string> { $"part{toeicPart}" };
        if (toeicPart == 5)
        {
            tags.Add("grammar");
        }
        else
        {
            tags.Add("reading");
        }

        if (toeicPart == 5
            && prompt.Contains("____", StringComparison.Ordinal)
            && LooksLikeWordFormSet(options.Values))
        {
            tags.Add("word_form");
        }

        return tags;
    }

    private static bool LooksLikeWordFormSet(IEnumerable<string> options)
    {
        var normalized = options
            .Select(option => Regex.Replace(option.ToLowerInvariant(), @"(ly|ive|ion|ness|ment|ed|ing|s)$", string.Empty))
            .Where(option => option.Length >= 4)
            .ToArray();

        return normalized
            .GroupBy(option => option)
            .Any(group => group.Count() >= 2);
    }

    private static int ExtractBlockOrdinal(string blockId)
    {
        var match = Regex.Match(blockId, @"-(?<ordinal>\d+)$");
        return match.Success ? int.Parse(match.Groups["ordinal"].Value) : int.MaxValue;
    }
}
