using System;
using System.Collections.Generic;

namespace Domain.Aggregates.LearnerProgress;

public enum FullTestSessionStatus
{
    Started,
    Submitted,
    Expired
}

public sealed record FullToeicTestSession(
    string SessionId,
    string LearnerId,
    FullTestSessionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset ExpiredAtUtc,
    IReadOnlyList<string> AssignedQuestionIds,
    IReadOnlyDictionary<string, string> Answers,
    string? ResultId
);
