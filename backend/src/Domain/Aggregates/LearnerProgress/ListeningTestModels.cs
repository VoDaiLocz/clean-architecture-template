using System;
using System.Collections.Generic;

namespace Domain.Aggregates.LearnerProgress;

public enum ListeningTestSessionStatus
{
    Started,
    Submitted,
    Expired
}

public class ListeningTestSession
{
    public required string SessionId { get; init; }
    public required string LearnerId { get; init; }
    public ListeningTestSessionStatus Status { get; set; }
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset? SubmittedAtUtc { get; set; }
    public DateTimeOffset ExpiredAtUtc { get; init; }
    
    public IReadOnlyList<string> AssignedQuestionIds { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> Answers { get; set; } = new Dictionary<string, string>();
    public string? ResultId { get; set; }
}
