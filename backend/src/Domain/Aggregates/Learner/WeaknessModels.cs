using System;

namespace Domain.Aggregates.Learner;

public record LearnerWeaknessEvent(
    string EventId,
    string LearnerId,
    string SourceActivityId,
    int ToeicPart,
    string SkillTag,
    decimal Weight,
    bool IsCorrect,
    DateTimeOffset CreatedAtUtc
);

public record LearnerWeaknessSummary(
    string LearnerId,
    int ToeicPart,
    string SkillTag,
    decimal SeverityScore,
    int EvidenceCount,
    DateTimeOffset LastUpdatedAtUtc
);
