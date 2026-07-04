using System;
using System.Collections.Generic;
using System.Text.Json;
using Application.Common.Models;
using Domain.Aggregates.Corpus;

namespace Application.Features.PartEngines;

public class ToeicPart1Engine : IToeicPartEngine
{
    public bool SupportsPart(int part) => part == 1;

    public ToeicPlayableItem CreatePlayableItem(PublishedQuestion question)
    {
        Validate(question);

        var choices = ParseChoices(question.OptionsJson);

        return new ToeicPlayableItem
        {
            Id = question.QuestionId,
            Part = question.ToeicPart,
            Prompt = question.Prompt,
            Choices = choices,
            MediaRefs = new List<string> { question.MediaAssetId! },
            Payload = new Part1Payload()
        };
    }

    public ToeicReviewItem CreateReviewItem(PublishedQuestion question)
    {
        Validate(question);

        var choices = ParseChoices(question.OptionsJson);

        return new ToeicReviewItem
        {
            Id = question.QuestionId,
            Part = question.ToeicPart,
            Prompt = question.Prompt,
            Choices = choices,
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation,
            MediaRefs = new List<string> { question.MediaAssetId! },
            Payload = new Part1Payload()
        };
    }

    private static void Validate(PublishedQuestion question)
    {
        if (question.ToeicPart != 1)
            throw new InvalidOperationException("This engine only supports TOEIC Part 1.");

        if (string.IsNullOrWhiteSpace(question.MediaAssetId))
            throw new InvalidOperationException("Part 1 questions require a MediaAssetId.");
    }

    private static string[] ParseChoices(string optionsJson)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(optionsJson);
            if (dict == null || dict.Count != 4)
            {
                throw new InvalidOperationException("Part 1 questions must have exactly 4 choices.");
            }

            if (!dict.ContainsKey("A") || !dict.ContainsKey("B") || !dict.ContainsKey("C") || !dict.ContainsKey("D"))
            {
                throw new InvalidOperationException("Part 1 questions must have choices A, B, C, and D.");
            }

            return new[]
            {
                $"A. {dict["A"]}",
                $"B. {dict["B"]}",
                $"C. {dict["C"]}",
                $"D. {dict["D"]}"
            };
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Failed to parse options JSON.");
        }
    }
}
