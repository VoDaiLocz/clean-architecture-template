namespace Domain.Aggregates.Corpus;

public sealed record CorpusManifest(
    string CorpusId,
    string Title,
    int SheetTabs,
    int SheetRows,
    int PdfBooks,
    int PdfPages,
    int AudioFiles,
    int TargetLearningItems
);

public sealed record NormalizationStageSnapshot(
    string StageKey,
    string DisplayName,
    int TotalCount,
    int CompletedCount,
    int RejectedCount
)
{
    public int RemainingCount => Math.Max(0, TotalCount - CompletedCount - RejectedCount);
}
