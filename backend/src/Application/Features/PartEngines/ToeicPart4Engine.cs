using System;
using System.Collections.Generic;
using System.Text.Json;
using Application.Common.Models;
using Domain.Aggregates.Corpus;

namespace Application.Features.PartEngines;

public class ToeicPart4Engine : IToeicPartEngine
{
    public bool SupportsPart(int part) => part == 4;

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
            GroupRef = question.GroupId,
            Payload = new Part4Payload()
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
            GroupRef = question.GroupId,
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation,
            Payload = new Part4Payload()
        };
    }

    private static void Validate(PublishedQuestion question)
    {
        if (question.ToeicPart != 4)
            throw new InvalidOperationException("This engine only supports TOEIC Part 4.");

        if (string.IsNullOrWhiteSpace(question.GroupId))
            throw new InvalidOperationException("Part 4 questions require a GroupId.");
    }

    private static string[] ParseChoices(string optionsJson)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(optionsJson);
            if (dict == null || dict.Count != 4)
            {
                throw new InvalidOperationException("Part 4 questions must have exactly 4 choices.");
            }

            if (!dict.ContainsKey("A") || !dict.ContainsKey("B") || !dict.ContainsKey("C") || !dict.ContainsKey("D"))
            {
                throw new InvalidOperationException("Part 4 questions must have choices A, B, C, and D.");
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
