using Application.Common.Interfaces.Storage;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public sealed class TagLibAudioMetadataProbe(IObjectStorage objectStorage) : IAudioMetadataProbe
{
    public AudioMetadataProbeResult Probe(SourceAsset asset)
    {
        var storedObject = objectStorage.Get(new ObjectKey(asset.ObjectKey))
            ?? throw new InvalidOperationException($"Object not found in storage: {asset.ObjectKey}");

        var fileAbstraction = new ByteArrayFileAbstraction(asset.FileName, storedObject.Content);
        using var file = TagLib.File.Create(fileAbstraction);
        
        var properties = file.Properties;
        if (properties == null)
        {
            throw new InvalidOperationException($"Could not read audio properties for asset: {asset.AssetId}");
        }

        return new AudioMetadataProbeResult(
            DurationSeconds: (int)Math.Round(properties.Duration.TotalSeconds),
            Format: properties.Description ?? asset.Extension,
            SampleRateHz: properties.AudioSampleRate,
            BitrateKbps: properties.AudioBitrate
        );
    }

    private class ByteArrayFileAbstraction : TagLib.File.IFileAbstraction
    {
        private readonly string _name;
        private readonly MemoryStream _stream;

        public ByteArrayFileAbstraction(string name, byte[] bytes)
        {
            _name = name;
            _stream = new MemoryStream(bytes);
        }

        public string Name => _name;

        public Stream ReadStream => _stream;

        public Stream WriteStream => throw new NotSupportedException();

        public void CloseStream(Stream stream)
        {
            // Do not close stream, let GC handle MemoryStream
        }
    }
}
