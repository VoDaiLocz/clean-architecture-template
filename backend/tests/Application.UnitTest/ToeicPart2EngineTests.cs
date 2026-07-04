using System;
using System.Text.Json;
using Application.Common.Models;
using Application.Features.PartEngines;
using Domain.Aggregates.Corpus;

public static class ToeicPart2EngineTests
{
    private static PublishedQuestion CreateValidQuestion()
    {
        return new PublishedQuestion(
            QuestionId: "q-2",
            LessonId: "l-2",
            ToeicPart: 2,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: "Where is the meeting room?",
            OptionsJson: "{\"A\":\"Down the hall.\",\"B\":\"At 3 PM.\",\"C\":\"Yes, it is.\"}",
            CorrectAnswer: "A",
            Explanation: "A is a valid location response.",
            MediaAssetId: "media-2",
            PassageId: null,
            GroupId: null,
            EvidenceJson: "{}",
            SkillTags: "[]",
            SourceTraceJson: "{}",
            Status: PublishedContentStatus.Published
        );
    }

    public static void EngineSupportsPart2Only()
    {
        var engine = new ToeicPart2Engine();
        if (!engine.SupportsPart(2)) throw new Exception("Should support Part 2");
        if (engine.SupportsPart(1)) throw new Exception("Should not support Part 1");
    }

    public static void MissingMediaAssetIdThrows()
    {
        var engine = new ToeicPart2Engine();
        var q = CreateValidQuestion() with { MediaAssetId = null };
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException");
        }
        catch (InvalidOperationException) { }
    }

    public static void InvalidOptionsThrows()
    {
        var engine = new ToeicPart2Engine();
        var q = CreateValidQuestion() with { OptionsJson = "{\"A\":\"a\",\"B\":\"b\",\"C\":\"c\",\"D\":\"d\"}" }; // 4 choices instead of 3
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException due to not exactly 3 choices");
        }
        catch (InvalidOperationException) { }
    }

    public static void PlayableItemHidesAnswerAndPrompt()
    {
        var engine = new ToeicPart2Engine();
        var q = CreateValidQuestion();
        var item = engine.CreatePlayableItem(q);

        var json = JsonSerializer.Serialize(item);
        var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
        if (dict!.ContainsKey("CorrectAnswer") || dict.ContainsKey("Explanation"))
        {
            throw new Exception("Playable item leaked answer keys");
        }
        
        if (item.Prompt != null)
        {
            throw new Exception("Playable item leaked the prompt");
        }
        
        if (item.Choices![0] != "A. Down the hall.") throw new Exception("Choices not formatted correctly");
        if (item.MediaRefs![0] != "media-2") throw new Exception("MediaRef missing");
    }

    public static void ReviewItemIncludesAnswerAndPrompt()
    {
        var engine = new ToeicPart2Engine();
        var q = CreateValidQuestion();
        var item = engine.CreateReviewItem(q);

        if (item.CorrectAnswer != "A") throw new Exception("Missing CorrectAnswer");
        if (item.Explanation != "A is a valid location response.") throw new Exception("Missing Explanation");
        if (item.Prompt != "Where is the meeting room?") throw new Exception("Review item missing prompt");
        if (item.MediaRefs![0] != "media-2") throw new Exception("MediaRef missing");
    }
}
