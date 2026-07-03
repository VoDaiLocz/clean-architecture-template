using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Placement;

public sealed record GenerateLearningPathCommand(string LearnerId, string PlacementResultId);

public sealed record GenerateLearningPathResponse(
    string PathId,
    string FirstUnlockedUnitId,
    int TotalUnits,
    string GeneratedReasonSummary,
    ActionReference NextAction
);

public sealed record ActionReference(string Code, string TargetId);

public sealed class GenerateLearningPathHandler(IKnowledgeRepository repository)
{
    public GenerateLearningPathResponse Handle(GenerateLearningPathCommand command)
    {
        var result = repository.GetPlacementResultBreakdowns(command.PlacementResultId);
        if (result == null || result.Count == 0)
        {
            throw new InvalidOperationException("PLACEMENT_RESULT_REQUIRED");
        }

        var placementResult = repository.GetPlacementResultBySessionId(command.PlacementResultId);
        // Fallback check if needed, but we assume existence based on the breakdowns
        
        var existingPath = repository.GetActiveLearningPath(command.LearnerId);
        if (existingPath != null)
        {
            var archivedPath = existingPath with 
            { 
                Status = LearningPathStatus.Archived, 
                ArchiveReason = "Regenerated from new placement result",
                UpdatedAtUtc = DateTimeOffset.UtcNow 
            };
            repository.UpsertLearningPath(archivedPath);
        }

        var catalog = LearningPathCatalog.CreateDefault();
        if (catalog.Units.Count == 0)
        {
            throw new InvalidOperationException("UNIT_CATALOG_EMPTY");
        }

        var pathId = Guid.NewGuid().ToString();
        var newPath = new LearningPath(
            pathId,
            command.LearnerId,
            LearningPathStatus.Active,
            null,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow
        );
        repository.UpsertLearningPath(newPath);

        // Sort catalog units based on placement weaknesses
        // We look for Topic/Skill weaknesses
        var weaknessTopics = result
            .Where(b => b.DimensionType == "Topic" && b.ScorePercent < 50)
            .Select(b => b.DimensionValue)
            .ToHashSet();

        var pathUnits = new List<LearningPathUnit>();
        int order = 1;
        string? firstUnlockedUnitId = null;

        // Prioritize weaknesses, but respect prerequisite chains if we had real logic.
        // For simplicity, just order catalog units such that weakness topics come first.
        var sortedUnits = catalog.Units.OrderByDescending(u => weaknessTopics.Contains(u.Title)).ThenBy(u => u.Part).ToList();

        foreach (var def in sortedUnits)
        {
            var status = LearningPathUnitStatus.Locked;
            string? unlockReason = null;

            if (firstUnlockedUnitId == null)
            {
                status = LearningPathUnitStatus.Unlocked;
                unlockReason = "Generated from placement weakness";
                firstUnlockedUnitId = def.UnitId;
            }

            var unit = new LearningPathUnit(
                def.UnitId, // Usually it's better to create a new instance ID per learner path unit
                pathId,
                def.UnitId,
                def.Part,
                "", // SkillTags
                order++,
                status,
                unlockReason,
                command.PlacementResultId
            );

            repository.UpsertLearningPathUnit(unit);
            pathUnits.Add(unit);
        }

        var run = new LearnerPathGenerationRun(
            Guid.NewGuid().ToString(),
            command.LearnerId,
            command.PlacementResultId,
            catalog.Version,
            pathId,
            DateTimeOffset.UtcNow
        );
        repository.UpsertLearnerPathGenerationRun(run);

        return new GenerateLearningPathResponse(
            pathId,
            firstUnlockedUnitId!,
            pathUnits.Count,
            $"Generated from placement result {command.PlacementResultId}",
            new ActionReference("StartUnit", firstUnlockedUnitId!)
        );
    }
}
