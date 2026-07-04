using System;
using System.Collections.Generic;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.MiniTests;

public record SubmitMiniTestCommand(string LearnerId, string SessionId, Dictionary<string, string> Answers);

public class SubmitMiniTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public SubmitMiniTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public MiniTestSession Handle(SubmitMiniTestCommand command)
    {
        var session = _repository.GetMiniTestSession(command.SessionId);
        if (session == null) throw new InvalidOperationException("Session not found");

        if (session.LearnerId != command.LearnerId)
        {
            throw new InvalidOperationException("Not owner");
        }

        if (session.Status != MiniTestSessionStatus.Started)
        {
            throw new InvalidOperationException("Already submitted or expired");
        }

        if (DateTimeOffset.UtcNow > session.ExpiredAtUtc)
        {
            var expiredSession = session with { Status = MiniTestSessionStatus.Expired };
            _repository.UpsertMiniTestSession(expiredSession);
            throw new InvalidOperationException("Expired");
        }

        var submittedSession = session with 
        { 
            Status = MiniTestSessionStatus.Submitted,
            SubmittedAtUtc = DateTimeOffset.UtcNow,
            Answers = command.Answers
        };

        _repository.UpsertMiniTestSession(submittedSession);

        return submittedSession;
    }
}
