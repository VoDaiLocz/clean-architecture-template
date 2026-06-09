using Application.Common.Interfaces.Repositories;

namespace Application.Features.Dashboard.Queries;

public sealed record DashboardResponse(
    int RawSourceCount,
    int LearningItemCount,
    int ValidationIssueCount
);

public sealed class GetDashboardHandler(IKnowledgeRepository repository)
{
    public DashboardResponse Handle()
    {
        return new DashboardResponse(
            repository.Count("raw_sources"),
            repository.Count("learning_items"),
            repository.Count("validation_issues")
        );
    }
}
