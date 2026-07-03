namespace Domain.Aggregates.LearningItems;

public enum LearningItemType
{
    Question,
    Vocabulary
}

public enum ToeicSkill
{
    Listening,
    Reading
}

public sealed record SourceRef(
    string SourceId,
    string FileId,
    int? Page,
    string? BlockId
);

public sealed record DraftLearningItem(
    LearningItemType ItemType,
    ToeicSkill Skill,
    int? Part,
    string Prompt,
    IReadOnlyDictionary<string, string> Options,
    string CorrectAnswer,
    string Explanation,
    SourceRef? SourceRef,
    decimal Confidence,
    string? GroupRef,
    string Word,
    string Meaning
);

public sealed record ValidationIssue(string Code, string Message);

public sealed record ValidationIssueCodeCount(string Code, int Count);

public sealed record ValidationResult(
    IReadOnlyList<ValidationIssue> Issues,
    bool NeedsReview
)
{
    public bool CanPublish => Issues.Count == 0 && !NeedsReview;
}
