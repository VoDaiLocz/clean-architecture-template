using System;
using System.Text.Json;
using Application.Common.Models;
using Application.Features.PartEngines;
using Domain.Aggregates.Corpus;

public static class ToeicPart4EngineTests
{
    private static PublishedQuestion CreateValidQuestion()
    {
        return new PublishedQuestion(
            QuestionId: "q-4",
            LessonId: "l-4",
            ToeicPart: 4,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: "What is the speaker mainly discussing?",
            OptionsJson: "{\"A\":\"A new product\",\"B\":\"A company policy\",\"C\":\"A schedule change\",\"D\":\"A weather forecast\"}",
            CorrectAnswer: "B",
            Explanation: "B is the correct topic.",
            MediaAssetId: "media-4",
            PassageId: null,
            GroupId: "group-4",
            EvidenceJson: "{}",
            SkillTags: "[]",
            SourceTraceJson: "{}",
            Status: PublishedContentStatus.Published
        );
    }

    public static void EngineSupportsPart4Only()
    {
        var engine = new ToeicPart4Engine();
        if (!engine.SupportsPart(4)) throw new Exception("Should support Part 4");
        if (engine.SupportsPart(3)) throw new Exception("Should not support Part 3");
    }

    public static void MissingGroupIdThrows()
    {
        var engine = new ToeicPart4Engine();
        var q = CreateValidQuestion() with { GroupId = null };
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException");
        }
        catch (InvalidOperationException) { }
    }

    public static void InvalidOptionsThrows()
    {
        var engine = new ToeicPart4Engine();
        var q = CreateValidQuestion() with { OptionsJson = "{\"A\":\"a\",\"B\":\"b\",\"C\":\"c\"}" }; // 3 choices instead of 4
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException due to not exactly 4 choices");
        }
        catch (InvalidOperationException) { }
    }

    public static void PlayableItemHasPromptAndGroupRef()
    {
        var engine = new ToeicPart4Engine();
        var q = CreateValidQuestion();
        var item = engine.CreatePlayableItem(q);

        var json = JsonSerializer.Serialize(item);
        var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
        if (dict!.ContainsKey("CorrectAnswer") || dict.ContainsKey("Explanation"))
        {
            throw new Exception("Playable item leaked answer keys");
        }
        
        if (item.Prompt != "What is the speaker mainly discussing?")
        {
            throw new Exception("Playable item must contain the prompt for Part 4");
        }
        
        if (item.GroupRef != "group-4")
        {
            throw new Exception("Playable item missing GroupRef");
        }
        
        if (item.Choices![0] != "A. A new product") throw new Exception("Choices not formatted correctly");
        if (item.Payload is not Part4Payload) throw new Exception("Payload must be Part4Payload");
    }

    public static void ReviewItemHasPromptAndAnswer()
    {
        var engine = new ToeicPart4Engine();
        var q = CreateValidQuestion();
        var item = engine.CreateReviewItem(q);

        if (item.CorrectAnswer != "B") throw new Exception("Missing CorrectAnswer");
        if (item.Explanation != "B is the correct topic.") throw new Exception("Missing Explanation");
        if (item.Prompt != "What is the speaker mainly discussing?") throw new Exception("Review item missing prompt");
        if (item.GroupRef != "group-4") throw new Exception("Review item missing GroupRef");
        if (item.Payload is not Part4Payload) throw new Exception("Payload must be Part4Payload");
    }
}
