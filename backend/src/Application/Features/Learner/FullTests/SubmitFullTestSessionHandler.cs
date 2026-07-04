using System;
using System.Collections.Generic;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.FullTests;

public record SubmitFullTestCommand(string LearnerId, string SessionId, Dictionary<string, string> Answers);

public class SubmitFullTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public SubmitFullTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public FullToeicTestSession Handle(SubmitFullTestCommand command)
    {
        var session = _repository.GetFullTestSession(command.SessionId);
        if (session == null) throw new InvalidOperationException("Session not found");

        if (session.LearnerId != command.LearnerId)
        {
            throw new InvalidOperationException("Not owner");
        }

        if (session.Status != FullTestSessionStatus.Started)
        {
            throw new InvalidOperationException("Already submitted or expired");
        }

        if (DateTimeOffset.UtcNow > session.ExpiredAtUtc)
        {
            var expiredSession = session with { Status = FullTestSessionStatus.Expired };
            _repository.UpsertFullTestSession(expiredSession);
            throw new InvalidOperationException("Expired");
        }

        var submittedSession = session with 
        { 
            Status = FullTestSessionStatus.Submitted,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            Answers = command.Answers
        };

        _repository.UpsertFullTestSession(submittedSession);

        return submittedSession;
    }
}
