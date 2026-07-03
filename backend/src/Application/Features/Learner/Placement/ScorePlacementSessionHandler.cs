using Application.Common.Interfaces.Repositories;
using Application.Features.Learner.Onboarding;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Placement;

public sealed record PlacementAnswerSubmission(string QuestionId, string? LearnerAnswer, bool Skipped);

public sealed record ScorePlacementSessionCommand(string SessionId, IReadOnlyList<PlacementAnswerSubmission> Answers);

public sealed record ScorePlacementSessionResponse(
    string ResultId,
    string SessionId,
    string LearnerId,
    int CorrectCount,
    int TotalCount,
    int ScorePercent,
    string DiagnosticScoreBand,
    LearnerNextAction NextAction
);

public sealed class ScorePlacementSessionHandler(IKnowledgeRepository repository)
{
    public ScorePlacementSessionResponse Handle(ScorePlacementSessionCommand command)
    {
        var session = repository.GetPlacementSessionById(command.SessionId)
            ?? throw new InvalidOperationException("PLACEMENT_SESSION_NOT_FOUND");

        var assignedQuestions = repository.GetPlacementSessionAssignedQuestions(command.SessionId);
        if (assignedQuestions.Count == 0)
        {
            throw new InvalidOperationException("PLACEMENT_ANSWER_SET_INCOMPLETE");
        }

        if (command.Answers.Count != assignedQuestions.Count)
        {
            throw new InvalidOperationException("PLACEMENT_ANSWER_SET_INCOMPLETE");
        }

        var submittedIds = command.Answers.Select(a => a.QuestionId).ToHashSet();
        if (!assignedQuestions.All(q => submittedIds.Contains(q)))
        {
            throw new InvalidOperationException("PLACEMENT_QUESTION_MISMATCH");
        }

        var existingResult = repository.GetPlacementResultBySessionId(command.SessionId);
        if (existingResult is not null)
        {
            // Simple idempotency: return existing result if already processed
            return new ScorePlacementSessionResponse(
                existingResult.ResultId,
                existingResult.SessionId,
                existingResult.LearnerId,
                existingResult.CorrectCount,
                existingResult.TotalCount,
                existingResult.ScorePercent,
                existingResult.DiagnosticScoreBand,
                new LearnerNextAction("GenerateLearningPath", $"/api/learner/placement/{command.SessionId}/generate-path", "Generate Learning Path")
            );
        }

        if (session.Status != PlacementSessionStatus.InProgress)
        {
            throw new InvalidOperationException("PLACEMENT_SESSION_NOT_IN_PROGRESS");
        }

        int correctCount = 0;
        int totalCount = assignedQuestions.Count;

        var breakdowns = new List<PlacementResultBreakdown>();
        string resultId = $"plres-{Guid.NewGuid():N}";

        foreach (var answer in command.Answers)
        {
            var q = repository.GetPublishedQuestion(answer.QuestionId);
            if (q != null)
            {
                bool isCorrect = !answer.Skipped && answer.LearnerAnswer == q.CorrectAnswer;
                if (isCorrect) correctCount++;
                
                // Add simple part breakdown logic
                breakdowns.Add(new PlacementResultBreakdown(
                    resultId,
                    "Part",
                    q.ToeicPart.ToString(),
                    isCorrect ? 1 : 0,
                    1,
                    isCorrect ? 100 : 0
                ));
            }
        }

        int percent = totalCount > 0 ? (correctCount * 100 / totalCount) : 0;
        
        string band = percent switch
        {
            < 50 => "High",     // Means high weakness / low score
            < 75 => "Medium",   
            < 90 => "Low",      
            _ => "None"         
        };

        var result = new PlacementResult(
            resultId,
            command.SessionId,
            session.LearnerId,
            correctCount,
            totalCount,
            percent,
            band,
            percent * 990 / 100, // naive estimate
            percent * 990 / 100, // naive estimate
            DateTimeOffset.UtcNow
        );

        // Aggregate breakdowns by dimension
        var aggBreakdowns = breakdowns
            .GroupBy(b => new { b.DimensionType, b.DimensionValue })
            .Select(g => new PlacementResultBreakdown(
                resultId,
                g.Key.DimensionType,
                g.Key.DimensionValue,
                g.Sum(x => x.CorrectCount),
                g.Sum(x => x.TotalCount),
                g.Sum(x => x.CorrectCount) * 100 / g.Sum(x => x.TotalCount)
            ))
            .ToList();

        repository.InsertPlacementResult(result, aggBreakdowns);

        session = session with { Status = PlacementSessionStatus.Completed, CompletedAtUtc = DateTimeOffset.UtcNow };
        repository.UpsertPlacementSession(session);

        return new ScorePlacementSessionResponse(
            resultId,
            command.SessionId,
            session.LearnerId,
            correctCount,
            totalCount,
            percent,
            band,
            new LearnerNextAction("GenerateLearningPath", $"/api/learner/placement/{command.SessionId}/generate-path", "Generate Learning Path")
        );
    }
}
