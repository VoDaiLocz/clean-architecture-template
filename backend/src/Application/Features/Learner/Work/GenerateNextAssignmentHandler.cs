using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Work;

public sealed record GenerateNextAssignmentCommand(string LearnerId);

public sealed class GenerateNextAssignmentHandler(IKnowledgeRepository repository)
{
    public LearnerTodayPlanResponse Handle(GenerateNextAssignmentCommand command)
    {
        // 1. Fetch current unit
        var path = repository.GetActiveLearningPath(command.LearnerId) ?? throw new ArgumentException("Path not found");
        var pathUnits = repository.GetLearningPathUnits(path.PathId);
        var currentUnit = pathUnits.Where(u => u.Status == LearningPathUnitStatus.Unlocked).OrderBy(u => u.DisplayOrder).FirstOrDefault() ?? throw new InvalidOperationException("No unlocked units available");
        
        // 2. Evaluate blockers
        var blockers = repository.GetUnlockBlockers(command.LearnerId, currentUnit.UnitId).Select(b => b.Reason).ToList();
        if (blockers.Contains("PREREQUISITE_NOT_COMPLETED")) throw new InvalidOperationException("Prerequisite not completed");
        
        // 3. Determine nextActivity
        LearnerAssignmentType nextActivity = LearnerAssignmentType.Lesson;
        if (blockers.Contains("REVIEWS_PENDING")) nextActivity = LearnerAssignmentType.Review;
        else if (blockers.Contains("LESSON_NOT_COMPLETED")) nextActivity = LearnerAssignmentType.Lesson;
        else if (blockers.Contains("DRILL_NOT_COMPLETED")) nextActivity = LearnerAssignmentType.Drill;
        else if (blockers.Contains("MINI_TEST_NOT_PASSED")) nextActivity = LearnerAssignmentType.MiniTest;
        
        // 4. UpsertLearnerAssignment
        var assignmentId = Guid.NewGuid().ToString();
        var assignment = new LearnerAssignment(assignmentId, command.LearnerId, nextActivity, currentUnit.UnitId, LearnerAssignmentStatus.Assigned, DateTimeOffset.UtcNow, null);
        repository.UpsertLearnerAssignment(assignment);
        
        // 5. Return response via GET handler
        return new GetLearnerTodayPlanHandler(repository).Handle(new GetLearnerTodayPlanQuery(command.LearnerId));
    }
}
