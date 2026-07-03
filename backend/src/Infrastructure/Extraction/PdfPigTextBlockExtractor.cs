using Application.Common.Interfaces.Storage;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;
using System.Text.Json;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;

namespace Infrastructure.Extraction;

public sealed class PdfPigTextBlockExtractor(IObjectStorage objectStorage) : IPdfTextBlockExtractor
{
    public IReadOnlyList<PdfExtractedPageResult> Extract(SourceAsset asset)
    {
        var storedObject = objectStorage.Get(new ObjectKey(asset.ObjectKey))
            ?? throw new InvalidOperationException($"Object not found in storage: {asset.ObjectKey}");

        using var document = PdfDocument.Open(storedObject.Content);
        var results = new List<PdfExtractedPageResult>();

        foreach (var page in document.GetPages())
        {
            var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(page.GetWords());
            var extractedBlocks = new List<PdfExtractedTextBlockResult>();

            foreach (var block in blocks)
            {
                var coords = new { x = block.BoundingBox.Left, y = block.BoundingBox.Bottom, w = block.BoundingBox.Width, h = block.BoundingBox.Height };
                var coordsJson = JsonSerializer.Serialize(coords);

                extractedBlocks.Add(new PdfExtractedTextBlockResult(
                    BlockType: ExtractedBlockType.Unknown, // Real parsing adapters can categorize later
                    Text: block.Text,
                    Confidence: 1.0m,
                    CoordinatesJson: coordsJson
                ));
            }

            results.Add(new PdfExtractedPageResult(
                PageNumber: page.Number,
                Width: (int)page.Width,
                Height: (int)page.Height,
                Blocks: extractedBlocks
            ));
        }

        return results;
    }
}
