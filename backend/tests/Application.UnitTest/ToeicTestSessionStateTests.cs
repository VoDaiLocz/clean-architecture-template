using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Features.Learner.TestSessions;
using Domain.Aggregates.LearnerProgress;
using Infrastructure.Data;

namespace Application.UnitTest;

public static class ToeicTestSessionStateTests
{
    public static async Task TestGetPracticeTestSessionHandler_ReturnsUnifiedState()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var sessionId = "mini-sess-1";
        var learnerId = "learner-1";
        var startedAt = DateTimeOffset.UtcNow;
        var expiredAt = startedAt.AddMinutes(30);

        repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPath(new LearningPath("path-1", learnerId, LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPathUnit(new LearningPathUnit("unit-1", "path-1", "key1", 5, "tag", 1, LearningPathUnitStatus.Unlocked, null, null));

        var miniSession = new MiniTestSession(
            sessionId,
            learnerId,
            "unit-1",
            MiniTestSessionStatus.Started,
            startedAt,
            null,
            expiredAt,
            new List<string> { "q1", "q2" },
            new Dictionary<string, string>(),
            null
        );

        repository.UpsertMiniTestSession(miniSession);

        var handler = new GetPracticeTestSessionHandler(repository);
        var result = await handler.Handle(new GetPracticeTestSessionQuery(learnerId, sessionId), CancellationToken.None);

        if (result.TestType != PracticeTestType.Mini) throw new Exception("Wrong type");
        if (result.LearnerId != learnerId) throw new Exception("Wrong learner");
        if (result.Status != PracticeTestStatus.Started) throw new Exception("Wrong status");
        if (result.AssignedQuestionIds.Count != 2) throw new Exception("Wrong assigned questions");
    }

    public static async Task TestGetPracticeTestSessionHandler_RejectsUnauthorized()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var sessionId = "mini-sess-1";
        var learner1 = "learner-1";
        var learner2 = "learner-2";

        repository.UpsertLearnerProfile(new LearnerProfile(learner1, "Name1", "email1@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearnerProfile(new LearnerProfile(learner2, "Name2", "email2@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPath(new LearningPath("path-1", learner1, LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPathUnit(new LearningPathUnit("unit-1", "path-1", "key1", 5, "tag", 1, LearningPathUnitStatus.Unlocked, null, null));

        var miniSession = new MiniTestSession(
            sessionId,
            learner1,
            "unit-1",
            MiniTestSessionStatus.Started,
            DateTimeOffset.UtcNow,
            null,
            DateTimeOffset.UtcNow.AddMinutes(30),
            new List<string>(),
            new Dictionary<string, string>(),
            null
        );

        repository.UpsertMiniTestSession(miniSession);

        var handler = new GetPracticeTestSessionHandler(repository);
        try
        {
            await handler.Handle(new GetPracticeTestSessionQuery("learner-2", sessionId), CancellationToken.None);
            throw new Exception("Should have thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected
        }
    }

    public static async Task TestCheckpointPracticeTestSessionHandler_PersistsPartialAnswers()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var sessionId = "mini-sess-1";
        var learnerId = "learner-1";

        repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPath(new LearningPath("path-1", learnerId, LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPathUnit(new LearningPathUnit("unit-1", "path-1", "key1", 5, "tag", 1, LearningPathUnitStatus.Unlocked, null, null));

        var miniSession = new MiniTestSession(
            sessionId,
            learnerId,
            "unit-1",
            MiniTestSessionStatus.Started,
            DateTimeOffset.UtcNow,
            null,
            DateTimeOffset.UtcNow.AddMinutes(30),
            new List<string> { "q1", "q2" },
            new Dictionary<string, string>(),
            null
        );

        repository.UpsertMiniTestSession(miniSession);

        var handler = new CheckpointPracticeTestSessionHandler(repository);
        var answers = new Dictionary<string, string> { { "q1", "A" } };
        
        await handler.Handle(new CheckpointPracticeTestSessionCommand(learnerId, sessionId, answers), CancellationToken.None);

        var updated = repository.GetMiniTestSession(sessionId);
        if (updated!.Answers["q1"] != "A") throw new Exception("Answer not saved");
    }

    public static async Task TestCheckpointPracticeTestSessionHandler_RejectsExpired()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var sessionId = "mini-sess-1";
        var learnerId = "learner-1";

        repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPath(new LearningPath("path-1", learnerId, LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPathUnit(new LearningPathUnit("unit-1", "path-1", "key1", 5, "tag", 1, LearningPathUnitStatus.Unlocked, null, null));

        var miniSession = new MiniTestSession(
            sessionId,
            learnerId,
            "unit-1",
            MiniTestSessionStatus.Started,
            DateTimeOffset.UtcNow.AddMinutes(-60),
            null,
            DateTimeOffset.UtcNow.AddMinutes(-30),
            new List<string>(),
            new Dictionary<string, string>(),
            null
        );

        repository.UpsertMiniTestSession(miniSession);

        var handler = new CheckpointPracticeTestSessionHandler(repository);
        try
        {
            await handler.Handle(new CheckpointPracticeTestSessionCommand(learnerId, sessionId, new Dictionary<string, string>()), CancellationToken.None);
            throw new Exception("Should have thrown");
        }
        catch (InvalidOperationException ex) when (ex.Message == "Expired")
        {
            // Expected
        }

        var updated = repository.GetMiniTestSession(sessionId);
        if (updated!.Status != MiniTestSessionStatus.Expired) throw new Exception("Status not updated to expired");
    }
}
