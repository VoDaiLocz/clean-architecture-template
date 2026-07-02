using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceDiscovery;

public sealed record DiscoverDriveSourceAssetsCommand;

public sealed record DiscoverDriveSourceAssetsResult(
    int DiscoveredContainerCount,
    int DiscoveredAssetCount,
    int BlockedIssueCount
);

public interface IDriveDiscoveryGateway
{
    IReadOnlyList<DriveDiscoveredAsset> ListFolderAssets(SourceManifestEntry source);
}

public sealed record DriveDiscoveredAsset(
    string ExternalId,
    string FileName,
    string MimeType,
    string Extension,
    long SizeBytes,
    string ProviderUrl,
    string Checksum
);

public sealed class DiscoverDriveSourceAssetsHandler(
    IKnowledgeRepository repository,
    IDriveDiscoveryGateway gateway
)
{
    public DiscoverDriveSourceAssetsResult Handle(DiscoverDriveSourceAssetsCommand command)
    {
        var containers = 0;
        var assets = 0;
        var issues = 0;

        foreach (var source in repository.GetSourceManifestEntries().Where(IsDriveFolder))
        {
            if (source.AccessStatus == SourceAccessStatus.AccessBlocked)
            {
                repository.UpsertSourceDiscoveryIssue(CreateBlockedIssue(source));
                issues++;
                continue;
            }

            var container = CreateContainer(source);
            repository.UpsertSourceContainer(container);
            containers++;

            foreach (var discoveredAsset in gateway.ListFolderAssets(source))
            {
                repository.UpsertSourceAsset(CreateAsset(source, container, discoveredAsset));
                assets++;
            }
        }

        return new DiscoverDriveSourceAssetsResult(containers, assets, issues);
    }

    private static bool IsDriveFolder(SourceManifestEntry source) =>
        source.Provider == SourceProvider.GoogleDrive && source.SourceType == SourceType.DriveFolder;

    private static SourceContainer CreateContainer(SourceManifestEntry source) =>
        new(
            ContainerId: $"drive-folder-audit-source-{source.SheetRowNumber}",
            SourceId: source.SourceId,
            Provider: SourceProvider.GoogleDrive,
            ExternalId: $"audit-source-{source.SheetRowNumber}",
            Title: source.Title,
            AccessStatus: source.AccessStatus,
            DiscoveredAtUtc: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
        );

    private static SourceAsset CreateAsset(
        SourceManifestEntry source,
        SourceContainer container,
        DriveDiscoveredAsset discoveredAsset
    ) =>
        new(
            AssetId: $"asset-{source.SourceId}-{discoveredAsset.ExternalId}",
            ContainerId: container.ContainerId,
            SourceId: source.SourceId,
            FileName: discoveredAsset.FileName,
            MimeType: discoveredAsset.MimeType,
            Extension: discoveredAsset.Extension,
            SizeBytes: discoveredAsset.SizeBytes,
            DetectedRole: DetectRole(discoveredAsset),
            ProviderUrl: discoveredAsset.ProviderUrl,
            ObjectKey: $"source-assets/{source.SourceId}/{discoveredAsset.FileName}",
            Checksum: discoveredAsset.Checksum
        );

    private static SourceDiscoveryIssue CreateBlockedIssue(SourceManifestEntry source) =>
        new(
            IssueId: $"source-discovery-blocked-{source.SourceId}",
            SourceId: source.SourceId,
            IssueCode: "source_access_blocked",
            Message: $"Drive source is blocked and cannot be discovered: {source.Title}",
            Status: SourceDiscoveryIssueStatus.Open,
            CreatedAtUtc: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
        );

    private static SourceAssetRole DetectRole(DriveDiscoveredAsset asset)
    {
        var value = $"{asset.FileName} {asset.MimeType} {asset.Extension}".ToLowerInvariant();
        if (value.Contains("pdf", StringComparison.Ordinal)) return SourceAssetRole.Pdf;
        if (value.Contains("audio", StringComparison.Ordinal) || value.Contains(".mp3", StringComparison.Ordinal)) return SourceAssetRole.Audio;
        if (value.Contains("image", StringComparison.Ordinal) || value.Contains(".jpg", StringComparison.Ordinal) || value.Contains(".png", StringComparison.Ordinal)) return SourceAssetRole.Image;
        return SourceAssetRole.Unknown;
    }
}
