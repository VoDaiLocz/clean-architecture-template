using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.TestSessions;

public record GetPracticeTestSessionQuery(string LearnerId, string SessionId);

public class GetPracticeTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public GetPracticeTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task<ToeicTestSessionState> Handle(GetPracticeTestSessionQuery request, CancellationToken cancellationToken)
    {
        var mini = _repository.GetMiniTestSession(request.SessionId);
        if (mini != null)
        {
            if (mini.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            return Task.FromResult(new ToeicTestSessionState(
                mini.SessionId, mini.LearnerId, PracticeTestType.Mini,
                MapStatus(mini.Status), mini.StartedAtUtc, mini.SubmittedAtUtc,
                mini.ExpiredAtUtc, mini.AssignedQuestionIds, mini.Answers, mini.ResultId));
        }

        var part = _repository.GetPartTestSession(request.SessionId);
        if (part != null)
        {
            if (part.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            return Task.FromResult(new ToeicTestSessionState(
                part.SessionId, part.LearnerId, PracticeTestType.Part,
                MapStatus(part.Status), part.StartedAtUtc, part.SubmittedAtUtc,
                part.ExpiredAtUtc, part.AssignedQuestionIds, part.Answers, part.ResultId));
        }

        var listening = _repository.GetListeningTestSession(request.SessionId);
        if (listening != null)
        {
            if (listening.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            return Task.FromResult(new ToeicTestSessionState(
                listening.SessionId, listening.LearnerId, PracticeTestType.Listening,
                MapStatus(listening.Status), listening.StartedAtUtc, listening.SubmittedAtUtc,
                listening.ExpiredAtUtc, listening.AssignedQuestionIds, listening.Answers, listening.ResultId));
        }

        var reading = _repository.GetReadingTestSession(request.SessionId);
        if (reading != null)
        {
            if (reading.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            return Task.FromResult(new ToeicTestSessionState(
                reading.SessionId, reading.LearnerId, PracticeTestType.Reading,
                MapStatus(reading.Status), reading.StartedAtUtc, reading.SubmittedAtUtc,
                reading.ExpiredAtUtc, reading.AssignedQuestionIds, reading.Answers, reading.ResultId));
        }

        var full = _repository.GetFullTestSession(request.SessionId);
        if (full != null)
        {
            if (full.LearnerId != request.LearnerId) throw new InvalidOperationException("Unauthorized");
            return Task.FromResult(new ToeicTestSessionState(
                full.SessionId, full.LearnerId, PracticeTestType.Full,
                MapStatus(full.Status), full.StartedAtUtc, full.SubmittedAtUtc,
                full.ExpiredAtUtc, full.AssignedQuestionIds, full.Answers, full.ResultId));
        }

        throw new InvalidOperationException("Session not found");
    }

    private static PracticeTestStatus MapStatus(MiniTestSessionStatus status) => status switch
    {
        MiniTestSessionStatus.Started => PracticeTestStatus.Started,
        MiniTestSessionStatus.Submitted => PracticeTestStatus.Submitted,
        MiniTestSessionStatus.Expired => PracticeTestStatus.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static PracticeTestStatus MapStatus(PartTestSessionStatus status) => status switch
    {
        PartTestSessionStatus.Started => PracticeTestStatus.Started,
        PartTestSessionStatus.Submitted => PracticeTestStatus.Submitted,
        PartTestSessionStatus.Expired => PracticeTestStatus.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static PracticeTestStatus MapStatus(ListeningTestSessionStatus status) => status switch
    {
        ListeningTestSessionStatus.Started => PracticeTestStatus.Started,
        ListeningTestSessionStatus.Submitted => PracticeTestStatus.Submitted,
        ListeningTestSessionStatus.Expired => PracticeTestStatus.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static PracticeTestStatus MapStatus(ReadingTestSessionStatus status) => status switch
    {
        ReadingTestSessionStatus.Started => PracticeTestStatus.Started,
        ReadingTestSessionStatus.Submitted => PracticeTestStatus.Submitted,
        ReadingTestSessionStatus.Expired => PracticeTestStatus.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    private static PracticeTestStatus MapStatus(FullTestSessionStatus status) => status switch
    {
        FullTestSessionStatus.Started => PracticeTestStatus.Started,
        FullTestSessionStatus.Submitted => PracticeTestStatus.Submitted,
        FullTestSessionStatus.Expired => PracticeTestStatus.Expired,
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };
}
