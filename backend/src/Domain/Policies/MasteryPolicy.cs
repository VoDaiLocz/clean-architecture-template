namespace Domain.Policies;

using Domain.Aggregates.LearnerProgress;

public static class MasteryPolicy
{
    public static bool IsPassed(LearnerAssignmentType type, int scorePercent)
    {
        return type switch
        {
            LearnerAssignmentType.Lesson => true, // Lessons don't have scores, just completion
            LearnerAssignmentType.Drill => scorePercent >= 80,
            LearnerAssignmentType.MiniTest => scorePercent >= 80,
            LearnerAssignmentType.PartTest => scorePercent >= 80,
            LearnerAssignmentType.FullTest => scorePercent >= 80,
            _ => false
        };
    }
}
