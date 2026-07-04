using System;
using System.Collections.Generic;
using System.Linq;
using Application.Features.Learner.FullTests;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearnerProgress;
using Domain.Aggregates.LearningItems;
using Infrastructure.Data;

namespace Application.UnitTest;

public static class FullTestSessionTests
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
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertPublishedLesson(new PublishedLesson("l1", "u1", 5, "lesson title", "obj", "tags", "{}", PublishedContentStatus.Published));
        SetupMediaAsset(repository);
        
        var requirements = new Dictionary<int, int>
        {
            { 1, 6 }, { 2, 25 }, { 3, 39 }, { 4, 30 }, { 5, 30 }, { 6, 16 }, { 7, 54 }
        };

        repository.UpsertPublishedLesson(new PublishedLesson("l1", "u1", 1, "lesson title", "obj", "tags", "{}", PublishedContentStatus.Published));

        foreach (var req in requirements)
        {
            for (int i = 0; i < req.Value + 5; i++)
            {
                string? mediaId = (req.Key == 1 || req.Key == 2) ? "media1" : null;
                string? groupId = (req.Key == 3 || req.Key == 4) ? "group1" : null;
                string? passageId = (req.Key == 6 || req.Key == 7) ? "passage1" : null;
                repository.UpsertPublishedQuestion(new PublishedQuestion($"q{req.Key}_{i}", "l1", req.Key, PublishedQuestionType.SingleQuestion, "prompt", "{}", "A", "exp", mediaId, passageId, groupId, "{}", "tags", "{}", PublishedContentStatus.Published));
            }
        }

        var handler = new StartFullTestSessionHandler(repository);
        var session = handler.Handle(new StartFullTestCommand("learner1"));

        Assert.Equal("learner1", session.LearnerId, "Should set learner");
        Assert.Equal(FullTestSessionStatus.Started, session.Status, "Should be started");
        Assert.Equal(200, session.AssignedQuestionIds.Count, "Should assign exactly 200 questions");
        
        var loaded = repository.GetFullTestSession(session.SessionId);
        Assert.True(loaded != null, "Should persist session");
        Assert.Equal(200, loaded!.AssignedQuestionIds.Count, "Should persist assigned questions");
    }

    public static void RejectsInsufficientContent()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        repository.UpsertPublishedLesson(new PublishedLesson("l1", "u1", 1, "lesson title", "obj", "tags", "{}", PublishedContentStatus.Published));
        
        // Not adding any questions

        var handler = new StartFullTestSessionHandler(repository);
        bool thrown = false;
        try
        {
            handler.Handle(new StartFullTestCommand("learner1"));
        }
        catch (InvalidOperationException ex) when (ex.Message == "Insufficient content")
        {
            thrown = true;
        }

        Assert.True(thrown, "Should throw when not enough questions");
    }

    public static void EnforcesExpiration()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        var session = new FullToeicTestSession("s1", "learner1", FullTestSessionStatus.Started, DateTimeOffset.UtcNow.AddMinutes(-150), null, DateTimeOffset.UtcNow.AddMinutes(-30), new[] { "q1" }, new Dictionary<string, string>(), null);
        repository.UpsertFullTestSession(session);

        var handler = new SubmitFullTestSessionHandler(repository);
        
        bool thrown = false;
        try
        {
            handler.Handle(new SubmitFullTestCommand("learner1", "s1", new Dictionary<string, string>()));
        }
        catch (InvalidOperationException ex) when (ex.Message == "Expired")
        {
            thrown = true;
        }

        Assert.True(thrown, "Should enforce expiration");
        
        var loaded = repository.GetFullTestSession("s1");
        Assert.Equal(FullTestSessionStatus.Expired, loaded!.Status, "Should mark as expired");
    }

    public static void PersistsAnswersAndFinalSubmit()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        var session = new FullToeicTestSession("s1", "learner1", FullTestSessionStatus.Started, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow.AddMinutes(120), new[] { "q1", "q2" }, new Dictionary<string, string>(), null);
        repository.UpsertFullTestSession(session);

        var handler = new SubmitFullTestSessionHandler(repository);
        var answers = new Dictionary<string, string> { { "q1", "A" }, { "q2", "B" } };
        
        var submitted = handler.Handle(new SubmitFullTestCommand("learner1", "s1", answers));
        
        Assert.Equal(FullTestSessionStatus.Submitted, submitted.Status, "Should be submitted");
        Assert.True(submitted.SubmittedAtUtc != null, "Should set submitted time");
        Assert.Equal("A", submitted.Answers["q1"], "Should save answer");
        
        var loaded = repository.GetFullTestSession("s1");
        Assert.Equal(FullTestSessionStatus.Submitted, loaded!.Status, "Should persist submitted status");
        Assert.Equal("A", loaded.Answers["q1"], "Should persist answers");
    }
}
