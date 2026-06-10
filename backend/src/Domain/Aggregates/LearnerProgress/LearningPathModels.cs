namespace Domain.Aggregates.LearnerProgress;

public sealed record LearningPathCatalog(IReadOnlyList<LearningUnitDefinition> Units)
{
    public static LearningPathCatalog CreateDefault() =>
        new(
            [
                new LearningUnitDefinition("part5-word-form", 5, "Word Form", null, 80),
                new LearningUnitDefinition("part5-verb-tense", 5, "Verb Tense", "part5-word-form", 80),
                new LearningUnitDefinition("part2-wh-question", 2, "WH Questions", null, 80),
            ]
        );

    public LearningUnitDefinition GetUnit(string unitId) =>
        Units.Single(unit => unit.UnitId == unitId);
}

public sealed record LearningUnitDefinition(
    string UnitId,
    int Part,
    string Title,
    string? RequiredPreviousUnitId,
    int MiniTestThresholdPercent
);

public sealed class LearnerState
{
    private LearnerState(string learnerId, string activeUnitId)
    {
        LearnerId = learnerId;
        ActiveUnitId = activeUnitId;
    }

    public string LearnerId { get; }

    public string ActiveUnitId { get; set; }

    public HashSet<string> ViewedLessonUnitIds { get; } = [];

    public HashSet<string> CompletedDrillUnitIds { get; } = [];

    public HashSet<string> CompletedUnitIds { get; } = [];

    public List<ReviewItemState> ReviewQueue { get; } = [];

    public static LearnerState Start(string learnerId, LearningPathCatalog catalog)
    {
        var firstUnit = catalog.Units.First();
        return new LearnerState(learnerId, firstUnit.UnitId);
    }
}

public sealed record ReviewItemState(
    string ReviewItemId,
    string UnitId,
    string QuestionId,
    string ErrorTag,
    bool Resolved
);

public sealed record UnitAccessResult(bool CanStart, IReadOnlyList<string> ReasonCodes);

public sealed record MiniTestAttemptResult(
    int ScorePercent,
    bool UnitCompleted,
    IReadOnlyList<string> CreatedReviewItemIds
);
