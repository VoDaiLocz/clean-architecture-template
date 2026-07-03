using System.Text.Json;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceExtraction;

public sealed record ParseToeicListeningGroupsCommand(string AssetId);

public sealed record ParseToeicListeningGroupsResult(int CreatedListeningDraftCount, int CreatedGroupCount);

public interface IListeningDraftParser
{
    IReadOnlyList<ListeningDraftQuestionResult> Parse(SourceAsset asset);
}

public sealed record ListeningDraftQuestionResult(
    int ToeicPart,
    string GroupId,
    int QuestionNumber,
    string Prompt,
    IReadOnlyList<string> SkillTags,
    string PayloadJson,
    decimal Confidence
);

public sealed class ParseToeicListeningGroupsHandler(
    IKnowledgeRepository repository,
    IListeningDraftParser parser
)
{
    public ParseToeicListeningGroupsResult Handle(ParseToeicListeningGroupsCommand command)
    {
        var asset = repository.GetSourceAsset(command.AssetId)
            ?? throw new InvalidOperationException($"Source asset not found: {command.AssetId}");

        if (asset.DetectedRole != SourceAssetRole.Audio)
        {
            throw new InvalidOperationException("Listening group parsing requires an audio source asset.");
        }

        var count = 0;
        var groupIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var question in parser.Parse(asset))
        {
            EnsureValidGroup(question);
            count++;
            groupIds.Add(question.GroupId);
            repository.UpsertDraftContentItem(new DraftContentItem(
                DraftId: $"draft-listening-{asset.AssetId}-{question.QuestionNumber}",
                AssetId: asset.AssetId,
                MaterialClass: MaterialClass.TestBook,
                ToeicPart: question.ToeicPart,
                ItemType: "ListeningQuestion",
                PayloadJson: MergePayload(question),
                SourceTraceJson: JsonSerializer.Serialize(new
                {
                    asset.AssetId,
                    asset.SourceId,
                    question.GroupId,
                }),
                ParserConfidence: question.Confidence,
                Status: DraftContentStatus.PendingValidation
            ));
        }

        return new ParseToeicListeningGroupsResult(count, groupIds.Count);
    }

    private static void EnsureValidGroup(ListeningDraftQuestionResult question)
    {
        if (question.ToeicPart is 3 or 4 && string.IsNullOrWhiteSpace(question.GroupId))
        {
            throw new InvalidOperationException("Part 3 and Part 4 listening drafts require a group id.");
        }
    }

    private static string MergePayload(ListeningDraftQuestionResult question) =>
        DraftPayloadEnvelope.Serialize("ListeningQuestion", new
        {
            groupId = question.GroupId,
            questionNumber = question.QuestionNumber,
            prompt = question.Prompt,
            skillTags = question.SkillTags,
            parserPayload = JsonSerializer.Deserialize<object>(question.PayloadJson),
        });
}
