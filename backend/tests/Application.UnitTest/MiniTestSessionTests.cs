using System;
using System.Collections.Generic;
using System.Linq;
using Application.Features.Learner.MiniTests;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearnerProgress;
using Domain.Aggregates.LearningItems;
using Infrastructure.Data;

namespace Application.UnitTest;

public static class MiniTestSessionTests
{
    public static void StartsSessionWithCorrectAssignedQuestions()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        repository.UpsertPublishedLesson(new PublishedLesson("l1", "u1", 5, "lesson title", "obj", "tags", "{}", PublishedContentStatus.Published));
        
        for (int i = 0; i < 15; i++)
        {
            repository.UpsertPublishedQuestion(new PublishedQuestion($"q{i}", "l1", 5, PublishedQuestionType.SingleQuestion, "prompt", "{}", "A", "exp", null, null, null, "{}", "tags", "{}", PublishedContentStatus.Published));
        }

        var handler = new StartMiniTestSessionHandler(repository);
        var session = handler.Handle(new StartMiniTestCommand("learner1", "unit1"));

        Assert.Equal("learner1", session.LearnerId, "Should set learner");
        Assert.Equal("unit1", session.UnitId, "Should set unit");
        Assert.Equal(MiniTestSessionStatus.Started, session.Status, "Should be started");
        Assert.Equal(10, session.AssignedQuestionIds.Count, "Should assign 10 questions");
        
        var loaded = repository.GetMiniTestSession(session.SessionId);
        Assert.True(loaded != null, "Should persist session");
        Assert.Equal(10, loaded!.AssignedQuestionIds.Count, "Should persist assigned questions");
    }

    public static void RejectsInsufficientContent()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        repository.UpsertPublishedLesson(new PublishedLesson("l1", "u1", 5, "lesson title", "obj", "tags", "{}", PublishedContentStatus.Published));
        
        for (int i = 0; i < 5; i++)
        {
            repository.UpsertPublishedQuestion(new PublishedQuestion($"q{i}", "l1", 5, PublishedQuestionType.SingleQuestion, "prompt", "{}", "A", "exp", null, null, null, "{}", "tags", "{}", PublishedContentStatus.Published));
        }

        var handler = new StartMiniTestSessionHandler(repository);
        bool thrown = false;
        try
        {
            handler.Handle(new StartMiniTestCommand("learner1", "unit1"));
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
        
        var session = new MiniTestSession("s1", "learner1", "u1", MiniTestSessionStatus.Started, DateTimeOffset.UtcNow.AddMinutes(-40), null, DateTimeOffset.UtcNow.AddMinutes(-10), new[] { "q1" }, new Dictionary<string, string>(), null);
        repository.UpsertMiniTestSession(session);

        var handler = new SubmitMiniTestSessionHandler(repository);
        
        bool thrown = false;
        try
        {
            handler.Handle(new SubmitMiniTestCommand("learner1", "s1", new Dictionary<string, string>()));
        }
        catch (InvalidOperationException ex) when (ex.Message == "Expired")
        {
            thrown = true;
        }

        Assert.True(thrown, "Should enforce expiration");
        
        var loaded = repository.GetMiniTestSession("s1");
        Assert.Equal(MiniTestSessionStatus.Expired, loaded!.Status, "Should mark as expired");
    }

    public static void PersistsAnswersAndFinalSubmit()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        repository.UpsertLearnerProfile(new LearnerProfile("learner1", "Display Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        
        var session = new MiniTestSession("s1", "learner1", "u1", MiniTestSessionStatus.Started, DateTimeOffset.UtcNow, null, DateTimeOffset.UtcNow.AddMinutes(30), new[] { "q1", "q2" }, new Dictionary<string, string>(), null);
        repository.UpsertMiniTestSession(session);

        var handler = new SubmitMiniTestSessionHandler(repository);
        var answers = new Dictionary<string, string> { { "q1", "A" }, { "q2", "B" } };
        
        var submitted = handler.Handle(new SubmitMiniTestCommand("learner1", "s1", answers));
        
        Assert.Equal(MiniTestSessionStatus.Submitted, submitted.Status, "Should be submitted");
        Assert.True(submitted.SubmittedAtUtc != null, "Should set submitted time");
        Assert.Equal("A", submitted.Answers["q1"], "Should save answer");
        
        var loaded = repository.GetMiniTestSession("s1");
        Assert.Equal(MiniTestSessionStatus.Submitted, loaded!.Status, "Should persist submitted status");
        Assert.Equal("A", loaded.Answers["q1"], "Should persist answers");
    }
}
