using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceManifests;

public sealed class GetSourceManifestSummaryHandler(IKnowledgeRepository repository)
{
    public SourceManifestSummary Handle()
    {
        return repository.GetSourceManifestSummary();
    }
}
