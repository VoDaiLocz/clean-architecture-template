using System;
using System.Text.Json;
using System.Collections.Generic;
using Application.Common.Models;
using Application.Features.PartEngines;
using Domain.Aggregates.Corpus;

public static class ToeicPart6EngineTests
{
    private static PublishedQuestion CreateValidQuestion()
    {
        return new PublishedQuestion(
            QuestionId: "q-1",
            LessonId: "l-1",
            ToeicPart: 6,
            QuestionType: PublishedQuestionType.SingleQuestion, // Or TextCompletion depending on domain
            Prompt: "Please review the attached ___.",
            OptionsJson: "{\"A\":\"document\",\"B\":\"documents\",\"C\":\"documenting\",\"D\":\"documented\"}",
            CorrectAnswer: "A",
            Explanation: "The noun form is required here.",
            MediaAssetId: null,
            PassageId: "p-1",
            GroupId: null,
            EvidenceJson: "{}",
            SkillTags: "[]",
            SourceTraceJson: "{}",
            Status: PublishedContentStatus.Published
        );
    }

    public static void EngineSupportsPart6Only()
    {
        var engine = new ToeicPart6Engine();
        if (!engine.SupportsPart(6)) throw new Exception("Should support Part 6");
        if (engine.SupportsPart(5)) throw new Exception("Should not support Part 5");
    }

    public static void MissingPassageIdThrows()
    {
        var engine = new ToeicPart6Engine();
        
        var q = CreateValidQuestion() with { PassageId = null };
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException for null PassageId");
        }
        catch (InvalidOperationException) { }

        q = CreateValidQuestion() with { PassageId = "   " };
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException for empty PassageId");
        }
        catch (InvalidOperationException) { }
    }

    public static void InvalidOptionsThrows()
    {
        var engine = new ToeicPart6Engine();
        var q = CreateValidQuestion() with { OptionsJson = "{\"A\":\"a\",\"B\":\"b\",\"C\":\"c\"}" }; // only 3 choices
        try
        {
            engine.CreatePlayableItem(q);
            throw new Exception("Expected InvalidOperationException due to not exactly 4 choices");
        }
        catch (InvalidOperationException) { }
    }

    public static void PlayableItemSetsPayloadAndPassageRefs()
    {
        var engine = new ToeicPart6Engine();
        var q = CreateValidQuestion();
        var item = engine.CreatePlayableItem(q);

        if (item.Choices == null || item.Choices.Length != 4) throw new Exception("Choices not formatted correctly");
        if (item.Choices[0] != "A. document") throw new Exception("Choices not formatted correctly");
        if (item.Prompt != "Please review the attached ___.") throw new Exception("Prompt missing");
        if (item.Payload is not Part6Payload) throw new Exception("Payload is not Part6Payload");
        if (item.PassageRefs == null || item.PassageRefs.Count != 1 || item.PassageRefs[0] != "p-1") throw new Exception("PassageRefs must contain the PassageId");
    }

    public static void ReviewItemIncludesAnswerAndPassageRefs()
    {
        var engine = new ToeicPart6Engine();
        var q = CreateValidQuestion();
        var item = engine.CreateReviewItem(q);

        if (item.CorrectAnswer != "A") throw new Exception("Missing CorrectAnswer");
        if (item.Explanation != "The noun form is required here.") throw new Exception("Missing Explanation");
        if (item.Prompt != "Please review the attached ___.") throw new Exception("Prompt missing");
        if (item.PassageRefs == null || item.PassageRefs.Count != 1 || item.PassageRefs[0] != "p-1") throw new Exception("PassageRefs must contain the PassageId");
    }
}
