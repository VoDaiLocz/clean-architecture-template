using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Work;

public sealed record SubmitAttemptCommand(
    string SessionId,
    string LearnerId,
    Dictionary<string, string> Answers
);

public sealed record SubmitAttemptResponse(
    string AttemptId,
    int ScorePercent,
    int CorrectCount,
    int TotalCount,
    string Status
);

public sealed class SubmitAttemptHandler(IKnowledgeRepository repository)
{
    public SubmitAttemptResponse Handle(SubmitAttemptCommand command)
    {
        var session = repository.GetActivitySession(command.SessionId);
        if (session is null)
        {
            throw new ArgumentException("SESSION_NOT_FOUND");
        }

        if (session.LearnerId != command.LearnerId)
        {
            throw new ArgumentException("SESSION_NOT_OWNED");
        }

        if (session.Status != ActivitySessionStatus.InProgress)
        {
            throw new ArgumentException("SESSION_NOT_ACTIVE");
        }

        var existingAttempts = repository.GetLearnerAttempts(command.SessionId);
        if (existingAttempts.Any(a => a.Status == LearnerAttemptStatus.Submitted || a.Status == LearnerAttemptStatus.Scored))
        {
            throw new ArgumentException("ATTEMPT_ALREADY_SUBMITTED");
        }

        if (command.Answers == null || !command.Answers.Any())
        {
            throw new ArgumentException("ANSWER_REQUIRED");
        }

        var attemptId = Guid.NewGuid().ToString();
        var correctCount = 0;
        var totalCount = command.Answers.Count;
        var attemptAnswers = new List<AttemptAnswer>();

        var existingReviewItems = repository.GetReviewItems(command.LearnerId);

        foreach (var (questionId, learnerAnswer) in command.Answers)
        {
            var question = repository.GetPublishedQuestion(questionId);
            if (question is null)
            {
                throw new ArgumentException("QUESTION_NOT_IN_SESSION");
            }

            var lesson = repository.GetPublishedLesson(question.LessonId);
            if (lesson is null)
            {
                throw new ArgumentException("LESSON_NOT_FOUND");
            }

            var isCorrect = question.CorrectAnswer == learnerAnswer;

            if (isCorrect)
            {
                correctCount++;
            }
            else
            {
                var isBlocking = session.ActivityType == LearnerAssignmentType.MiniTest || session.ActivityType == LearnerAssignmentType.PartTest || session.ActivityType == LearnerAssignmentType.FullTest;
                
                var existingItem = existingReviewItems.FirstOrDefault(r => r.QuestionId == questionId);
                if (existingItem != null)
                {
                    var updatedItem = existingItem with
                    {
                        SourceAttemptId = attemptId,
                        LearnerAnswer = learnerAnswer,
                        Status = ReviewItemStatus.Open,
                        IsBlocking = isBlocking,
                        ResolvedAtUtc = null
                    };
                    repository.UpsertReviewItem(updatedItem);
                }
                else
                {
                    var newItem = new ReviewItem(
                        Guid.NewGuid().ToString(),
                        command.LearnerId,
                        attemptId,
                        questionId,
                        lesson.UnitId,
                        question.SkillTags,
                        learnerAnswer,
                        question.CorrectAnswer ?? "",
                        ReviewItemStatus.Open,
                        isBlocking,
                        DateTimeOffset.UtcNow,
                        null
                    );
                    repository.UpsertReviewItem(newItem);
                }
            }

            var attemptAnswer = new AttemptAnswer(
                Guid.NewGuid().ToString(),
                attemptId,
                questionId,
                learnerAnswer,
                question.CorrectAnswer ?? "",
                isCorrect,
                DateTimeOffset.UtcNow
            );

            attemptAnswers.Add(attemptAnswer);
        }

        var scorePercent = totalCount > 0 ? (int)Math.Round((double)correctCount / totalCount * 100) : 0;

        var attempt = new LearnerAttempt(
            attemptId,
            session.SessionId,
            command.LearnerId,
            LearnerAttemptStatus.Scored,
            correctCount,
            totalCount,
            scorePercent,
            DateTimeOffset.UtcNow
        );

        repository.UpsertLearnerAttempt(attempt);

        foreach (var ans in attemptAnswers)
        {
            repository.UpsertAttemptAnswer(ans);
        }

        // Mark parent session complete
        var updatedSession = session with 
        { 
            Status = ActivitySessionStatus.Completed,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
        repository.UpsertActivitySession(updatedSession);

        var assignments = repository.GetLearnerAssignments(command.LearnerId);
        var assignment = assignments.FirstOrDefault(a => a.AssignmentId == session.AssignmentId);
        if (assignment != null)
        {
            repository.UpsertLearnerAssignment(assignment with { Status = LearnerAssignmentStatus.Completed });
            var masteryService = new Application.Features.Learner.Mastery.MasteryCalculationService(repository);
            masteryService.RecalculateMastery(command.LearnerId, assignment.ContentRefId);
        }

        return new SubmitAttemptResponse(
            attempt.AttemptId,
            attempt.ScorePercent,
            attempt.CorrectCount,
            attempt.TotalCount,
            attempt.Status.ToString()
        );
    }
}
