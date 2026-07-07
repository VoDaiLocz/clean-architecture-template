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
        var audioAssetId = ResolveAudioAssetId(question);
        var imageAssetId = question.MediaAssetId!;

        return new ToeicPlayableItem
        {
            Id = question.QuestionId,
            Part = question.ToeicPart,
            Prompt = question.Prompt,
            Choices = choices,
            MediaRefs = new List<string> { imageAssetId, audioAssetId },
            Payload = new Part1Payload
            {
                ImageAssetId = imageAssetId,
                AudioAssetId = audioAssetId,
            }
        };
    }

    public ToeicReviewItem CreateReviewItem(PublishedQuestion question)
    {
        Validate(question);

        var choices = ParseChoices(question.OptionsJson);
        var audioAssetId = ResolveAudioAssetId(question);
        var imageAssetId = question.MediaAssetId!;

        return new ToeicReviewItem
        {
            Id = question.QuestionId,
            Part = question.ToeicPart,
            Prompt = question.Prompt,
            Choices = choices,
            CorrectAnswer = question.CorrectAnswer,
            Explanation = question.Explanation,
            MediaRefs = new List<string> { imageAssetId, audioAssetId },
            Payload = new Part1Payload
            {
                ImageAssetId = imageAssetId,
                AudioAssetId = audioAssetId,
            }
        };
    }

    private static void Validate(PublishedQuestion question)
    {
        if (question.ToeicPart != 1)
            throw new InvalidOperationException("This engine only supports TOEIC Part 1.");

        if (string.IsNullOrWhiteSpace(question.MediaAssetId))
            throw new InvalidOperationException("Part 1 questions require an image MediaAssetId.");

        _ = ResolveAudioAssetId(question);
    }

    private static string ResolveAudioAssetId(PublishedQuestion question)
    {
        return TryReadStringProperty(question.EvidenceJson, "audioAssetId")
            ?? TryReadStringProperty(question.SourceTraceJson, "audioAssetId")
            ?? throw new InvalidOperationException("Part 1 questions require an audioAssetId in evidence/source trace.");
    }

    private static string? TryReadStringProperty(string json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return TryFindString(document.RootElement, propertyName);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? TryFindString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName)
                    && property.Value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(property.Value.GetString()))
                {
                    return property.Value.GetString();
                }

                var nested = TryFindString(property.Value, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                var nested = TryFindString(item, propertyName);
                if (!string.IsNullOrWhiteSpace(nested))
                {
                    return nested;
                }
            }
        }

        return null;
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
