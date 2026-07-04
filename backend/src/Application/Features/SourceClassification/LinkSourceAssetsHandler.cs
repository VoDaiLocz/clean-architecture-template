using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceClassification;

public class LinkSourceAssetsHandler(IKnowledgeRepository repository)
{
    public void Handle()
    {
        var allAssets = repository.GetAllSourceAssets();
        var books = allAssets.Where(a => a.DetectedRole == SourceAssetRole.Pdf).ToList();
        var others = allAssets.Where(a => a.DetectedRole == SourceAssetRole.AnswerKey || a.DetectedRole == SourceAssetRole.Transcript).ToList();

        foreach (var other in others)
        {
            // Heuristic 1: Same container
            var match = books.FirstOrDefault(b => b.ContainerId == other.ContainerId);
            if (match != null)
            {
                var relType = other.DetectedRole == SourceAssetRole.AnswerKey 
                    ? SourceAssetRelationType.ProvidesAnswerKeyFor 
                    : SourceAssetRelationType.ProvidesTranscriptFor;
                repository.UpsertSourceAssetLink(new SourceAssetLink(other.AssetId, match.AssetId, relType));
            }
        }
    }
}
