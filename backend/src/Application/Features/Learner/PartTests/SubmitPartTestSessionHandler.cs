using System;
using System.Collections.Generic;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.PartTests;

public record SubmitPartTestCommand(string LearnerId, string SessionId, Dictionary<string, string> Answers);

public class SubmitPartTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public SubmitPartTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public PartTestSession Handle(SubmitPartTestCommand command)
    {
        var session = _repository.GetPartTestSession(command.SessionId);
        if (session == null)
        {
            throw new InvalidOperationException("Session not found");
        }

        if (session.LearnerId != command.LearnerId)
        {
            throw new InvalidOperationException("Not owner");
        }

        if (session.Status == PartTestSessionStatus.Submitted)
        {
            throw new InvalidOperationException("Already submitted");
        }

        if (session.Status == PartTestSessionStatus.Expired)
        {
            throw new InvalidOperationException("Expired");
        }

        var now = DateTimeOffset.UtcNow;
        if (now > session.ExpiredAtUtc)
        {
            var expiredSession = session with { Status = PartTestSessionStatus.Expired };
            _repository.UpsertPartTestSession(expiredSession);
            throw new InvalidOperationException("Expired");
        }

        var submittedSession = session with
        {
            Status = PartTestSessionStatus.Submitted,
            SubmittedAtUtc = now,
            Answers = command.Answers
        };

        _repository.UpsertPartTestSession(submittedSession);
        return submittedSession;
    }
}
