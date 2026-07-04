using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.ReadingTests;

public record StartReadingTestCommand(string LearnerId);

public record StartReadingTestResponse(
    string SessionId,
    IReadOnlyList<string> AssignedQuestionIds,
    DateTimeOffset ExpiredAtUtc
);

public class StartReadingTestSessionHandler
{
    private readonly IKnowledgeRepository _repository;

    public StartReadingTestSessionHandler(IKnowledgeRepository repository)
    {
        _repository = repository;
    }

    public Task<StartReadingTestResponse> Handle(StartReadingTestCommand request, CancellationToken cancellationToken)
    {
        var assignedQuestionIds = new List<string>();
        
        var parts = new[] { 5, 6, 7 };
        foreach (var part in parts)
        {
            var questions = _repository.GetPublishedQuestions(part);
            if (questions == null || questions.Count == 0)
            {
                throw new InvalidOperationException("Insufficient content");
            }
            
            assignedQuestionIds.AddRange(questions.Take(5).Select(q => q.QuestionId));
        }

        if (assignedQuestionIds.Count == 0)
        {
            throw new InvalidOperationException("Insufficient content");
        }

        var now = DateTimeOffset.UtcNow;
        var sessionId = Guid.NewGuid().ToString();
        var expiredAt = now.AddMinutes(75);

        var session = new ReadingTestSession
        {
            SessionId = sessionId,
            LearnerId = request.LearnerId,
            Status = ReadingTestSessionStatus.Started,
            StartedAtUtc = now,
            ExpiredAtUtc = expiredAt,
            AssignedQuestionIds = assignedQuestionIds,
            Answers = new Dictionary<string, string>()
        };

        _repository.UpsertReadingTestSession(session);

        return Task.FromResult(new StartReadingTestResponse(
            sessionId,
            assignedQuestionIds,
            expiredAt
        ));
    }
}
