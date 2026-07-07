using System;
using System.Text.Json;
using Application.Common.Models;
using Application.Features.PartEngines;
using Domain.Aggregates.Corpus;

public static class ToeicPart1EngineTests
{
    private static PublishedQuestion CreateValidQuestion()
    {
        return new PublishedQuestion(
            QuestionId: "q-1",
            LessonId: "l-1",
            ToeicPart: 1,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: "Look at the picture.",
            OptionsJson: "{\"A\":\"He is walking.\",\"B\":\"He is running.\",\"C\":\"He is jumping.\",\"D\":\"He is sleeping.\"}",
            CorrectAnswer: "A",
            Explanation: "He is clearly walking.",
            MediaAssetId: "image-1",
            PassageId: null,
            GroupId: null,
            EvidenceJson: "{\"audioAssetId\":\"audio-1\"}",
            SkillTags: "[]",
            SourceTraceJson: "{}",
            Status: PublishedContentStatus.Published
        );
    }

    public static void EngineSupportsPart1Only()
    {
        var engine = new ToeicPart1Engine();
        if (!engine.SupportsPart(1)) throw new Exception("Should support Part 1");
        if (engine.SupportsPart(2)) throw new Exception("Should not support Part 2");
    }

    public static void MissingMediaAssetIdThrows()
    {
        var engine = new ToeicPart1Engine();
        var q = CreateValidQuestion() with { MediaAssetId = null };
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException");
        }
        catch (InvalidOperationException) { }
    }

    public static void MissingAudioAssetIdThrows()
    {
        var engine = new ToeicPart1Engine();
        var q = CreateValidQuestion() with { EvidenceJson = "{}", SourceTraceJson = "{}" };
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException");
        }
        catch (InvalidOperationException) { }
    }

    public static void InvalidOptionsThrows()
    {
        var engine = new ToeicPart1Engine();
        var q = CreateValidQuestion() with { OptionsJson = "{\"A\":\"a\",\"B\":\"b\",\"C\":\"c\"}" }; // only 3 choices
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException due to not exactly 4 choices");
        }
        catch (InvalidOperationException) { }
    }

    public static void PlayableItemHidesAnswer()
    {
        var engine = new ToeicPart1Engine();
        var q = CreateValidQuestion();
        var item = engine.CreatePlayableItem(q);

        var json = JsonSerializer.Serialize(item);
        var dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(json);
        if (dict!.ContainsKey("CorrectAnswer") || dict.ContainsKey("Explanation"))
        {
            throw new Exception("Playable item leaked answer keys");
        }
        
        if (item.Choices![0] != "A. He is walking.") throw new Exception("Choices not formatted correctly");
        if (item.MediaRefs![0] != "image-1") throw new Exception("Image MediaRef missing");
        if (item.MediaRefs![1] != "audio-1") throw new Exception("Audio MediaRef missing");
        var payload = (Part1Payload)item.Payload!;
        if (payload.ImageAssetId != "image-1") throw new Exception("Payload image asset missing");
        if (payload.AudioAssetId != "audio-1") throw new Exception("Payload audio asset missing");
    }

    public static void ReviewItemIncludesAnswer()
    {
        var engine = new ToeicPart1Engine();
        var q = CreateValidQuestion();
        var item = engine.CreateReviewItem(q);

        if (item.CorrectAnswer != "A") throw new Exception("Missing CorrectAnswer");
        if (item.Explanation != "He is clearly walking.") throw new Exception("Missing Explanation");
        if (item.MediaRefs![0] != "image-1") throw new Exception("Image MediaRef missing");
        if (item.MediaRefs![1] != "audio-1") throw new Exception("Audio MediaRef missing");
    }
}
