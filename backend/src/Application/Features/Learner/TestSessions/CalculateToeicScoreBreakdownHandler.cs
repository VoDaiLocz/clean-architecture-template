using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Features.Learner.TestSessions;

public record CalculateToeicScoreBreakdownQuery(string LearnerId, string SessionId);

public class CalculateToeicScoreBreakdownHandler(IKnowledgeRepository repository)
{
    public async Task<ToeicScoreBreakdown> Handle(CalculateToeicScoreBreakdownQuery query, CancellationToken cancellationToken = default)
    {
        var session = await new GetPracticeTestSessionHandler(repository)
            .Handle(new GetPracticeTestSessionQuery(query.LearnerId, query.SessionId), cancellationToken);

        if (session.Status != PracticeTestStatus.Submitted && session.Status != PracticeTestStatus.Expired)
        {
            throw new InvalidOperationException("Session not submitted or expired");
        }

        int totalCorrect = 0;
        int totalQuestions = session.AssignedQuestionIds.Count;
        int listeningCorrect = 0;
        int readingCorrect = 0;
        
        var partTotal = new Dictionary<int, int>();
        var partCorrect = new Dictionary<int, int>();
        var skillWeaknesses = new Dictionary<string, int>();

        foreach (var qId in session.AssignedQuestionIds)
        {
            var pq = repository.GetPublishedQuestion(qId);
            if (pq == null) continue;

            if (!partTotal.ContainsKey(pq.ToeicPart)) partTotal[pq.ToeicPart] = 0;
            if (!partCorrect.ContainsKey(pq.ToeicPart)) partCorrect[pq.ToeicPart] = 0;

            partTotal[pq.ToeicPart]++;

            session.Answers.TryGetValue(qId, out var answer);
            bool isCorrect = string.Equals(pq.CorrectAnswer, answer, StringComparison.OrdinalIgnoreCase);

            if (isCorrect)
            {
                totalCorrect++;
                partCorrect[pq.ToeicPart]++;
                if (pq.ToeicPart <= 4) listeningCorrect++;
                else readingCorrect++;
            }
            else
            {
                if (!string.IsNullOrEmpty(pq.SkillTags))
                {
                    try
                    {
                        var tags = JsonSerializer.Deserialize<List<string>>(pq.SkillTags);
                        if (tags != null)
                        {
                            foreach (var tag in tags)
                            {
                                if (!skillWeaknesses.ContainsKey(tag)) skillWeaknesses[tag] = 0;
                                skillWeaknesses[tag]++;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore parse errors
                    }
                }
            }
        }

        var partBreakdown = new Dictionary<int, PartAccuracy>();
        foreach (var kvp in partTotal)
        {
            partBreakdown[kvp.Key] = new PartAccuracy(kvp.Value, partCorrect[kvp.Key]);
        }

        return new ToeicScoreBreakdown(
            session.SessionId,
            session.LearnerId,
            totalCorrect,
            totalQuestions,
            listeningCorrect * 5, // Simple estimation
            readingCorrect * 5,   // Simple estimation
            partBreakdown,
            skillWeaknesses
        );
    }
}
