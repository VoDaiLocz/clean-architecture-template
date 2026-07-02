using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Corpus;

namespace Application.Features.SourceExtraction;

public sealed record ExtractToeicPdfBlocksCommand(string AssetId);

public sealed record ExtractToeicPdfBlocksResult(int ExtractedPageCount, int ExtractedBlockCount);

public interface IPdfTextBlockExtractor
{
    IReadOnlyList<PdfExtractedPageResult> Extract(SourceAsset asset);
}

public sealed record PdfExtractedPageResult(
    int PageNumber,
    int Width,
    int Height,
    IReadOnlyList<PdfExtractedTextBlockResult> Blocks
);

public sealed record PdfExtractedTextBlockResult(
    ExtractedBlockType BlockType,
    string Text,
    decimal Confidence,
    string CoordinatesJson
);

public sealed class ExtractToeicPdfBlocksHandler(
    IKnowledgeRepository repository,
    IPdfTextBlockExtractor extractor
)
{
    public ExtractToeicPdfBlocksResult Handle(ExtractToeicPdfBlocksCommand command)
    {
        var asset = repository.GetSourceAsset(command.AssetId)
            ?? throw new InvalidOperationException($"Source asset not found: {command.AssetId}");

        if (asset.DetectedRole != SourceAssetRole.Pdf)
        {
            throw new InvalidOperationException("Only PDF source assets can be extracted by this handler.");
        }

        var pageCount = 0;
        var blockCount = 0;
        foreach (var pageResult in extractor.Extract(asset))
        {
            var pageId = $"extracted-page-{asset.AssetId}-{pageResult.PageNumber}";
            repository.UpsertExtractedPage(new ExtractedPage(
                PageId: pageId,
                AssetId: asset.AssetId,
                PageNumber: pageResult.PageNumber,
                Width: pageResult.Width,
                Height: pageResult.Height,
                ExtractedAtUtc: new DateTimeOffset(2026, 7, 2, 0, 0, 0, TimeSpan.Zero)
            ));
            pageCount++;

            var displayOrder = 0;
            foreach (var block in pageResult.Blocks)
            {
                displayOrder++;
                repository.UpsertExtractedTextBlock(new ExtractedTextBlock(
                    BlockId: $"extracted-block-{asset.AssetId}-{pageResult.PageNumber}-{displayOrder}",
                    AssetId: asset.AssetId,
                    PageId: pageId,
                    PageNumber: pageResult.PageNumber,
                    BlockType: block.BlockType,
                    Text: block.Text,
                    Confidence: block.Confidence,
                    CoordinatesJson: block.CoordinatesJson
                ));
                blockCount++;
            }
        }

        return new ExtractToeicPdfBlocksResult(pageCount, blockCount);
    }
}
