using System;
using System.Text.Json;
using Application.Common.Models;
using Application.Features.PartEngines;
using Domain.Aggregates.Corpus;

public static class ToeicPart3EngineTests
{
    private static PublishedQuestion CreateValidQuestion()
    {
        return new PublishedQuestion(
            QuestionId: "q-3",
            LessonId: "l-3",
            ToeicPart: 3,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: "What are the speakers discussing?",
            OptionsJson: "{\"A\":\"A contract\",\"B\":\"A new employee\",\"C\":\"A budget report\",\"D\":\"A project deadline\"}",
            CorrectAnswer: "B",
            Explanation: "B is the correct topic.",
            MediaAssetId: "media-3",
            PassageId: null,
            GroupId: "group-3",
            EvidenceJson: "{}",
            SkillTags: "[]",
            SourceTraceJson: "{}",
            Status: PublishedContentStatus.Published
        );
    }

    public static void EngineSupportsPart3Only()
    {
        var engine = new ToeicPart3Engine();
        if (!engine.SupportsPart(3)) throw new Exception("Should support Part 3");
        if (engine.SupportsPart(2)) throw new Exception("Should not support Part 2");
    }

    public static void MissingGroupIdThrows()
    {
        var engine = new ToeicPart3Engine();
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
        var engine = new ToeicPart3Engine();
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
        var engine = new ToeicPart3Engine();
        var q = CreateValidQuestion();
        var item = engine.CreatePlayableItem(q);

        var json = JsonSerializer.Serialize(item);
        var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
        if (dict!.ContainsKey("CorrectAnswer") || dict.ContainsKey("Explanation"))
        {
            throw new Exception("Playable item leaked answer keys");
        }
        
        if (item.Prompt != "What are the speakers discussing?")
        {
            throw new Exception("Playable item must contain the prompt for Part 3");
        }
        
        if (item.GroupRef != "group-3")
        {
            throw new Exception("Playable item missing GroupRef");
        }
        
        if (item.Choices![0] != "A. A contract") throw new Exception("Choices not formatted correctly");
    }

    public static void ReviewItemHasPromptAndAnswer()
    {
        var engine = new ToeicPart3Engine();
        var q = CreateValidQuestion();
        var item = engine.CreateReviewItem(q);

        if (item.CorrectAnswer != "B") throw new Exception("Missing CorrectAnswer");
        if (item.Explanation != "B is the correct topic.") throw new Exception("Missing Explanation");
        if (item.Prompt != "What are the speakers discussing?") throw new Exception("Review item missing prompt");
        if (item.GroupRef != "group-3") throw new Exception("Review item missing GroupRef");
    }
}
