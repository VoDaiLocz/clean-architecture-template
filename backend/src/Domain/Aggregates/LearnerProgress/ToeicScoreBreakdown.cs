namespace Domain.Aggregates.LearnerProgress;

public record PartAccuracy(int Total, int Correct);

public record ToeicScoreBreakdown(
    string SessionId,
    string LearnerId,
    int TotalCorrect,
    int TotalQuestions,
    int EstimatedListeningScore,
    int EstimatedReadingScore,
    IReadOnlyDictionary<int, PartAccuracy> PartBreakdown,
    IReadOnlyDictionary<string, int> SkillTagWeaknesses
);
