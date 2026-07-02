using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceDiscovery;

public sealed record RegisterToeicSourceAssetsCommand;

public sealed record RegisterToeicSourceAssetsResult(
    int RegisteredContainerCount,
    int RegisteredAssetCount,
    int SkippedBlockedSourceCount
);

public sealed class RegisterToeicSourceAssetsHandler(IKnowledgeRepository repository)
{
    public RegisterToeicSourceAssetsResult Handle(RegisterToeicSourceAssetsCommand command)
    {
        var containers = 0;
        var assets = 0;
        var skipped = 0;

        foreach (var source in repository.GetSourceManifestEntries().Where(HasRegisterableEvidence))
        {
            if (source.AccessStatus == SourceAccessStatus.AccessBlocked)
            {
                skipped++;
                continue;
            }

            var container = CreateContainer(source);
            repository.UpsertSourceContainer(container);
            containers++;

            foreach (var role in EvidenceRoles(source))
            {
                repository.UpsertSourceAsset(CreateAsset(source, container, role));
                assets++;
            }
        }

        return new RegisterToeicSourceAssetsResult(containers, assets, skipped);
    }

    private static bool HasRegisterableEvidence(SourceManifestEntry source) =>
        source.Evidence.HasPdf || source.Evidence.HasAudio || source.Evidence.HasImage;

    private static IEnumerable<SourceAssetRole> EvidenceRoles(SourceManifestEntry source)
    {
        if (source.Evidence.HasPdf) yield return SourceAssetRole.Pdf;
        if (source.Evidence.HasAudio) yield return SourceAssetRole.Audio;
        if (source.Evidence.HasImage) yield return SourceAssetRole.Image;
    }

    private static SourceContainer CreateContainer(SourceManifestEntry source) =>
        new(
            ContainerId: $"registered-source-{source.SourceId}",
            SourceId: source.SourceId,
            Provider: source.Provider,
            ExternalId: source.SourceId,
            Title: source.Title,
            AccessStatus: source.AccessStatus,
            DiscoveredAtUtc: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
        );

    private static SourceAsset CreateAsset(
        SourceManifestEntry source,
        SourceContainer container,
        SourceAssetRole role
    ) =>
        new(
            AssetId: $"registered-asset-{source.SourceId}-{role.ToString().ToLowerInvariant()}",
            ContainerId: container.ContainerId,
            SourceId: source.SourceId,
            FileName: $"{source.SourceId}-{role.ToString().ToLowerInvariant()}{ExtensionFor(role)}",
            MimeType: MimeTypeFor(role),
            Extension: ExtensionFor(role),
            SizeBytes: 0,
            DetectedRole: role,
            ProviderUrl: source.Url,
            ObjectKey: $"source-assets/{source.SourceId}/{role.ToString().ToLowerInvariant()}",
            Checksum: "pending-registration"
        );

    private static string ExtensionFor(SourceAssetRole role) =>
        role switch
        {
            SourceAssetRole.Pdf => ".pdf",
            SourceAssetRole.Audio => ".mp3",
            SourceAssetRole.Image => ".jpg",
            _ => ".bin",
        };

    private static string MimeTypeFor(SourceAssetRole role) =>
        role switch
        {
            SourceAssetRole.Pdf => "application/pdf",
            SourceAssetRole.Audio => "audio/mpeg",
            SourceAssetRole.Image => "image/jpeg",
            _ => "application/octet-stream",
        };
}
