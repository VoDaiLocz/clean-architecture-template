using Application.Common.Models;
using Domain.Aggregates.Corpus;

namespace Application.Features.PartEngines;

public interface IToeicPartEngine
{
    bool SupportsPart(int part);
    ToeicPlayableItem CreatePlayableItem(PublishedQuestion question);
    ToeicReviewItem CreateReviewItem(PublishedQuestion question);
}
