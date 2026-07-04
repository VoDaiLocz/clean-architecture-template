using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;
using Domain.Aggregates.LearningItems;
using System.Text.Json;

namespace Application.Features.Learner.TestSessions;

public record GenerateToeicRepairPlanCommand(string SessionId, string LearnerId);

public class GenerateToeicRepairPlanHandler(IKnowledgeRepository repository)
{
    public async Task<ToeicRepairPlan> Handle(GenerateToeicRepairPlanCommand command, CancellationToken cancellationToken = default)
    {
        var session = await new GetPracticeTestSessionHandler(repository)
            .Handle(new GetPracticeTestSessionQuery(command.LearnerId, command.SessionId), cancellationToken);

        if (session.Status != PracticeTestStatus.Submitted && session.Status != PracticeTestStatus.Expired)
        {
            throw new InvalidOperationException("Session not submitted or expired");
        }

        var reviewQuestionIds = new List<string>();
        var tagsToPractice = new HashSet<string>();
        var incorrectParts = new HashSet<int>();

        foreach (var qId in session.AssignedQuestionIds)
        {
            var pq = repository.GetPublishedQuestion(qId);
            if (pq == null) continue;

            session.Answers.TryGetValue(qId, out var answer);
            bool isCorrect = string.Equals(pq.CorrectAnswer, answer, StringComparison.OrdinalIgnoreCase);

            if (!isCorrect)
            {
                reviewQuestionIds.Add(qId);
                incorrectParts.Add(pq.ToeicPart);

                if (!string.IsNullOrEmpty(pq.SkillTags))
                {
                    try
                    {
                        var tags = JsonSerializer.Deserialize<List<string>>(pq.SkillTags);
                        if (tags != null)
                        {
                            foreach (var tag in tags) tagsToPractice.Add(tag);
                        }
                    }
                    catch { }
                }
            }
        }

        var drillQuestionIds = new HashSet<string>();
        var selectedGroups = new HashSet<string>();
        var selectedPassages = new HashSet<string>();

        foreach (var part in incorrectParts)
        {
            var published = repository.GetPublishedQuestions(part);
            foreach (var pq in published)
            {
                if (drillQuestionIds.Count >= 5) break;
                if (session.AssignedQuestionIds.Contains(pq.QuestionId)) continue; // unseen only

                bool matchesTag = false;
                if (!string.IsNullOrEmpty(pq.SkillTags))
                {
                    try
                    {
                        var tags = JsonSerializer.Deserialize<List<string>>(pq.SkillTags);
                        if (tags != null && tags.Any(t => tagsToPractice.Contains(t)))
                        {
                            matchesTag = true;
                        }
                    }
                    catch { }
                }

                if (matchesTag || tagsToPractice.Count == 0)
                {
                    // If part 3/4, need group
                    if (!string.IsNullOrEmpty(pq.GroupId))
                    {
                        if (selectedGroups.Add(pq.GroupId))
                        {
                            var groupQs = published.Where(q => q.GroupId == pq.GroupId).Select(q => q.QuestionId);
                            foreach (var gq in groupQs) drillQuestionIds.Add(gq);
                        }
                    }
                    else if (!string.IsNullOrEmpty(pq.PassageId))
                    {
                        if (selectedPassages.Add(pq.PassageId))
                        {
                            var passageQs = published.Where(q => q.PassageId == pq.PassageId).Select(q => q.QuestionId);
                            foreach (var gq in passageQs) drillQuestionIds.Add(gq);
                        }
                    }
                    else
                    {
                        drillQuestionIds.Add(pq.QuestionId);
                    }
                }
            }
        }

        var plan = new ToeicRepairPlan(
            RepairPlanId: Guid.NewGuid().ToString(),
            SourceSessionId: command.SessionId,
            LearnerId: command.LearnerId,
            Status: RepairPlanStatus.Generated,
            ReviewQuestionIds: reviewQuestionIds,
            DrillQuestionIds: drillQuestionIds.ToList(),
            Answers: new Dictionary<string, string>(),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            StartedAtUtc: null,
            SubmittedAtUtc: null,
            ExpiredAtUtc: null
        );

        repository.UpsertToeicRepairPlan(plan);
        return plan;
    }
}
