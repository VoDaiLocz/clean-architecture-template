using System;
using System.Collections.Generic;
using System.Text.Json;
using Application.Common.Models;
using Domain.Aggregates.Corpus;

namespace Application.Features.PartEngines;

public class ToeicPart5Engine : IToeicPartEngine
{
    public bool SupportsPart(int part) => part == 5;

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
            Payload = new Part5Payload()
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
            Payload = new Part5Payload()
        };
    }

    private static void Validate(PublishedQuestion question)
    {
        if (question.ToeicPart != 5)
            throw new InvalidOperationException("This engine only supports TOEIC Part 5.");

        if (string.IsNullOrWhiteSpace(question.Prompt))
            throw new InvalidOperationException("Part 5 questions require a prompt.");
    }

    private static string[] ParseChoices(string optionsJson)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(optionsJson);
            if (dict == null || dict.Count != 4)
            {
                throw new InvalidOperationException("Part 5 questions must have exactly 4 choices.");
            }

            if (!dict.ContainsKey("A") || !dict.ContainsKey("B") || !dict.ContainsKey("C") || !dict.ContainsKey("D"))
            {
                throw new InvalidOperationException("Part 5 questions must have choices A, B, C, and D.");
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
