using System;
using System.Collections.Generic;

namespace Domain.Aggregates.LearnerProgress;

public enum ReadingTestSessionStatus
{
    Started,
    Submitted,
    Expired
}

public class ReadingTestSession
{
    public string SessionId { get; init; } = string.Empty;
    public string LearnerId { get; init; } = string.Empty;
    public ReadingTestSessionStatus Status { get; set; } = ReadingTestSessionStatus.Started;
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public DateTimeOffset ExpiredAtUtc { get; init; }
    public IReadOnlyList<string> AssignedQuestionIds { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Answers { get; set; } = new Dictionary<string, string>();
    public string? ResultId { get; set; }
}
