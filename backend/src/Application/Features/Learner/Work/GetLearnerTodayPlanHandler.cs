using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Work;

public sealed record GetLearnerTodayPlanQuery(string LearnerId);

public sealed record LearnerTodayPlanResponse(
    LearnerAssignmentResponse? PrimaryAssignment,
    IReadOnlyList<string> Blockers,
    LearnerPathProgressResponse PathProgress,
    int ReviewCount
);

public sealed record LearnerAssignmentResponse(
    string AssignmentId,
    string UnitId,
    string ActivityType,
    string Status
);

public sealed record LearnerPathProgressResponse(
    int TotalUnits,
    int CompletedUnits,
    string? CurrentUnitId
);

public sealed class GetLearnerTodayPlanHandler(IKnowledgeRepository repository)
{
    public LearnerTodayPlanResponse Handle(GetLearnerTodayPlanQuery query)
    {
        var profile = repository.GetLearnerProfile(query.LearnerId);
        if (profile is null)
        {
            throw new ArgumentException("Learner profile not found.");
        }

        var path = repository.GetActiveLearningPath(query.LearnerId);
        if (path is null)
        {
            throw new ArgumentException("Active learning path not found.");
        }

        var pathUnits = repository.GetLearningPathUnits(path.PathId);
        var completedUnits = pathUnits.Count(u => u.Status == LearningPathUnitStatus.Completed);
        
        var currentUnit = pathUnits
            .Where(u => u.Status == LearningPathUnitStatus.Unlocked)
            .OrderBy(u => u.DisplayOrder)
            .FirstOrDefault();

        var pathProgress = new LearnerPathProgressResponse(
            pathUnits.Count,
            completedUnits,
            currentUnit?.UnitId
        );

        var reviews = repository.GetReviewItems(query.LearnerId);
        var openReviews = reviews.Count(r => r.Status == ReviewItemStatus.Open);

        // 1. Check for active assignments
        var activeAssignments = repository.GetLearnerAssignments(query.LearnerId)
            .Where(a => a.Status == LearnerAssignmentStatus.Assigned || a.Status == LearnerAssignmentStatus.Started)
            .ToList();

        if (activeAssignments.Any())
        {
            var primary = activeAssignments.First();
            return new LearnerTodayPlanResponse(
                new LearnerAssignmentResponse(
                    primary.AssignmentId,
                    primary.ContentRefId,
                    primary.AssignmentType.ToString(),
                    primary.Status.ToString()
                ),
                [],
                pathProgress,
                openReviews
            );
        }

        // 2. Generate new assignment if unit is available
        if (currentUnit != null)
        {
            var assignmentId = Guid.NewGuid().ToString();
            var assignment = new LearnerAssignment(
                assignmentId,
                query.LearnerId,
                LearnerAssignmentType.Lesson,
                currentUnit.UnitId,
                LearnerAssignmentStatus.Assigned,
                DateTimeOffset.UtcNow,
                null
            );
            
            repository.UpsertLearnerAssignment(assignment);

            return new LearnerTodayPlanResponse(
                new LearnerAssignmentResponse(
                    assignment.AssignmentId,
                    assignment.ContentRefId,
                    assignment.AssignmentType.ToString(),
                    assignment.Status.ToString()
                ),
                [],
                pathProgress,
                openReviews
            );
        }

        return new LearnerTodayPlanResponse(
            null,
            ["ContentUnavailable"],
            pathProgress,
            openReviews
        );
    }
}
