using Application.Features.Learner.MiniTests;
using Application.Features.Learner.TestSessions;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearnerProgress;
using Infrastructure.Data;
using System.Threading;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.UnitTest;

public static class CalculateToeicScoreBreakdownTests
{
    public static async Task TestCalculateToeicScoreBreakdown_CalculatesCorrectly()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = "learner-1";
        repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "Name", "email", 500, 500, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var source = Domain.Aggregates.Corpus.SourceManifestClassifier.Classify(8, "SPARTA TOEIC PDF", "url", false, true, false, false, true, false);
        repository.UpsertSourceManifestEntry(source);
        repository.UpsertSourceContainer(new SourceContainer("cont", source.SourceId, source.Provider, "url", "name", source.AccessStatus, DateTimeOffset.UtcNow));
        repository.UpsertSourceAsset(new SourceAsset("asset", "cont", source.SourceId, "file", "mime", ".mp3", 100, SourceAssetRole.Audio, "url", "key", "chk"));

        var lesson = new PublishedLesson("cont", "unit-1", 1, "Lesson Title", "Objective", "[]", "[]", PublishedContentStatus.Published);
        repository.UpsertPublishedLesson(lesson);

        var q1 = new PublishedQuestion("q1", "cont", 1, PublishedQuestionType.SingleQuestion, "text", "{}", "A", "Exp", "asset", null, null, "[]", "[\"Grammar\"]", "[]", PublishedContentStatus.Published);
        var q2 = new PublishedQuestion("q2", "cont", 2, PublishedQuestionType.SingleQuestion, "text", "{}", "B", "Exp", "asset", null, null, "[]", "[\"Vocab\"]", "[]", PublishedContentStatus.Published);
        var q3 = new PublishedQuestion("q3", "cont", 5, PublishedQuestionType.SingleQuestion, "text", "{}", "C", "Exp", null, null, null, "[]", "[\"Grammar\"]", "[]", PublishedContentStatus.Published);

        repository.UpsertPublishedQuestion(q1);
        repository.UpsertPublishedQuestion(q2);
        repository.UpsertPublishedQuestion(q3);

        var session = new MiniTestSession("sess-1", learnerId, "unit-1", MiniTestSessionStatus.Started, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow.AddMinutes(10), new[] { "q1", "q2", "q3" }, new Dictionary<string, string>(), null);
        repository.UpsertMiniTestSession(session);

        var answers = new Dictionary<string, string>
        {
            { "q1", "A" }, // Correct (Part 1)
            { "q2", "C" }, // Incorrect (Part 2), expected B
            { "q3", "C" }  // Correct (Part 5)
        };

        await new CheckpointPracticeTestSessionHandler(repository).Handle(new CheckpointPracticeTestSessionCommand(learnerId, "sess-1", answers), CancellationToken.None);
        new SubmitMiniTestSessionHandler(repository).Handle(new SubmitMiniTestCommand(learnerId, "sess-1", answers));

        var handler = new CalculateToeicScoreBreakdownHandler(repository);
        var breakdown = await handler.Handle(new CalculateToeicScoreBreakdownQuery(learnerId, "sess-1"));

        if (breakdown.TotalQuestions != 3) throw new Exception("Expected 3 questions");
        if (breakdown.TotalCorrect != 2) throw new Exception($"Expected 2 correct, got {breakdown.TotalCorrect}");
        if (breakdown.EstimatedListeningScore != 5) throw new Exception("Expected 5 listening score");
        if (breakdown.EstimatedReadingScore != 5) throw new Exception("Expected 5 reading score");

        if (breakdown.PartBreakdown[1].Total != 1 || breakdown.PartBreakdown[1].Correct != 1) throw new Exception("Part 1 breakdown incorrect");
        if (breakdown.PartBreakdown[2].Total != 1 || breakdown.PartBreakdown[2].Correct != 0) throw new Exception("Part 2 breakdown incorrect");
        if (breakdown.PartBreakdown[5].Total != 1 || breakdown.PartBreakdown[5].Correct != 1) throw new Exception("Part 5 breakdown incorrect");

        if (breakdown.SkillTagWeaknesses.Count != 1 || !breakdown.SkillTagWeaknesses.ContainsKey("Vocab")) throw new Exception("Skill tag weaknesses incorrect");
        if (breakdown.SkillTagWeaknesses["Vocab"] != 1) throw new Exception("Vocab weakness count incorrect");
    }

    public static async Task TestCalculateToeicScoreBreakdown_RejectsNotSubmitted()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = "learner-1";
        repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "Name", "email", 500, 500, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var session = new MiniTestSession("sess-1", learnerId, "unit-1", MiniTestSessionStatus.Started, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow.AddMinutes(10), new string[] { }, new Dictionary<string, string>(), null);
        repository.UpsertMiniTestSession(session);

        var handler = new CalculateToeicScoreBreakdownHandler(repository);
        try
        {
            await handler.Handle(new CalculateToeicScoreBreakdownQuery(learnerId, "sess-1"));
            throw new Exception("Expected InvalidOperationException");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }
}
