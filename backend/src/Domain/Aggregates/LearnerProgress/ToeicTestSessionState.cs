using System;
using System.Collections.Generic;

namespace Domain.Aggregates.LearnerProgress;

public enum PracticeTestType { Mini, Part, Listening, Reading, Full }
public enum PracticeTestStatus { Started, Submitted, Expired }

public record ToeicTestSessionState(
    string SessionId,
    string LearnerId,
    PracticeTestType TestType,
    PracticeTestStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? SubmittedAtUtc,
    DateTimeOffset ExpiredAtUtc,
    IReadOnlyList<string> AssignedQuestionIds,
    IReadOnlyDictionary<string, string> Answers,
    string? ResultId
);
