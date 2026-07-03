using Application.Common.Interfaces.Storage;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public sealed class CsvListeningDraftParser : IListeningDraftParser
{
    public IReadOnlyList<ListeningDraftQuestionResult> Parse(SourceAsset asset)
    {
        return Array.Empty<ListeningDraftQuestionResult>();
    }
}
