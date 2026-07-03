using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceManifests;

public sealed record RejectedFilesReport(
    int TotalRejectedFiles,
    IReadOnlyList<RejectedLocalSourceFile> Files
);

public sealed class GetRejectedFilesHandler(IKnowledgeRepository repository)
{
    public RejectedFilesReport Handle()
    {
        var files = repository.GetRejectedLocalSourceFiles();
        return new RejectedFilesReport(files.Count, files);
    }
}
