using System;
using System.Collections.Generic;
using System.Text.Json;
using Application.Common.Models;
using Domain.Aggregates.Corpus;

namespace Application.Features.PartEngines;

public class ToeicPart2Engine : IToeicPartEngine
{
    public bool SupportsPart(int part) => part == 2;

    public ToeicPlayableItem CreatePlayableItem(PublishedQuestion question)
    {
        Validate(question);

        var choices = ParseChoices(question.OptionsJson);

        return new ToeicPlayableItem
        {
            Id = question.QuestionId,
            Part = question.ToeicPart,
            Prompt = null, // Hide prompt for Part 2 in playable item
            Choices = choices,
            MediaRefs = new List<string> { question.MediaAssetId! },
            Payload = new Part2Payload()
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
            Prompt = question.Prompt, // Real prompt included in review item
            Choices = choices,
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation,
            MediaRefs = new List<string> { question.MediaAssetId! },
            Payload = new Part2Payload()
        };
    }

    private static void Validate(PublishedQuestion question)
    {
        if (question.ToeicPart != 2)
            throw new InvalidOperationException("This engine only supports TOEIC Part 2.");

        if (string.IsNullOrWhiteSpace(question.MediaAssetId))
            throw new InvalidOperationException("Part 2 questions require a MediaAssetId.");
    }

    private static string[] ParseChoices(string optionsJson)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(optionsJson);
            if (dict == null || dict.Count != 3)
            {
                throw new InvalidOperationException("Part 2 questions must have exactly 3 choices.");
            }

            if (!dict.ContainsKey("A") || !dict.ContainsKey("B") || !dict.ContainsKey("C"))
            {
                throw new InvalidOperationException("Part 2 questions must have choices A, B, and C.");
            }

            return new[]
            {
                $"A. {dict["A"]}",
                $"B. {dict["B"]}",
                $"C. {dict["C"]}"
            };
        }
        catch (JsonException)
        {
            throw new InvalidOperationException("Failed to parse options JSON.");
        }
    }
}
