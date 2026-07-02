using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceDiscovery;

public sealed record ResolveToeicExternalSourcesCommand;

public sealed record ResolveToeicExternalSourcesResult(int ResolvedCount, int FailedCount);

public interface IExternalSourceResolver
{
    ExternalSourceResolutionResult Resolve(string url);
}

public sealed record ExternalSourceResolutionResult(
    string OriginalUrl,
    string ResolvedUrl,
    int HttpStatusCode,
    int RedirectCount
);

public sealed class ResolveToeicExternalSourcesHandler(
    IKnowledgeRepository repository,
    IExternalSourceResolver resolver
)
{
    public ResolveToeicExternalSourcesResult Handle(ResolveToeicExternalSourcesCommand command)
    {
        var resolved = 0;
        var failed = 0;

        foreach (var source in repository.GetSourceManifestEntries().Where(ShouldResolve))
        {
            var result = resolver.Resolve(source.Url);
            var status = result.HttpStatusCode is >= 200 and < 400
                ? SourceResolutionStatus.Resolved
                : SourceResolutionStatus.Failed;

            repository.UpsertSourceResolutionRecord(new SourceResolutionRecord(
                ResolutionId: $"source-resolution-{source.SourceId}",
                SourceId: source.SourceId,
                OriginalUrl: result.OriginalUrl,
                ResolvedUrl: result.ResolvedUrl,
                HttpStatusCode: result.HttpStatusCode,
                RedirectCount: result.RedirectCount,
                Status: status,
                ResolvedAtUtc: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
            ));

            if (status == SourceResolutionStatus.Resolved) resolved++;
            else failed++;
        }

        return new ResolveToeicExternalSourcesResult(resolved, failed);
    }

    private static bool ShouldResolve(SourceManifestEntry source) =>
        source.AccessStatus == SourceAccessStatus.Accessible
        && source.SourceType is SourceType.Shortlink or SourceType.ExternalWeb or SourceType.SharePoint;
}
