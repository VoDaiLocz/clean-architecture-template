using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Infrastructure.Extraction;

public sealed class RegexReadingDraftParser : IReadingDraftParser
{
    private static readonly Regex QuestionRegex = new(
        @"(?<number>1[0-9]{2}|200)\.\s*(?<prompt>.+?)\s*(?:\(?A\)|A[.)])\s*(?<a>.+?)\s*(?:\(?B\)|B[.)]|\(8\)|8[.)])\s*(?<b>.+?)\s*(?:\(?C\)|C[.)])\s*(?<c>.+?)\s*(?:\(?D\)|D[.)]|\(0\)|0[.)])\s*(?<d>.+?)(?=(?:\s+1[0-9]{2}\.|\s+200\.|$))",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex AnswerKeyRegex = new(
        @"\b(?<number>1[0-9]{2}|200)\s*[\).:-]?\s*\(?(?<answer>[ABCD80])\)?\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex QuestionNumberRegex = new(
        @"\b(?<number>1[0-9]{2}|200)\.",
        RegexOptions.Compiled);

    private static readonly Regex VietnameseAnswerRegex = new(
        @"(?:(?:chọn\s+)?(?:đáp\s*án|dap\s*an)\s*(?:là)?|chọn)\s*[""“”']?(?<answer>[ABCD])\b",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex Part6BlankRegex = new(
        @"\((?<number>13[1-9]|14[0-6])\)\s*-+",
        RegexOptions.Compiled);

    private static readonly Regex OptionsOnlyRegex = new(
        @"(?:\(?A\)|A[.)])\s*(?<a>.+?)\s*(?:\(?B\)|B[.)])\s*(?<b>.+?)\s*(?:\(?C\)|C[.)])\s*(?<c>.+?)\s*(?:\(?D\)|D[.)])\s*(?<d>.+?)(?=$)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex ParenthesizedOptionsRegex = new(
        @"\(A\)\s*(?<a>.+?)\s*\(B\)\s*(?<b>.+?)\s*\(C\)\s*(?<c>.+?)\s*\(D\)\s*(?<d>.+?)(?=$)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex SingleAnswerRegex = new(
        @"^[\s\-–—:]*[""“”']?(?<answer>[ABCD])[""“”']?[\s\-–—:]*$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public IReadOnlyList<ReadingDraftQuestionResult> Parse(SourceAsset asset, IReadOnlyList<ExtractedTextBlock> blocks)
    {
        var results = new List<ReadingDraftQuestionResult>();
        var orderedBlocks = blocks
            .OrderBy(block => block.PageNumber)
            .ThenBy(block => ExtractBlockOrdinal(block.BlockId))
            .ThenBy(block => block.BlockId, StringComparer.Ordinal)
            .ToArray();
        var globalAnswerEvidence = BuildGlobalAnswerEvidence(orderedBlocks);

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

                var answerEvidence = FindNearbyAnswer(questionNumber, blockIndex, orderedBlocks)
                    ?? (globalAnswerEvidence.TryGetValue(questionNumber, out var globalAnswer)
                        ? globalAnswer
                        : null);
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

        results.AddRange(ParsePart6ClozePassages(asset, orderedBlocks, results));
        
        return results;
    }

    private sealed record AnswerEvidence(string Answer, string Explanation);

    private sealed record PassageEvidence(string PassageId, string PassageText);

    private sealed record OptionEvidence(
        IReadOnlyDictionary<string, string> Options,
        string CorrectAnswer,
        string Explanation
    );

    private static IReadOnlyList<ReadingDraftQuestionResult> ParsePart6ClozePassages(
        SourceAsset asset,
        IReadOnlyList<ExtractedTextBlock> blocks,
        IReadOnlyList<ReadingDraftQuestionResult> existingResults
    )
    {
        var existingNumbers = existingResults
            .Select(result => TryReadExtractedNumber(result.PayloadJson))
            .Where(number => number is not null)
            .Select(number => number!.Value)
            .ToHashSet();
        var results = new List<ReadingDraftQuestionResult>();

        for (var blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
        {
            var block = blocks[blockIndex];
            var passageText = NormalizeWhitespace(block.Text);
            foreach (Match blank in Part6BlankRegex.Matches(passageText))
            {
                var questionNumber = int.Parse(blank.Groups["number"].Value);
                if (existingNumbers.Contains(questionNumber))
                {
                    continue;
                }

                var optionEvidence = FindPart6ClozeOptions(questionNumber, blockIndex, blocks);
                if (optionEvidence is null)
                {
                    continue;
                }

                var passageEvidence = new PassageEvidence(
                    PassageId: $"passage-{SanitizeId(block.AssetId)}-{SanitizeId(block.BlockId)}",
                    PassageText: passageText
                );

                results.Add(new ReadingDraftQuestionResult(
                    ToeicPart: 6,
                    QuestionType: "TextCompletion",
                    Prompt: $"Complete blank ({questionNumber}) in the passage.",
                    SkillTags: ["part6", "reading", "text_completion"],
                    PayloadJson: JsonSerializer.Serialize(new
                    {
                        extractedNumber = questionNumber,
                        passageId = passageEvidence.PassageId,
                        passageText = passageEvidence.PassageText,
                        options = optionEvidence.Options,
                        correctAnswer = optionEvidence.CorrectAnswer,
                        explanation = optionEvidence.Explanation,
                        sourceAssetId = asset.AssetId,
                    }),
                    SourceBlockId: block.BlockId,
                    Confidence: Math.Min(0.95m, Math.Max(0.9m, block.Confidence))
                ));
                existingNumbers.Add(questionNumber);
            }
        }

        return results;
    }

    private static OptionEvidence? FindPart6ClozeOptions(
        int questionNumber,
        int passageBlockIndex,
        IReadOnlyList<ExtractedTextBlock> blocks
    )
    {
        var nearbyBlocks = blocks
            .Skip(passageBlockIndex + 1)
            .Take(16)
            .ToArray();
        var windowText = NormalizeWhitespace(string.Join(" ", nearbyBlocks.Select(block => block.Text)));
        var options = ParseOptions(windowText, requireParenthesizedLabels: true);
        if (options is null)
        {
            return null;
        }

        var answer = nearbyBlocks
            .Select(block => SingleAnswerRegex.Match(NormalizeWhitespace(block.Text)))
            .Where(match => match.Success)
            .Select(match => match.Groups["answer"].Value.ToUpperInvariant())
            .FirstOrDefault();

        if (string.IsNullOrWhiteSpace(answer))
        {
            var answerKeyMatch = AnswerKeyRegex.Matches(windowText)
                .FirstOrDefault(match => int.Parse(match.Groups["number"].Value) == questionNumber);
            answer = answerKeyMatch?.Groups["answer"].Value.ToUpperInvariant() ?? "";
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            return null;
        }

        var explanation = nearbyBlocks
            .Select(block => NormalizeWhitespace(block.Text))
            .FirstOrDefault(text =>
                text.Contains($"{questionNumber}.", StringComparison.Ordinal)
                && !ParenthesizedOptionsRegex.IsMatch(text)
            );

        return new OptionEvidence(
            Options: options,
            CorrectAnswer: answer,
            Explanation: string.IsNullOrWhiteSpace(explanation) ? $"Answer evidence: {answer}" : explanation
        );
    }

    private static IReadOnlyDictionary<string, string>? ParseOptions(
        string text,
        bool requireParenthesizedLabels = false
    )
    {
        var normalized = NormalizeWhitespace(text);
        var match = requireParenthesizedLabels
            ? ParenthesizedOptionsRegex.Match(normalized)
            : OptionsOnlyRegex.Match(normalized);
        if (!match.Success)
        {
            return null;
        }

        var options = new Dictionary<string, string>
        {
            ["A"] = CleanOption(match.Groups["a"].Value),
            ["B"] = CleanOption(match.Groups["b"].Value),
            ["C"] = CleanOption(match.Groups["c"].Value),
            ["D"] = CleanOption(match.Groups["d"].Value),
        };

        return options.Values.Any(string.IsNullOrWhiteSpace) ? null : options;
    }

    private static int? TryReadExtractedNumber(string payloadJson)
    {
        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.TryGetProperty("extractedNumber", out var number)
                && number.TryGetInt32(out var value)
                    ? value
                    : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

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
                            NormalizeAnswer(match.Groups["answer"].Value),
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

    private static IReadOnlyDictionary<int, AnswerEvidence> BuildGlobalAnswerEvidence(
        IReadOnlyList<ExtractedTextBlock> blocks
    )
    {
        var answers = new Dictionary<int, AnswerEvidence>();
        foreach (var block in blocks)
        {
            var text = NormalizeWhitespace(block.Text);
            if (!ContainsAnswerKeyMarker(text))
            {
                continue;
            }

            foreach (Match match in AnswerKeyRegex.Matches(text))
            {
                var number = int.Parse(match.Groups["number"].Value);
                answers.TryAdd(number, new AnswerEvidence(
                    NormalizeAnswer(match.Groups["answer"].Value),
                    text
                ));
            }
        }

        return answers;
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

            var passageText = BuildPassageTextFromMarker(index, questionBlockIndex, blocks);
            return new PassageEvidence(
                PassageId: $"passage-{SanitizeId(block.AssetId)}-{SanitizeId(block.BlockId)}",
                PassageText: string.IsNullOrWhiteSpace(passageText) ? text : passageText
            );
        }

        return null;
    }

    private static string BuildPassageTextFromMarker(
        int markerBlockIndex,
        int questionBlockIndex,
        IReadOnlyList<ExtractedTextBlock> blocks
    )
    {
        var parts = new List<string>();
        for (var index = markerBlockIndex; index < questionBlockIndex; index++)
        {
            var text = NormalizeWhitespace(blocks[index].Text);
            if (string.IsNullOrWhiteSpace(text)
                || text.Length < 12
                || QuestionRegex.IsMatch(text)
                || ContainsBoilerplate(text))
            {
                continue;
            }

            parts.Add(text);
        }

        return NormalizeWhitespace(string.Join(" ", parts));
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
        || text.Contains("dap", StringComparison.OrdinalIgnoreCase)
        || Regex.IsMatch(text, @"\b1[5-9]\d\.\s*\([ABCD80]\)", RegexOptions.IgnoreCase);

    private static bool ContainsBoilerplate(string text) =>
        text.Contains("Go on to the next page", StringComparison.OrdinalIgnoreCase)
        || text.Contains("Facebook.com", StringComparison.OrdinalIgnoreCase)
        || text.Contains("BenzenEnglish", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeAnswer(string answer) => answer.ToUpperInvariant() switch
    {
        "8" => "B",
        "0" => "D",
        var value => value,
    };

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
