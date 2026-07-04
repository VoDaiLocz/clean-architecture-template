using System;
using System.Collections.Generic;

namespace Domain.Aggregates.LearnerProgress;

public enum MiniTestSessionStatus
{
    Started,
    Submitted,
    Expired
}

public sealed record MiniTestSession(
    string SessionId,
    string LearnerId,
    string UnitId,
    MiniTestSessionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset ExpiredAtUtc,
    IReadOnlyList<string> AssignedQuestionIds,
    IReadOnlyDictionary<string, string> Answers,
    string? ResultId
);
