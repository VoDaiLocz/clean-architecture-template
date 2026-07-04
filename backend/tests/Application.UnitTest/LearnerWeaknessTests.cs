using Application.Features.Learner.Weaknesses;
using Infrastructure.Data;
using System;
using System.Linq;

namespace Application.UnitTest;

public static class LearnerWeaknessTests
{
    public static void AttemptEventCreatesWeaknessSummary()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        
        // Ensure Learner exists due to FOREIGN KEY constraints
        repository.UpsertLearnerProfile(new Domain.Aggregates.LearnerProgress.LearnerProfile("learner-1", "Learner", "test@test.com", 500, 0, 30, "UTC", Domain.Aggregates.LearnerProgress.LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var handler = new TagLearnerWeaknessHandler(repository);

        var result = handler.Handle(new TagLearnerWeaknessCommand("learner-1", "activity-1", 5, "word_form", false));

        if (result.Count != 1) throw new Exception("Expected 1 summary");
        var summary = result.First();
        if (summary.SeverityScore != 1.0m) throw new Exception($"Expected SeverityScore 1.0, got {summary.SeverityScore}");
        if (summary.EvidenceCount != 1) throw new Exception($"Expected EvidenceCount 1, got {summary.EvidenceCount}");
    }

    public static void DuplicateEventIsIdempotent()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        repository.UpsertLearnerProfile(new Domain.Aggregates.LearnerProgress.LearnerProfile("learner-1", "Learner", "test@test.com", 500, 0, 30, "UTC", Domain.Aggregates.LearnerProgress.LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var handler = new TagLearnerWeaknessHandler(repository);

        var cmd = new TagLearnerWeaknessCommand("learner-1", "activity-1", 5, "word_form", false);
        handler.Handle(cmd);
        var result = handler.Handle(cmd); // Duplicate

        if (result.Count != 1) throw new Exception("Expected 1 summary");
        var summary = result.First();
        if (summary.SeverityScore != 1.0m) throw new Exception($"Expected SeverityScore 1.0, got {summary.SeverityScore}");
        if (summary.EvidenceCount != 1) throw new Exception($"Expected EvidenceCount 1, got {summary.EvidenceCount}");
    }

    public static void RepairReducesSeverity()
    {
        using var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();
        repository.UpsertLearnerProfile(new Domain.Aggregates.LearnerProgress.LearnerProfile("learner-1", "Learner", "test@test.com", 500, 0, 30, "UTC", Domain.Aggregates.LearnerProgress.LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));

        var handler = new TagLearnerWeaknessHandler(repository);

        handler.Handle(new TagLearnerWeaknessCommand("learner-1", "activity-1", 5, "word_form", false)); // +1.0
        var result = handler.Handle(new TagLearnerWeaknessCommand("learner-1", "activity-2", 5, "word_form", true)); // -0.5

        if (result.Count != 1) throw new Exception("Expected 1 summary");
        var summary = result.First();
        if (summary.SeverityScore != 0.5m) throw new Exception($"Expected SeverityScore 0.5, got {summary.SeverityScore}");
        if (summary.EvidenceCount != 2) throw new Exception($"Expected EvidenceCount 2, got {summary.EvidenceCount}");
        
        // Another correct repair reduces to 0, not below
        var finalResult = handler.Handle(new TagLearnerWeaknessCommand("learner-1", "activity-3", 5, "word_form", true));
        if (finalResult.First().SeverityScore != 0m) throw new Exception($"Expected SeverityScore 0.0, got {finalResult.First().SeverityScore}");
    }
}
