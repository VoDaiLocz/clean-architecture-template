using Domain.Aggregates.Corpus;
namespace Application.Features.SourceExtraction;

public interface IToeicParserProfile
{
    bool CanParse(SourceAsset asset);
    IReadOnlyList<AnswerKeyMappingResult> ParseAnswerKeys(IReadOnlyList<ExtractedTextBlock> blocks);
}
