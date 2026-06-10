using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.Dashboard.Queries;

public sealed record DashboardResponse(
    int RawSourceCount,
    int LearningItemCount,
    int ValidationIssueCount,
    CorpusManifest Corpus,
    IReadOnlyList<NormalizationStageSnapshot> NormalizationStages,
    SourceManifestSummary SourceManifest
);

public sealed class GetDashboardHandler(IKnowledgeRepository repository)
{
    public DashboardResponse Handle()
    {
        return new DashboardResponse(
            repository.Count("raw_sources"),
            repository.Count("learning_items"),
            repository.Count("validation_issues"),
            repository.GetCorpusManifest(),
            repository.GetNormalizationStages(),
            repository.GetSourceManifestSummary()
        );
    }
}
