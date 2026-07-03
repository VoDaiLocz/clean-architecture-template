using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Mastery;

public sealed class MasteryCalculationService(IKnowledgeRepository repository)
{
    public void RecalculateMastery(string learnerId, string unitId)
    {
        var path = repository.GetActiveLearningPath(learnerId);
        if (path == null) return;

        var pathUnits = repository.GetLearningPathUnits(path.PathId);
        var targetUnit = pathUnits.FirstOrDefault(u => u.UnitId == unitId);
        if (targetUnit == null) return;

        var assignments = repository.GetLearnerAssignments(learnerId).Where(a => a.ContentRefId == unitId).ToList();
        
        var lesson = assignments.FirstOrDefault(a => a.AssignmentType == LearnerAssignmentType.Lesson);
        var drill = assignments.FirstOrDefault(a => a.AssignmentType == LearnerAssignmentType.Drill);
        var minitest = assignments.FirstOrDefault(a => a.AssignmentType == LearnerAssignmentType.MiniTest);

        bool IsAssignmentPassed(LearnerAssignment? assignment)
        {
            if (assignment == null) return false;
            if (assignment.AssignmentType == LearnerAssignmentType.Lesson) 
                return assignment.Status == LearnerAssignmentStatus.Completed;
                
            if (assignment.Status != LearnerAssignmentStatus.Completed) 
                return false;

            var sessions = repository.GetActivitySessions(assignment.AssignmentId);
            var attempts = sessions.SelectMany(s => repository.GetLearnerAttempts(s.SessionId));
            var bestAttempt = attempts.OrderByDescending(a => a.ScorePercent).FirstOrDefault();
            
            var score = bestAttempt?.ScorePercent ?? 0;
            return Domain.Policies.MasteryPolicy.IsPassed(assignment.AssignmentType, score);
        }

        var lessonComplete = IsAssignmentPassed(lesson);
        var drillComplete = IsAssignmentPassed(drill);
        var minitestComplete = IsAssignmentPassed(minitest);

        var reviewItems = repository.GetReviewItems(learnerId)
            .Where(r => r.UnitId == unitId && r.Status == ReviewItemStatus.Open && r.IsBlocking)
            .ToList();

        var blockingReviewCount = reviewItems.Count;
        
        // Calculate blockers for the unit
        var blockers = new List<UnlockBlocker>();

        // Check prerequisite
        var prevUnit = pathUnits
            .Where(u => u.DisplayOrder < targetUnit.DisplayOrder)
            .OrderByDescending(u => u.DisplayOrder)
            .FirstOrDefault();

        if (prevUnit != null && prevUnit.Status != LearningPathUnitStatus.Completed)
        {
            blockers.Add(new UnlockBlocker(Guid.NewGuid().ToString(), learnerId, unitId, "PREREQUISITE_NOT_COMPLETED", DateTimeOffset.UtcNow));
        }

        if (!lessonComplete)
        {
            blockers.Add(new UnlockBlocker(Guid.NewGuid().ToString(), learnerId, unitId, "LESSON_NOT_COMPLETED", DateTimeOffset.UtcNow));
        }

        if (!drillComplete)
        {
            blockers.Add(new UnlockBlocker(Guid.NewGuid().ToString(), learnerId, unitId, "DRILL_NOT_COMPLETED", DateTimeOffset.UtcNow));
        }

        if (!minitestComplete)
        {
            blockers.Add(new UnlockBlocker(Guid.NewGuid().ToString(), learnerId, unitId, "MINI_TEST_NOT_PASSED", DateTimeOffset.UtcNow));
        }

        if (blockingReviewCount > 0)
        {
            blockers.Add(new UnlockBlocker(Guid.NewGuid().ToString(), learnerId, unitId, "REVIEWS_PENDING", DateTimeOffset.UtcNow));
        }

        var isUnlocked = prevUnit == null || prevUnit.Status == LearningPathUnitStatus.Completed;

        var masteryPercent = 0;
        if (lessonComplete) masteryPercent += 25;
        if (drillComplete) masteryPercent += 25;
        if (minitestComplete) masteryPercent += 50;

        var record = new MasteryRecord(
            Guid.NewGuid().ToString(),
            learnerId,
            unitId,
            masteryPercent,
            isUnlocked,
            blockingReviewCount,
            DateTimeOffset.UtcNow
        );

        var existing = repository.GetMasteryRecord(learnerId, unitId);
        if (existing != null)
        {
            record = record with { MasteryRecordId = existing.MasteryRecordId };
        }

        repository.UpsertMasteryRecord(record);

        repository.DeleteUnlockBlockers(learnerId, unitId);
        foreach (var blocker in blockers)
        {
            repository.UpsertUnlockBlocker(blocker);
        }

        if (isUnlocked && targetUnit.Status == LearningPathUnitStatus.Locked)
        {
            targetUnit = targetUnit with { Status = LearningPathUnitStatus.Unlocked };
            repository.UpsertLearningPathUnit(targetUnit);
        }

        // Update learning path unit status if fully complete
        var isUnitCompleted = lessonComplete && drillComplete && minitestComplete && blockingReviewCount == 0;
        if (isUnitCompleted && targetUnit.Status != LearningPathUnitStatus.Completed)
        {
            var updatedUnit = targetUnit with { Status = LearningPathUnitStatus.Completed };
            repository.UpsertLearningPathUnit(updatedUnit);
            
            // Recalculate next units since they might be unlocked now
            var nextUnits = pathUnits.Where(u => u.DisplayOrder > targetUnit.DisplayOrder).ToList();
            foreach (var nextUnit in nextUnits)
            {
                RecalculateMastery(learnerId, nextUnit.UnitId);
            }
        }
    }

    public bool CanUnlockUnit(string learnerId, string unitId)
    {
        var existing = repository.GetMasteryRecord(learnerId, unitId);
        return existing?.IsUnlocked == true;
    }
}
