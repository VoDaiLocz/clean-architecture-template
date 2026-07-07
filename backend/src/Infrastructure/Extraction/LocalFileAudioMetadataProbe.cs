using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public sealed class LocalFileAudioMetadataProbe(string downloadsRootPath) : IAudioMetadataProbe
{
    private readonly string downloadsRootPath = Path.GetFullPath(downloadsRootPath);

    public AudioMetadataProbeResult Probe(SourceAsset asset)
    {
        var path = ResolveLocalPath(asset);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Local audio file was not found for asset: {asset.AssetId}", path);
        }

        using var file = TagLib.File.Create(path);
        var properties = file.Properties
            ?? throw new InvalidOperationException($"Could not read audio properties for asset: {asset.AssetId}");

        return new AudioMetadataProbeResult(
            DurationSeconds: (int)Math.Round(properties.Duration.TotalSeconds),
            Format: properties.Description ?? asset.Extension,
            SampleRateHz: properties.AudioSampleRate,
            BitrateKbps: properties.AudioBitrate
        );
    }

    private string ResolveLocalPath(SourceAsset asset)
    {
        const string localDownloadsPrefix = "local-downloads/";
        if (asset.ObjectKey.StartsWith(localDownloadsPrefix, StringComparison.Ordinal))
        {
            var relativePath = asset.ObjectKey[localDownloadsPrefix.Length..]
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(downloadsRootPath, relativePath));
        }

        if (asset.ProviderUrl.StartsWith("file://downloads/", StringComparison.Ordinal))
        {
            var relativePath = asset.ProviderUrl["file://downloads/".Length..]
                .Replace('/', Path.DirectorySeparatorChar);
            return Path.GetFullPath(Path.Combine(downloadsRootPath, relativePath));
        }

        throw new InvalidOperationException($"Asset is not a local downloads audio object: {asset.AssetId}");
    }
}
