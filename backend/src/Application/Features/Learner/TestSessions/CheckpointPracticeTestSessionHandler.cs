using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.TestSessions;

public record CheckpointPracticeTestSessionCommand(string LearnerId, string SessionId, Dictionary<string, string> Answers);

public class CheckpointPracticeTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public CheckpointPracticeTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task Handle(CheckpointPracticeTestSessionCommand request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var mini = _repository.GetMiniTestSession(request.SessionId);
        if (mini != null)
        {
            if (mini.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            if (mini.Status != MiniTestSessionStatus.Started) throw new InvalidOperationException("Session not started");
            
            if (mini.ExpiredAtUtc <= now)
            {
                var expiredSession = mini with { Status = MiniTestSessionStatus.Expired };
                _repository.UpsertMiniTestSession(expiredSession);
                throw new InvalidOperationException("Expired");
            }

            var updatedSession = mini with { Answers = request.Answers };
            _repository.UpsertMiniTestSession(updatedSession);
            return Task.CompletedTask;
        }

        var part = _repository.GetPartTestSession(request.SessionId);
        if (part != null)
        {
            if (part.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            if (part.Status != PartTestSessionStatus.Started) throw new InvalidOperationException("Session not started");
            
            if (part.ExpiredAtUtc <= now)
            {
                var expiredSession = part with { Status = PartTestSessionStatus.Expired };
                _repository.UpsertPartTestSession(expiredSession);
                throw new InvalidOperationException("Expired");
            }

            var updatedSession = part with { Answers = request.Answers };
            _repository.UpsertPartTestSession(updatedSession);
            return Task.CompletedTask;
        }

        var listening = _repository.GetListeningTestSession(request.SessionId);
        if (listening != null)
        {
            if (listening.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            if (listening.Status != ListeningTestSessionStatus.Started) throw new InvalidOperationException("Session not started");
            
            if (listening.ExpiredAtUtc <= now)
            {
                listening.Status = ListeningTestSessionStatus.Expired;
                _repository.UpsertListeningTestSession(listening);
                throw new InvalidOperationException("Expired");
            }

            listening.Answers = request.Answers;
            _repository.UpsertListeningTestSession(listening);
            return Task.CompletedTask;
        }

        var reading = _repository.GetReadingTestSession(request.SessionId);
        if (reading != null)
        {
            if (reading.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            if (reading.Status != ReadingTestSessionStatus.Started) throw new InvalidOperationException("Session not started");
            
            if (reading.ExpiredAtUtc <= now)
            {
                reading.Status = ReadingTestSessionStatus.Expired;
                _repository.UpsertReadingTestSession(reading);
                throw new InvalidOperationException("Expired");
            }

            reading.Answers = request.Answers;
            _repository.UpsertReadingTestSession(reading);
            return Task.CompletedTask;
        }

        var full = _repository.GetFullTestSession(request.SessionId);
        if (full != null)
        {
            if (full.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            if (full.Status != FullTestSessionStatus.Started) throw new InvalidOperationException("Session not started");
            
            if (full.ExpiredAtUtc <= now)
            {
                var expiredSession = full with { Status = FullTestSessionStatus.Expired };
                _repository.UpsertFullTestSession(expiredSession);
                throw new InvalidOperationException("Expired");
            }

            var updatedSession = full with { Answers = request.Answers };
            _repository.UpsertFullTestSession(updatedSession);
            return Task.CompletedTask;
        }

        throw new InvalidOperationException("Session not found");
    }
}
