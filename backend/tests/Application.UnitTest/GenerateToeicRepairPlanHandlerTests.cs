using Application.Features.Learner.TestSessions;
using Domain.Aggregates.LearnerProgress;
using Domain.Aggregates.LearningItems;
using Domain.Aggregates.Corpus;
using Infrastructure.Data;

namespace Application.UnitTest;

public static class GenerateToeicRepairPlanHandlerTests
{
    public static async Task TestGenerateToeicRepairPlan_CreatesPlan()
    {
        var repository = SqliteKnowledgeRepository.InMemory();
        repository.Initialize();

        var learnerId = "learner_1";
        var sessionId = "session_1";

        repository.UpsertLearnerProfile(new LearnerProfile(learnerId, "Name", "email@test.com", 500, 0, 30, "UTC", LearnerProfileStatus.Active, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPath(new LearningPath("path1", learnerId, LearningPathStatus.Active, null, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow));
        repository.UpsertLearningPathUnit(new LearningPathUnit("u1", "path1", "key1", 5, "tag", 1, LearningPathUnitStatus.Unlocked, null, null));
        repository.UpsertPublishedLesson(new PublishedLesson("l1", "u1", 5, "title", "obj", "tags", "{}", PublishedContentStatus.Published));

        // Setup a published question
        repository.UpsertPublishedQuestion(new PublishedQuestion(
            QuestionId: "q1",
            LessonId: "l1",
            ToeicPart: 5,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: "Test Prompt",
            OptionsJson: "{}",
            CorrectAnswer: "A",
            Explanation: "Because",
            MediaAssetId: null,
            PassageId: null,
            GroupId: null,
            EvidenceJson: "{}",
            SkillTags: "[\"grammar\"]",
            SourceTraceJson: "{}",
            Status: PublishedContentStatus.Published
        ));

        // Setup a test session
        var session = new MiniTestSession(
            SessionId: sessionId,
            LearnerId: learnerId,
            UnitId: "u1",
            Status: MiniTestSessionStatus.Submitted,
            StartedAtUtc: DateTimeOffset.UtcNow,
            SubmittedAtUtc: DateTimeOffset.UtcNow,
            ExpiredAtUtc: DateTimeOffset.UtcNow.AddMinutes(30),
            AssignedQuestionIds: new List<string> { "q1" },
            Answers: new Dictionary<string, string> { { "q1", "B" } }, // incorrect
            ResultId: null
        );
        repository.UpsertMiniTestSession(session);

        // Setup some unseen questions for the drill
        repository.UpsertPublishedQuestion(new PublishedQuestion(
            QuestionId: "q2",
            LessonId: "l1",
            ToeicPart: 5,
            QuestionType: PublishedQuestionType.SingleQuestion,
            Prompt: "Drill Prompt",
            OptionsJson: "{}",
            CorrectAnswer: "C",
            Explanation: "Because",
            MediaAssetId: null,
            PassageId: null,
            GroupId: null,
            EvidenceJson: "{}",
            SkillTags: "[\"grammar\"]",
            SourceTraceJson: "{}",
            Status: PublishedContentStatus.Published
        ));

        var handler = new GenerateToeicRepairPlanHandler(repository);
        var plan = await handler.Handle(new GenerateToeicRepairPlanCommand(sessionId, learnerId));

        if (plan == null) throw new Exception("Plan is null");
        if (plan.Status != RepairPlanStatus.Generated) throw new Exception("Expected Generated status");
        if (plan.ReviewQuestionIds.Count != 1 || plan.ReviewQuestionIds[0] != "q1") throw new Exception("Expected q1 in review");
        if (plan.DrillQuestionIds.Count != 1 || plan.DrillQuestionIds[0] != "q2") throw new Exception("Expected q2 in drill");
    }
}
