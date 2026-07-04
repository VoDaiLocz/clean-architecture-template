using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.ListeningTests;

public record StartListeningTestCommand(string LearnerId);


public record StartListeningTestResponse(
    string SessionId,
    IReadOnlyList<string> AssignedQuestionIds,
    DateTimeOffset ExpiredAtUtc
);

public class StartListeningTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public StartListeningTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task<StartListeningTestResponse> Handle(StartListeningTestCommand request, CancellationToken cancellationToken)
    {
        var assignedQuestionIds = new List<string>();
        
        // Parts 1-4 for Listening
        var parts = new[] { 1, 2, 3, 4 };
        foreach (var part in parts)
        {
            var questions = _repository.GetPublishedQuestions(part);
            if (questions == null || questions.Count == 0)
            {
                throw new InvalidOperationException("Insufficient content");
            }
            
            // Just take up to 5 questions from each part for practice test
            assignedQuestionIds.AddRange(questions.Take(5).Select(q => q.QuestionId));
        }

        if (assignedQuestionIds.Count == 0)
        {
            throw new InvalidOperationException("Insufficient content");
        }

        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid().ToString();
        var expiredAt = now.AddMinutes(45);

        var session = new ListeningTestSession
        {
            SessionId = sessionId,
            LearnerId = request.LearnerId,
            Status = ListeningTestSessionStatus.Started,
            StartedAtUtc = now,
            ExpiredAtUtc = expiredAt,
            AssignedQuestionIds = assignedQuestionIds,
            Answers = new Dictionary<string, string>()
        };

        _repository.UpsertListeningTestSession(session);

        return Task.FromResult(new StartListeningTestResponse(
            sessionId,
            assignedQuestionIds,
            expiredAt
        ));
    }
}
