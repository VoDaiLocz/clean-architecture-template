using System;
using System.Collections.Generic;
using System.Threading;
using Application.Features.Learner.ReadingTests;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearnerProgress;
using Domain.Aggregates.LearningItems;
using Infrastructure.Data;

namespace Application.UnitTest;

public static class ReadingTestSessionTests
{
    private static void SetupMediaAsset(SqliteKnowledgeRepository repository)
    {
        repository.UpsertSourceManifestEntry(new SourceManifestEntry(
            "source1", 1, "title", "url", SourceProvider.GoogleDrive, SourceType.DriveFolder, MaterialClass.TestBook, SourceAccessStatus.Accessible,
            new SourceEvidenceFlags(true, true, true, true, true), "notes"
        ));

        repository.UpsertSourceContainer(new SourceContainer(
            "container1", "source1", SourceProvider.GoogleDrive, "ext1", "title", SourceAccessStatus.Accessible, DateTimeOffset.UtcNow
        ));

        repository.UpsertSourceAsset(new SourceAsset(
            "media1", "container1", "source1", "file.mp3", "audio/mpeg", ".mp3", 1024, SourceAssetRole.Audio, "url", "key", "hash"
        ));
    }

    public static void StartsSessionWithCorrectAssignedQuestions()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertPublishedLesson(new PublishedLesson("lesson1", "u1", 5, "lesson title", "obj", "tags", "{}", PublishedContentStatus.Published));
        SetupMediaAsset(repository);

        // Add 5 questions to parts 5,6,7
        for (int p = 5; p <= 7; p++)
        {
            for (int i = 0; i < 5; i++)
            {
                var qId = $"q_p{p}_{i}";
                string? passageId = p == 6 || p == 7 ? "passage1" : null;

                repository.UpsertPublishedQuestion(new PublishedQuestion(
                    QuestionId: qId,
                    LessonId: "lesson1",
                    ToeicPart: p,
                    QuestionType: PublishedQuestionType.SingleQuestion,
                    Prompt: "prompt",
                    OptionsJson: "{}",
                    CorrectAnswer: "A",
                    Explanation: "exp",
                    MediaAssetId: null,
                    PassageId: passageId,
                    GroupId: null,
                    EvidenceJson: "{}",
                    SkillTags: "",
                    SourceTraceJson: "{}",
                    Status: PublishedContentStatus.Published
                ));
            }
        }

        var handler = new StartReadingTestSessionHandler(repository);
        var command = new StartReadingTestCommand("learner1");

        var result = handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();

        if (string.IsNullOrEmpty(result.SessionId)) throw new Exception("SessionId should not be null");
        if (result.AssignedQuestionIds.Count != 15) throw new Exception("Should have 15 questions (5 from 3 parts)");
        
        var diff = result.ExpiredAtUtc - DateTimeOffset.UtcNow.AddMinutes(75);
        if (Math.Abs(diff.TotalSeconds) > 5) throw new Exception("ExpiredAtUtc should be ~75 mins from now");

        var session = repository.GetReadingTestSession(result.SessionId);
        if (session == null) throw new Exception("Session not saved");
        if (session.Status != ReadingTestSessionStatus.Started) throw new Exception("Status should be Started");
        if (session.LearnerId != "learner1") throw new Exception("LearnerId mismatch");
        if (session.AssignedQuestionIds.Count != 15) throw new Exception("Assigned questions not saved");
    }

    public static void RejectsInsufficientContent()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var handler = new StartReadingTestSessionHandler(repository);
        var command = new StartReadingTestCommand("learner1");

        try
        {
            handler.Handle(command, CancellationToken.None).GetAwaiter().GetResult();
            throw new Exception("Expected InvalidOperationException");
        }
        catch (InvalidOperationException ex) when (ex.Message == "Insufficient content")
        {
            // Expected
        }
    }

    public static void EnforcesExpiration()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var session = new ReadingTestSession
        {
            SessionId = "s1",
            LearnerId = "learner1",
            Status = ReadingTestSessionStatus.Started,
            StartedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-90),
            ExpiredAtUtc = DateTimeOffset.UtcNow.AddMinutes(-15),
            AssignedQuestionIds = new List<string> { "q1" },
            Answers = new Dictionary<string, string>()
        };
        repository.UpsertReadingTestSession(session);

        var submitHandler = new SubmitReadingTestSessionHandler(repository);
        var submitCmd = new SubmitReadingTestCommand("learner1", "s1", new Dictionary<string, string>());

        try
        {
            submitHandler.Handle(submitCmd, CancellationToken.None).GetAwaiter().GetResult();
            throw new Exception("Expected Expiration exception");
        }
        catch (InvalidOperationException ex) when (ex.Message == "Expired")
        {
            // Expected
        }

        var loaded = repository.GetReadingTestSession("s1");
        if (loaded!.Status != ReadingTestSessionStatus.Expired) throw new Exception("Status should be Expired");
    }

    public static void PersistsAnswersAndFinalSubmit()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var session = new ReadingTestSession
        {
            SessionId = "s1",
            LearnerId = "learner1",
            Status = ReadingTestSessionStatus.Started,
            StartedAtUtc = DateTimeOffset.UtcNow,
            ExpiredAtUtc = DateTimeOffset.UtcNow.AddMinutes(75),
            AssignedQuestionIds = new List<string> { "q1", "q2" },
            Answers = new Dictionary<string, string>()
        };
        repository.UpsertReadingTestSession(session);

        var submitHandler = new SubmitReadingTestSessionHandler(repository);
        var answers = new Dictionary<string, string> { { "q1", "A" }, { "q2", "B" } };
        var submitCmd = new SubmitReadingTestCommand("learner1", "s1", answers);

        var submitResult = submitHandler.Handle(submitCmd, CancellationToken.None).GetAwaiter().GetResult();

        if (submitResult.Status != ReadingTestSessionStatus.Submitted) throw new Exception("Should be submitted");

        var loaded = repository.GetReadingTestSession("s1");
        if (loaded!.Status != ReadingTestSessionStatus.Submitted) throw new Exception("Status should be Submitted");
        if (loaded.Answers.Count != 2) throw new Exception("Answers not saved");
        if (loaded.Answers["q1"] != "A") throw new Exception("Wrong answer");
    }
}
