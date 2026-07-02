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

public enum LearnerProfileStatus
{
    Active,
    Suspended,
    Deleted,
}

public sealed record LearnerProfile(
    string LearnerId,
    string DisplayName,
    string Email,
    int TargetScore,
    int CurrentEstimatedScore,
    int DailyStudyMinutes,
    string TimeZoneId,
    LearnerProfileStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc
);

public enum PlacementSessionStatus
{
    InProgress,
    Completed,
    Cancelled,
}

public sealed record PlacementSession(
    string SessionId,
    string LearnerId,
    PlacementSessionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc
);

public static class PlacementRules
{
    public static void EnsureValid(PlacementSession session)
    {
        RequireText(session.SessionId, "Placement session id is required.");
        RequireText(session.LearnerId, "Placement session learner id is required.");

        if (session.Status != PlacementSessionStatus.Completed && session.CompletedAtUtc is not null)
        {
            throw new InvalidOperationException("Only completed placement sessions can have completed timestamp.");
        }
    }

    private static void RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }
    }
}

public enum LearnerAssignmentType
{
    Lesson,
    Drill,
    MiniTest,
    PartTest,
    FullTest,
    Review,
}

public enum LearnerAssignmentStatus
{
    Assigned,
    Started,
    Completed,
    Cancelled,
}

public enum ActivitySessionStatus
{
    InProgress,
    Completed,
    Abandoned,
}

public enum LearnerAttemptStatus
{
    Submitted,
    Scored,
    Invalidated,
}

public sealed record LearnerAssignment(
    string AssignmentId,
    string LearnerId,
    LearnerAssignmentType AssignmentType,
    string ContentRefId,
    LearnerAssignmentStatus Status,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? DueAtUtc
);

public sealed record ActivitySession(
    string SessionId,
    string AssignmentId,
    string LearnerId,
    LearnerAssignmentType ActivityType,
    ActivitySessionStatus Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? CompletedAtUtc
);

public sealed record LearnerAttempt(
    string AttemptId,
    string SessionId,
    string LearnerId,
    LearnerAttemptStatus Status,
    int CorrectCount,
    int TotalCount,
    int ScorePercent,
    DateTimeOffset SubmittedAtUtc
);

public sealed record AttemptAnswer(
    string AnswerId,
    string AttemptId,
    string QuestionId,
    string LearnerAnswer,
    string CorrectAnswer,
    bool IsCorrect,
    DateTimeOffset AnsweredAtUtc
);

public static class LearnerWorkRules
{
    public static void EnsureValid(LearnerAssignment assignment)
    {
        RequireText(assignment.AssignmentId, "Assignment id is required.");
        RequireText(assignment.LearnerId, "Assignment learner id is required.");
        RequireText(assignment.ContentRefId, "Assignment content reference is required.");
    }

    public static void EnsureValid(ActivitySession session)
    {
        RequireText(session.SessionId, "Activity session id is required.");
        RequireText(session.AssignmentId, "Activity session assignment id is required.");
        RequireText(session.LearnerId, "Activity session learner id is required.");
    }

    public static void EnsureValid(LearnerAttempt attempt)
    {
        RequireText(attempt.AttemptId, "Attempt id is required.");
        RequireText(attempt.SessionId, "Attempt session id is required.");
        RequireText(attempt.LearnerId, "Attempt learner id is required.");

        if (attempt.TotalCount <= 0 || attempt.CorrectCount < 0 || attempt.CorrectCount > attempt.TotalCount)
        {
            throw new InvalidOperationException("Attempt correct count must be between zero and total count.");
        }

        if (attempt.ScorePercent is < 0 or > 100)
        {
            throw new InvalidOperationException("Attempt score percent must be between 0 and 100.");
        }
    }

    public static void EnsureValid(AttemptAnswer answer)
    {
        RequireText(answer.AnswerId, "Attempt answer id is required.");
        RequireText(answer.AttemptId, "Attempt answer attempt id is required.");
        RequireText(answer.QuestionId, "Attempt answer question id is required.");
        RequireText(answer.CorrectAnswer, "Attempt answer correct answer is required.");
    }

    private static void RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }
    }
}

public enum ReviewItemStatus
{
    Open,
    Resolved,
    Dismissed,
}

public sealed record ReviewItem(
    string ReviewItemId,
    string LearnerId,
    string SourceAttemptId,
    string QuestionId,
    string UnitId,
    string ErrorTag,
    string LearnerAnswer,
    string CorrectAnswer,
    ReviewItemStatus Status,
    bool IsBlocking,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ResolvedAtUtc
);

public sealed record RepairAttempt(
    string RepairAttemptId,
    string ReviewItemId,
    string LearnerId,
    string Answer,
    bool IsCorrect,
    DateTimeOffset AttemptedAtUtc
);

public sealed record MasteryRecord(
    string MasteryRecordId,
    string LearnerId,
    string UnitId,
    int MasteryPercent,
    bool IsUnlocked,
    int BlockingReviewCount,
    DateTimeOffset UpdatedAtUtc
);

public static class ReviewMasteryRules
{
    public static void EnsureValid(ReviewItem item)
    {
        RequireText(item.ReviewItemId, "Review item id is required.");
        RequireText(item.LearnerId, "Review item learner id is required.");
        RequireText(item.QuestionId, "Review item question id is required.");
        RequireText(item.UnitId, "Review item unit id is required.");
        RequireText(item.CorrectAnswer, "Review item correct answer is required.");
    }

    public static void EnsureValid(RepairAttempt attempt)
    {
        RequireText(attempt.RepairAttemptId, "Repair attempt id is required.");
        RequireText(attempt.ReviewItemId, "Repair attempt review item id is required.");
        RequireText(attempt.LearnerId, "Repair attempt learner id is required.");
        RequireText(attempt.Answer, "Repair attempt answer is required.");
    }

    public static void EnsureValid(MasteryRecord record)
    {
        RequireText(record.MasteryRecordId, "Mastery record id is required.");
        RequireText(record.LearnerId, "Mastery record learner id is required.");
        RequireText(record.UnitId, "Mastery record unit id is required.");

        if (record.MasteryPercent is < 0 or > 100 || record.BlockingReviewCount < 0)
        {
            throw new InvalidOperationException("Mastery percent must be 0-100 and blocking review count cannot be negative.");
        }
    }

    private static void RequireText(string value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(message);
        }
    }
}
