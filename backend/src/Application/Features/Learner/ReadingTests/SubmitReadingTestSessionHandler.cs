using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.ReadingTests;

public record SubmitReadingTestCommand(
    string LearnerId,
    string SessionId,
    Dictionary<string, string> Answers
);

public record SubmitReadingTestResponse(
    string SessionId,
    ReadingTestSessionStatus Status
);

public class SubmitReadingTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public SubmitReadingTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task<SubmitReadingTestResponse> Handle(SubmitReadingTestCommand request, CancellationToken cancellationToken)
    {
        var session = _repository.GetReadingTestSession(request.SessionId);
        if (session == null || session.LearnerId != request.LearnerId)
        {
            throw new InvalidOperationException("Session not found");
        }

        if (session.Status == ReadingTestSessionStatus.Submitted)
        {
            throw new InvalidOperationException("Already submitted");
        }
        
        if (session.Status == ReadingTestSessionStatus.Expired)
        {
            throw new InvalidOperationException("Expired");
        }

        var now = DateTimeOffset.UtcNow;
        if (now > session.ExpiredAtUtc)
        {
            session.Status = ReadingTestSessionStatus.Expired;
            _repository.UpsertReadingTestSession(session);
            throw new InvalidOperationException("Expired");
        }

        session.Status = ReadingTestSessionStatus.Submitted;
        session.SubmittedAtUtc = now;
        session.Answers = request.Answers;

        _repository.UpsertReadingTestSession(session);

        return Task.FromResult(new SubmitReadingTestResponse(
            session.SessionId,
            session.Status
        ));
    }
}
