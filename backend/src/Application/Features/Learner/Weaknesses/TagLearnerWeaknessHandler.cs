using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.Learner;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Application.Features.Learner.Weaknesses;

public record TagLearnerWeaknessCommand(
    string LearnerId,
    string SourceActivityId,
    int ToeicPart,
    string SkillTag,
    bool IsCorrect
);

public sealed class TagLearnerWeaknessHandler
{
    private readonly IKnowledgeRepository repository;

    public TagLearnerWeaknessHandler(IKnowledgeRepository repository)
    {
        this.repository = repository;
    }

    public IReadOnlyList<LearnerWeaknessSummary> Handle(TagLearnerWeaknessCommand command)
    {
        var now = DateTimeOffset.UtcNow;
        var eventId = $"weakness-{command.LearnerId}-{command.SourceActivityId}-{command.SkillTag}";
        var weight = command.IsCorrect ? -0.5m : 1.0m;

        var @event = new LearnerWeaknessEvent(
            eventId,
            command.LearnerId,
            command.SourceActivityId,
            command.ToeicPart,
            command.SkillTag,
            weight,
            command.IsCorrect,
            now
        );

        var inserted = repository.UpsertWeaknessEvent(@event);
        
        var summaries = repository.GetWeaknessSummaries(command.LearnerId).ToList();

        if (inserted)
        {
            var summary = summaries.FirstOrDefault(s => s.ToeicPart == command.ToeicPart && s.SkillTag == command.SkillTag);
            
            decimal newScore = (summary?.SeverityScore ?? 0m) + weight;
            if (newScore < 0)
            {
                newScore = 0;
            }

            int newCount = (summary?.EvidenceCount ?? 0) + 1;

            var newSummary = new LearnerWeaknessSummary(
                command.LearnerId,
                command.ToeicPart,
                command.SkillTag,
                newScore,
                newCount,
                now
            );

            repository.UpsertWeaknessSummary(newSummary);
            
            if (summary != null)
            {
                summaries.Remove(summary);
            }
            summaries.Add(newSummary);
        }

        return summaries.OrderByDescending(s => s.SeverityScore).ToList();
    }
}
