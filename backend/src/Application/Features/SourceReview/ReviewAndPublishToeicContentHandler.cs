using System.Text.Json;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceReview;

public enum ReviewDecisionAction
{
    Approve,
    Reject,
}

public sealed record ReviewDecision(string DraftId, ReviewDecisionAction Action);

public sealed record ReviewAndPublishToeicContentCommand(
    string AssetId,
    string LessonId,
    IReadOnlyList<ReviewDecision> Decisions
);

public sealed record ReviewAndPublishToeicContentResult(int PublishedCount, int RejectedCount);

public sealed class ReviewAndPublishToeicContentHandler(IKnowledgeRepository repository)
{
    public ReviewAndPublishToeicContentResult Handle(ReviewAndPublishToeicContentCommand command)
    {
        var drafts = repository.GetDraftContentItems(command.AssetId);
        var published = 0;
        var rejected = 0;

        foreach (var decision in command.Decisions)
        {
            var draft = drafts.Single(item => item.DraftId == decision.DraftId);
            if (draft.Status != DraftContentStatus.ReadyForReview)
            {
                throw new InvalidOperationException("Only ready-for-review drafts can receive review decisions.");
            }

            if (decision.Action == ReviewDecisionAction.Reject)
            {
                repository.UpsertDraftContentItem(draft with { Status = DraftContentStatus.Rejected });
                rejected++;
                continue;
            }

            repository.UpsertPublishedQuestion(CreatePublishedQuestion(draft, command.LessonId));
            repository.UpsertDraftContentItem(draft with { Status = DraftContentStatus.Published });
            published++;
        }

        return new ReviewAndPublishToeicContentResult(published, rejected);
    }

    private static PublishedQuestion CreatePublishedQuestion(DraftContentItem draft, string lessonId)
    {
        using var document = JsonDocument.Parse(draft.PayloadJson);
        var data = GetDraftData(document.RootElement);
        var parserPayload = data.TryGetProperty("parserPayload", out var nestedPayload)
            ? nestedPayload
            : data;
        var toeicPart = draft.ToeicPart ?? throw new InvalidOperationException("Published draft requires TOEIC part.");

        return new PublishedQuestion(
            QuestionId: $"published-question-{draft.DraftId}",
            LessonId: lessonId,
            ToeicPart: toeicPart,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: data.GetProperty("prompt").GetString() ?? "",
            OptionsJson: parserPayload.GetProperty("options").GetRawText(),
            CorrectAnswer: parserPayload.GetProperty("correctAnswer").GetString() ?? "",
            Explanation: parserPayload.TryGetProperty("explanation", out var explanation) ? explanation.GetString() ?? "" : "Reviewed TOEIC explanation pending enrichment.",
            MediaAssetId: null,
            PassageId: GetOptionalString(data, parserPayload, "passageId"),
            GroupId: GetOptionalString(data, parserPayload, "groupId"),
            EvidenceJson: draft.SourceTraceJson,
            SkillTags: data.TryGetProperty("skillTags", out var tags) ? tags.GetRawText() : "[]",
            SourceTraceJson: draft.SourceTraceJson,
            Status: PublishedContentStatus.Published
        );
    }

    private static JsonElement GetDraftData(JsonElement root)
    {
        if (root.TryGetProperty("schemaVersion", out _)
            && root.TryGetProperty("data", out var data))
        {
            return data;
        }

        return root;
    }

    private static string? GetOptionalString(JsonElement data, JsonElement parserPayload, string propertyName)
    {
        if (data.TryGetProperty(propertyName, out var dataValue))
        {
            return dataValue.GetString();
        }

        return parserPayload.TryGetProperty(propertyName, out var payloadValue) ? payloadValue.GetString() : null;
    }
}
