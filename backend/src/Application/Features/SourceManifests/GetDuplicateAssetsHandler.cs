using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceManifests;

public sealed class GetDuplicateAssetsHandler(IKnowledgeRepository repository)
{
    public DuplicateAssetReport Handle()
    {
        return repository.GetDuplicateAssets();
    }
}
