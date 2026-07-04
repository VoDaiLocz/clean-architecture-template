using System;
using System.Text.Json;
using Application.Common.Models;
using Application.Features.PartEngines;
using Domain.Aggregates.Corpus;

public static class ToeicPart5EngineTests
{
    private static PublishedQuestion CreateValidQuestion()
    {
        return new PublishedQuestion(
            QuestionId: "q-1",
            LessonId: "l-1",
            ToeicPart: 5,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: "The manager ___ the report yesterday.",
            OptionsJson: "{\"A\":\"review\",\"B\":\"reviews\",\"C\":\"reviewed\",\"D\":\"reviewing\"}",
            CorrectAnswer: "C",
            Explanation: "The sentence is in the past tense.",
            MediaAssetId: null,
            PassageId: null,
            GroupId: null,
            EvidenceJson: "{}",
            SkillTags: "[]",
            SourceTraceJson: "{}",
            Status: PublishedContentStatus.Published
        );
    }

    public static void EngineSupportsPart5Only()
    {
        var engine = new ToeicPart5Engine();
        if (!engine.SupportsPart(5)) throw new Exception("Should support Part 5");
        if (engine.SupportsPart(4)) throw new Exception("Should not support Part 4");
    }

    public static void MissingPromptThrows()
    {
        var engine = new ToeicPart5Engine();
        var q = CreateValidQuestion() with { Prompt = null! };
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException");
        }
        catch (InvalidOperationException) { }

        q = CreateValidQuestion() with { Prompt = "   " };
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException");
        }
        catch (InvalidOperationException) { }
    }

    public static void InvalidOptionsThrows()
    {
        var engine = new ToeicPart5Engine();
        var q = CreateValidQuestion() with { OptionsJson = "{\"A\":\"a\",\"B\":\"b\",\"C\":\"c\"}" }; // only 3 choices
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException due to not exactly 4 choices");
        }
        catch (InvalidOperationException) { }
    }

    public static void PlayableItemHidesAnswerAndSetsPayload()
    {
        var engine = new ToeicPart5Engine();
        var q = CreateValidQuestion();
        var item = engine.CreatePlayableItem(q);

        var json = JsonSerializer.Serialize(item);
        var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
        if (dict!.ContainsKey("CorrectAnswer") || dict.ContainsKey("Explanation"))
        {
            throw new Exception("Playable item leaked answer keys");
        }
        
        if (item.Choices![0] != "A. review") throw new Exception("Choices not formatted correctly");
        if (item.Prompt != "The manager ___ the report yesterday.") throw new Exception("Prompt missing");
        if (item.Payload is not Part5Payload) throw new Exception("Payload is not Part5Payload");
    }

    public static void ReviewItemIncludesAnswer()
    {
        var engine = new ToeicPart5Engine();
        var q = CreateValidQuestion();
        var item = engine.CreateReviewItem(q);

        if (item.CorrectAnswer != "C") throw new Exception("Missing CorrectAnswer");
        if (item.Explanation != "The sentence is in the past tense.") throw new Exception("Missing Explanation");
        if (item.Prompt != "The manager ___ the report yesterday.") throw new Exception("Prompt missing");
    }
}
