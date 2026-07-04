using System;
using System.Collections.Generic;

namespace Domain.Aggregates.LearnerProgress;

public enum PartTestSessionStatus
{
    Started,
    Submitted,
    Expired
}

public sealed record PartTestSession(
    string SessionId,
    string LearnerId,
    int ToeicPart,
    PartTestSessionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset ExpiredAtUtc,
    IReadOnlyList<string> AssignedQuestionIds,
    IReadOnlyDictionary<string, string> Answers,
    string? ResultId
);
