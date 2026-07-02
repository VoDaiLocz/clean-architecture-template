using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;
using Domain.Aggregates.LearningItems;

namespace Application.Features.SourceValidation;

public sealed record ValidateToeicDraftContentCommand(string AssetId);

public sealed record ValidateToeicDraftContentResult(int ValidDraftCount, int InvalidDraftCount);

public sealed class ValidateToeicDraftContentHandler(IKnowledgeRepository repository)
{
    public ValidateToeicDraftContentResult Handle(ValidateToeicDraftContentCommand command)
    {
        var valid = 0;
        var invalid = 0;

        foreach (var draft in repository.GetDraftContentItems(command.AssetId))
        {
            var issues = Validate(draft);
            if (issues.Count == 0)
            {
                repository.UpsertDraftContentItem(draft with { Status = DraftContentStatus.ReadyForReview });
                valid++;
                continue;
            }

            repository.UpsertDraftContentItem(draft with { Status = DraftContentStatus.ValidationFailed });
            foreach (var issue in issues)
            {
                repository.RecordValidationIssue(issue, draft.ItemType, draft.DraftId);
            }
            invalid++;
        }

        return new ValidateToeicDraftContentResult(valid, invalid);
    }

    private static IReadOnlyList<ValidationIssue> Validate(DraftContentItem draft)
    {
        var issues = new List<ValidationIssue>();

        if (draft.ParserConfidence < 0.85m)
        {
            issues.Add(new ValidationIssue("low_parser_confidence", "Draft parser confidence is below validation threshold."));
        }

        if (string.IsNullOrWhiteSpace(draft.SourceTraceJson))
        {
            issues.Add(new ValidationIssue("missing_source_trace", "Draft content must keep source trace."));
        }

        if (draft.ItemType is "ReadingQuestion" or "ListeningQuestion"
            && !draft.PayloadJson.Contains("prompt", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new ValidationIssue("missing_prompt", "Draft question prompt is required."));
        }

        if (draft.ToeicPart is 3 or 4
            && !draft.PayloadJson.Contains("groupId", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new ValidationIssue("missing_group_ref", "Part 3 and Part 4 drafts require group relationship."));
        }

        if (draft.ToeicPart is 6 or 7
            && !draft.PayloadJson.Contains("passage", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new ValidationIssue("missing_passage_context", "Part 6 and Part 7 drafts require passage context."));
        }

        return issues;
    }
}
