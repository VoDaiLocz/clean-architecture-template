using System.Text.Json;
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

        ValidatePayloadEnvelope(draft, issues);

        if (draft.ItemType is "ReadingQuestion" or "ListeningQuestion"
            && !draft.PayloadJson.Contains("prompt", StringComparison.OrdinalIgnoreCase))
        {
            issues.Add(new ValidationIssue("missing_prompt", "Draft question prompt is required."));
        }

        ValidateQuestionAnswerContract(draft, issues);

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

    private static void ValidatePayloadEnvelope(DraftContentItem draft, List<ValidationIssue> issues)
    {
        try
        {
            using var document = JsonDocument.Parse(draft.PayloadJson);
            var root = document.RootElement;

            if (!root.TryGetProperty("schemaVersion", out var schemaVersion)
                || schemaVersion.GetString() != "toeic-draft.v1")
            {
                issues.Add(new ValidationIssue("missing_draft_schema_version", "Draft payload must use schemaVersion toeic-draft.v1."));
                return;
            }

            if (!root.TryGetProperty("kind", out var kind)
                || kind.GetString() != draft.ItemType)
            {
                issues.Add(new ValidationIssue("draft_kind_mismatch", "Draft payload kind must match item type."));
            }

            if (!root.TryGetProperty("data", out _))
            {
                issues.Add(new ValidationIssue("missing_draft_data", "Draft payload must wrap parser output in data."));
            }
        }
        catch (JsonException)
        {
            issues.Add(new ValidationIssue("malformed_draft_payload", "Draft payload must be valid JSON."));
        }
    }

    private static void ValidateQuestionAnswerContract(DraftContentItem draft, List<ValidationIssue> issues)
    {
        if (draft.ItemType is not ("ReadingQuestion" or "ListeningQuestion"))
        {
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(draft.PayloadJson);
            var root = document.RootElement;
            if (!root.TryGetProperty("schemaVersion", out var schemaVersion)
                || schemaVersion.GetString() != "toeic-draft.v1"
                || !root.TryGetProperty("data", out var data))
            {
                return;
            }

            var payload = data.TryGetProperty("parserPayload", out var parserPayload)
                ? parserPayload
                : data;

            if (!payload.TryGetProperty("options", out var options)
                || options.ValueKind != JsonValueKind.Object
                || !payload.TryGetProperty("correctAnswer", out var correctAnswer)
                || string.IsNullOrWhiteSpace(correctAnswer.GetString())
                || !options.TryGetProperty(correctAnswer.GetString()!, out _))
            {
                issues.Add(new ValidationIssue("invalid_answer_options", "Question draft must include options and a correct answer that exists in options."));
            }
        }
        catch (JsonException)
        {
            return;
        }
    }
}
