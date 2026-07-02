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
        var root = document.RootElement;
        var toeicPart = draft.ToeicPart ?? throw new InvalidOperationException("Published draft requires TOEIC part.");

        return new PublishedQuestion(
            QuestionId: $"published-question-{draft.DraftId}",
            LessonId: lessonId,
            ToeicPart: toeicPart,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: root.GetProperty("prompt").GetString() ?? "",
            OptionsJson: root.GetProperty("options").GetRawText(),
            CorrectAnswer: root.GetProperty("correctAnswer").GetString() ?? "",
            Explanation: root.TryGetProperty("explanation", out var explanation) ? explanation.GetString() ?? "" : "Reviewed TOEIC explanation pending enrichment.",
            MediaAssetId: null,
            PassageId: root.TryGetProperty("passageId", out var passageId) ? passageId.GetString() : null,
            GroupId: root.TryGetProperty("groupId", out var groupId) ? groupId.GetString() : null,
            EvidenceJson: draft.SourceTraceJson,
            SkillTags: root.TryGetProperty("skillTags", out var tags) ? tags.GetRawText() : "[]",
            SourceTraceJson: draft.SourceTraceJson,
            Status: PublishedContentStatus.Published
        );
    }
}
