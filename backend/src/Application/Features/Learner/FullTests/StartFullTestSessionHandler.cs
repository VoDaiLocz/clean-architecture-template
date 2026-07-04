using System;
using System.Collections.Generic;
using System.Linq;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.FullTests;

public record StartFullTestCommand(string LearnerId);

public class StartFullTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public StartFullTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public FullToeicTestSession Handle(StartFullTestCommand command)
    {
        var requirements = new Dictionary<int, int>
        {
            { 1, 6 },
            { 2, 25 },
            { 3, 39 },
            { 4, 30 },
            { 5, 30 },
            { 6, 16 },
            { 7, 54 }
        };

        var selectedIds = new List<string>();

        foreach (var req in requirements)
        {
            var questions = _repository.GetPublishedQuestions(req.Key);
            if (questions.Count < req.Value)
            {
                throw new InvalidOperationException("Insufficient content");
            }
            selectedIds.AddRange(questions.Take(req.Value).Select(q => q.QuestionId));
        }

        var session = new FullToeicTestSession(
            SessionId: Guid.NewGuid().ToString(),
            LearnerId: command.LearnerId,
            Status: FullTestSessionStatus.Started,
            StartedAtUtc: DateTimeOffset.UtcNow,
            SubmittedAtUtc: null,
            ExpiredAtUtc: DateTimeOffset.UtcNow.AddMinutes(120),
            AssignedQuestionIds: selectedIds,
            Answers: new Dictionary<string, string>(),
            ResultId: null
        );

        _repository.UpsertFullTestSession(session);
        return session;
    }
}
