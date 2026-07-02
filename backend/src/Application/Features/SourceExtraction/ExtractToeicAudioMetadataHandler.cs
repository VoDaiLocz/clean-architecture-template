using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceExtraction;

public sealed record ExtractToeicAudioMetadataCommand(string AssetId);

public sealed record ExtractToeicAudioMetadataResult(int ExtractedAudioMetadataCount);

public interface IAudioMetadataProbe
{
    AudioMetadataProbeResult Probe(SourceAsset asset);
}

public sealed record AudioMetadataProbeResult(
    int DurationSeconds,
    string Format,
    int SampleRateHz,
    int BitrateKbps
);

public sealed class ExtractToeicAudioMetadataHandler(
    IKnowledgeRepository repository,
    IAudioMetadataProbe probe
)
{
    public ExtractToeicAudioMetadataResult Handle(ExtractToeicAudioMetadataCommand command)
    {
        var asset = repository.GetSourceAsset(command.AssetId)
            ?? throw new InvalidOperationException($"Source asset not found: {command.AssetId}");

        if (asset.DetectedRole != SourceAssetRole.Audio)
        {
            throw new InvalidOperationException("Only audio source assets can be probed by this handler.");
        }

        var result = probe.Probe(asset);
        repository.UpsertSourceAudioMetadata(new SourceAudioMetadata(
            AudioMetadataId: $"audio-metadata-{asset.AssetId}",
            AssetId: asset.AssetId,
            DurationSeconds: result.DurationSeconds,
            Format: result.Format,
            SampleRateHz: result.SampleRateHz,
            BitrateKbps: result.BitrateKbps,
            ExtractedAtUtc: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
        ));

        return new ExtractToeicAudioMetadataResult(1);
    }
}
