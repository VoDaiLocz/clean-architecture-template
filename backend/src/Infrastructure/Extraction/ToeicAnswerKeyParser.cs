using Application.Common.Interfaces.Repositories;
using Application.Features.SourceExtraction;
using Domain.Aggregates.Corpus;

namespace Infrastructure.Extraction;

public class ToeicAnswerKeyParser(IKnowledgeRepository repository, IEnumerable<IToeicParserProfile> profiles) : IAnswerKeyParser
{
    public IReadOnlyList<AnswerKeyMappingResult> Parse(SourceAsset asset)
    {
        var blocks = repository.GetExtractedTextBlocks(asset.AssetId);
        var applicableProfiles = profiles.Where(p => p.CanParse(asset)).ToList();
        if (!applicableProfiles.Any()) return [];
        
        var bestResult = applicableProfiles
            .Select(p => p.ParseAnswerKeys(blocks))
            .OrderByDescending(r => r.FirstOrDefault()?.Confidence ?? 0)
            .FirstOrDefault();
            
        return bestResult ?? [];
    }
}
