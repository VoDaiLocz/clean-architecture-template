using System;
using System.Collections.Generic;
using System.Linq;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.PartTests;

public record StartPartTestCommand(string LearnerId, int ToeicPart);

public class StartPartTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public StartPartTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public PartTestSession Handle(StartPartTestCommand command)
    {
        var questions = _repository.GetPublishedQuestions(command.ToeicPart);
        if (questions.Count == 0 || questions.Count < 5) // Throwing for empty or insufficient, arbitrarily choosing 5 as "insufficient" similar to MiniTest
        {
            throw new InvalidOperationException("Insufficient content");
        }

        var selectedIds = questions.Take(10).Select(q => q.QuestionId).ToList(); // Taking up to 10 for the test

        var session = new PartTestSession(
            SessionId: Guid.NewGuid().ToString(),
            LearnerId: command.LearnerId,
            ToeicPart: command.ToeicPart,
            Status: PartTestSessionStatus.Started,
            StartedAtUtc: DateTimeOffset.UtcNow,
            SubmittedAtUtc: null,
            ExpiredAtUtc: DateTimeOffset.UtcNow.AddMinutes(45),
            AssignedQuestionIds: selectedIds,
            Answers: new Dictionary<string, string>(),
            ResultId: null
        );

        _repository.UpsertPartTestSession(session);
        return session;
    }
}
