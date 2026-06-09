namespace Domain.Aggregates.LearningItems;

public static class ValidationGate
{
    private static readonly HashSet<int> ListeningParts = [1, 2, 3, 4];
    private static readonly HashSet<int> ReadingParts = [5, 6, 7];
    private static readonly HashSet<int> GroupedParts = [3, 4, 6, 7];
    private const decimal PublishConfidenceMinimum = 0.8m;

    public static ValidationResult Validate(DraftLearningItem item)
    {
        var issues = new List<ValidationIssue>();
        var needsReview = false;

        if (item.SourceRef is null)
        {
            issues.Add(new ValidationIssue("missing_source_ref", "Every item must keep source provenance."));
        }

        if (item.Confidence < PublishConfidenceMinimum)
        {
            issues.Add(new ValidationIssue("low_confidence", "Item confidence is below publish threshold."));
            needsReview = true;
        }

        switch (item.ItemType)
        {
            case LearningItemType.Question:
                ValidateQuestion(item, issues);
                break;
            case LearningItemType.Vocabulary:
                ValidateVocabulary(item, issues);
                break;
            default:
                issues.Add(new ValidationIssue("unsupported_item_type", "Unsupported learning item type."));
                break;
        }

        return new ValidationResult(issues, needsReview);
    }

    private static void ValidateQuestion(DraftLearningItem item, List<ValidationIssue> issues)
    {
        if (item.Part is int part)
        {
            if (ListeningParts.Contains(part) && item.Skill != ToeicSkill.Listening)
            {
                issues.Add(new ValidationIssue("part_skill_mismatch", "TOEIC parts 1-4 are listening."));
            }

            if (ReadingParts.Contains(part) && item.Skill != ToeicSkill.Reading)
            {
                issues.Add(new ValidationIssue("part_skill_mismatch", "TOEIC parts 5-7 are reading."));
            }

            if (GroupedParts.Contains(part) && string.IsNullOrWhiteSpace(item.GroupRef))
            {
                issues.Add(new ValidationIssue("missing_group_ref", "Grouped parts require passage or transcript source."));
            }
        }

        if (string.IsNullOrWhiteSpace(item.Prompt))
        {
            issues.Add(new ValidationIssue("missing_prompt", "Question prompt is required."));
        }

        if (item.Options.Count == 0)
        {
            issues.Add(new ValidationIssue("missing_options", "Question options are required."));
        }

        if (!item.Options.ContainsKey(item.CorrectAnswer))
        {
            issues.Add(new ValidationIssue("answer_not_in_options", "Correct answer must match an option label."));
        }
    }

    private static void ValidateVocabulary(DraftLearningItem item, List<ValidationIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(item.Word))
        {
            issues.Add(new ValidationIssue("missing_vocabulary_word", "Vocabulary item requires a word."));
        }

        if (string.IsNullOrWhiteSpace(item.Meaning))
        {
            issues.Add(new ValidationIssue("missing_vocabulary_meaning", "Vocabulary item requires a meaning."));
        }
    }
}
