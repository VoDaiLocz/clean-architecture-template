using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.ListeningTests;

public record SubmitListeningTestCommand(
    string LearnerId,
    string SessionId,
    Dictionary<string, string> Answers
);

public record SubmitListeningTestResponse(
    string SessionId,
    ListeningTestSessionStatus Status
);

public class SubmitListeningTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public SubmitListeningTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task<SubmitListeningTestResponse> Handle(SubmitListeningTestCommand request, CancellationToken cancellationToken)
    {
        var session = _repository.GetListeningTestSession(request.SessionId);
        if (session == null || session.LearnerId != request.LearnerId)
        {
            throw new InvalidOperationException("Session not found");
        }

        if (session.Status == ListeningTestSessionStatus.Submitted)
        {
            throw new InvalidOperationException("Already submitted");
        }
        
        if (session.Status == ListeningTestSessionStatus.Expired)
        {
            throw new InvalidOperationException("Expired");
        }

        var now = DateTimeOffset.UtcNow;
        if (now > session.ExpiredAtUtc)
        {
            session.Status = ListeningTestSessionStatus.Expired;
            _repository.UpsertListeningTestSession(session);
            throw new InvalidOperationException("Expired");
        }

        session.Status = ListeningTestSessionStatus.Submitted;
        session.SubmittedAtUtc = now;
        session.Answers = request.Answers;

        _repository.UpsertListeningTestSession(session);

        return Task.FromResult(new SubmitListeningTestResponse(
            session.SessionId,
            session.Status
        ));
    }
}
