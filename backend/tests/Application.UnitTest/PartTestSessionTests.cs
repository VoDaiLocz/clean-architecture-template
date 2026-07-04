using System;
using System.Collections.Generic;
using System.Linq;
using Application.Common.Interfaces.Repositories;
using Application.Features.Learner.PartTests;
using Domain.Aggregates.LearnerProgress;
using Domain.Aggregates.LearningItems;
using Domain.Aggregates.Corpus;
using Infrastructure.Data;

namespace Application.UnitTest;

public static class PartTestSessionTests
{
    private static void EnsureLearner(IKnowledgeRepository repository, string learnerId)
    {
        repository.UpsertLearnerProfile(new LearnerProfile(
            LearnerId: learnerId,
            DisplayName: "Test Learner",
            Email: "test@test.com",
            TargetScore: 500,
            CurrentEstimatedScore: 0,
            DailyStudyMinutes: 30,
            TimeZoneId: "UTC",
            Status: LearnerProfileStatus.Active,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            UpdatedAtUtc: DateTimeOffset.UtcNow
        ));
    }

    public static void StartsSessionWithCorrectAssignedQuestions()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        EnsureLearner(repository, "learner-1");
        
        repository.UpsertPublishedLesson(new PublishedLesson("l1", "u1", 5, "lesson title", "obj", "tags", "{}", PublishedContentStatus.Published));
        for (int i = 0; i < 6; i++)
        {
            repository.UpsertPublishedQuestion(new PublishedQuestion($"q{i}", "l1", 5, PublishedQuestionType.SingleQuestion, "prompt", "{}", "A", "exp", null, null, null, "{}", "tags", "{}", PublishedContentStatus.Published));
        }

        var handler = new StartPartTestSessionHandler(repository);
        var session = handler.Handle(new StartPartTestCommand("learner-1", 5));

        if (session.Status != PartTestSessionStatus.Started) throw new Exception("Session should be started");
        if (session.AssignedQuestionIds.Count != 6) throw new Exception("Expected 6 assigned questions");
    }

    public static void RejectsInsufficientContent()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        EnsureLearner(repository, "learner-1");
        
        var handler = new StartPartTestSessionHandler(repository);
        bool thrown = false;
        try
        {
            handler.Handle(new StartPartTestCommand("learner-1", 5));
        }
        catch (InvalidOperationException)
        {
            thrown = true;
        }

        if (!thrown) throw new Exception("Expected InvalidOperationException");
    }

    public static void EnforcesExpiration()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        EnsureLearner(repository, "learner-1");
        
        var session = new PartTestSession(
            SessionId: "session-1",
            LearnerId: "learner-1",
            ToeicPart: 5,
            Status: PartTestSessionStatus.Started,
            StartedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-60),
            SubmittedAtUtc: null,
            ExpiredAtUtc: DateTimeOffset.UtcNow.AddMinutes(-15),
            AssignedQuestionIds: new List<string> { "q1" },
            Answers: new Dictionary<string, string>(),
            ResultId: null
        );
        repository.UpsertPartTestSession(session);

        var handler = new SubmitPartTestSessionHandler(repository);
        bool thrown = false;
        try
        {
            handler.Handle(new SubmitPartTestCommand("learner-1", "session-1", new Dictionary<string, string>()));
        }
        catch (InvalidOperationException ex) when (ex.Message == "Expired")
        {
            thrown = true;
        }

        if (!thrown) throw new Exception("Expected Expired exception");
        
        var updated = repository.GetPartTestSession("session-1");
        if (updated?.Status != PartTestSessionStatus.Expired) throw new Exception("Session status should be updated to Expired");
    }

    public static void PersistsAnswersAndFinalSubmit()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        EnsureLearner(repository, "learner-1");
        
        var session = new PartTestSession(
            SessionId: "session-1",
            LearnerId: "learner-1",
            ToeicPart: 5,
            Status: PartTestSessionStatus.Started,
            StartedAtUtc: DateTimeOffset.UtcNow,
            SubmittedAtUtc: null,
            ExpiredAtUtc: DateTimeOffset.UtcNow.AddMinutes(45),
            AssignedQuestionIds: new List<string> { "q1", "q2" },
            Answers: new Dictionary<string, string>(),
            ResultId: null
        );
        repository.UpsertPartTestSession(session);

        var handler = new SubmitPartTestSessionHandler(repository);
        var answers = new Dictionary<string, string> { { "q1", "A" }, { "q2", "B" } };
        var submitted = handler.Handle(new SubmitPartTestCommand("learner-1", "session-1", answers));

        if (submitted.Status != PartTestSessionStatus.Submitted) throw new Exception("Session should be Submitted");
        if (submitted.Answers.Count != 2) throw new Exception("Expected 2 answers");
        
        var dbSession = repository.GetPartTestSession("session-1");
        if (dbSession?.Status != PartTestSessionStatus.Submitted) throw new Exception("DB Session should be Submitted");
        if (dbSession.Answers["q1"] != "A") throw new Exception("DB answer mismatch");
    }
}
