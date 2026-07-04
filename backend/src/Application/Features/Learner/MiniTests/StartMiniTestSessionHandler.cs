using System;
using System.Collections.Generic;
using System.Linq;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.MiniTests;

public record StartMiniTestCommand(string LearnerId, string UnitId);

public class StartMiniTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public StartMiniTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public MiniTestSession Handle(StartMiniTestCommand command)
    {
        var questions = _repository.GetPublishedQuestions(5);
        if (questions.Count < 10)
        {
            throw new InvalidOperationException("Insufficient content");
        }

        var selectedIds = questions.Take(10).Select(q => q.QuestionId).ToList();

        var session = new MiniTestSession(
            SessionId: Guid.NewGuid().ToString(),
            LearnerId: command.LearnerId,
            UnitId: command.UnitId,
            Status: MiniTestSessionStatus.Started,
            StartedAtUtc: DateTimeOffset.UtcNow,
            SubmittedAtUtc: null,
            ExpiredAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            AssignedQuestionIds: selectedIds,
            Answers: new Dictionary<string, string>(),
            ResultId: null
        );

        _repository.UpsertMiniTestSession(session);
        return session;
    }
}
