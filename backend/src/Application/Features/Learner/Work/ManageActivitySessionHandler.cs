using Application.Common.Interfaces.Repositories;
using Domain.Aggregates.LearnerProgress;

namespace Application.Features.Learner.Work;

public sealed record StartActivitySessionCommand(string AssignmentId, string LearnerId);
public sealed record CompleteActivitySessionCommand(string SessionId, string LearnerId);
public sealed record AbandonActivitySessionCommand(string SessionId, string LearnerId);
public sealed record GetActivitySessionQuery(string SessionId, string LearnerId);

public sealed record ActivitySessionResponse(
    string SessionId,
    string AssignmentId,
    string Status,
    string ActivityType
);

public sealed class ManageActivitySessionHandler(IKnowledgeRepository repository)
{
    public ActivitySessionResponse Handle(StartActivitySessionCommand command)
    {
        var assignments = repository.GetLearnerAssignments(command.LearnerId);
        var assignment = assignments.FirstOrDefault(a => a.AssignmentId == command.AssignmentId);
        if (assignment is null)
        {
            throw new ArgumentException("ASSIGNMENT_NOT_FOUND");
        }

        if (assignment.LearnerId != command.LearnerId)
        {
            throw new ArgumentException("SESSION_NOT_OWNED");
        }

        var sessions = repository.GetActivitySessions(command.AssignmentId);
        var activeSession = sessions.FirstOrDefault(s => s.Status == ActivitySessionStatus.InProgress);
        
        if (activeSession != null)
        {
            return new ActivitySessionResponse(
                activeSession.SessionId,
                activeSession.AssignmentId,
                activeSession.Status.ToString(),
                activeSession.ActivityType.ToString()
            );
        }

        var sessionId = Guid.NewGuid().ToString();
        var session = new ActivitySession(
            sessionId,
            command.AssignmentId,
            command.LearnerId,
            assignment.AssignmentType,
            ActivitySessionStatus.InProgress,
            DateTimeOffset.UtcNow,
            null
        );

        repository.UpsertActivitySession(session);

        // Update assignment status
        var updatedAssignment = assignment with { Status = LearnerAssignmentStatus.Started };
        repository.UpsertLearnerAssignment(updatedAssignment);

        return new ActivitySessionResponse(
            session.SessionId,
            session.AssignmentId,
            session.Status.ToString(),
            session.ActivityType.ToString()
        );
    }

    public ActivitySessionResponse Handle(GetActivitySessionQuery query)
    {
        var session = repository.GetActivitySession(query.SessionId);
        if (session is null || session.LearnerId != query.LearnerId)
        {
            throw new ArgumentException("SESSION_NOT_OWNED");
        }

        return new ActivitySessionResponse(
            session.SessionId,
            session.AssignmentId,
            session.Status.ToString(),
            session.ActivityType.ToString()
        );
    }

    public ActivitySessionResponse Handle(CompleteActivitySessionCommand command)
    {
        var session = repository.GetActivitySession(command.SessionId);
        if (session is null || session.LearnerId != command.LearnerId)
        {
            throw new ArgumentException("SESSION_NOT_OWNED");
        }

        if (session.Status != ActivitySessionStatus.InProgress)
        {
            throw new ArgumentException("INVALID_SESSION_TRANSITION");
        }

        var updatedSession = session with 
        { 
            Status = ActivitySessionStatus.Completed,
            CompletedAtUtc = DateTimeOffset.UtcNow
        };
        
        repository.UpsertActivitySession(updatedSession);

        return new ActivitySessionResponse(
            updatedSession.SessionId,
            updatedSession.AssignmentId,
            updatedSession.Status.ToString(),
            updatedSession.ActivityType.ToString()
        );
    }

    public ActivitySessionResponse Handle(AbandonActivitySessionCommand command)
    {
        var session = repository.GetActivitySession(command.SessionId);
        if (session is null || session.LearnerId != command.LearnerId)
        {
            throw new ArgumentException("SESSION_NOT_OWNED");
        }

        if (session.Status != ActivitySessionStatus.InProgress)
        {
            throw new ArgumentException("INVALID_SESSION_TRANSITION");
        }

        var updatedSession = session with 
        { 
            Status = ActivitySessionStatus.Abandoned
        };
        
        repository.UpsertActivitySession(updatedSession);

        return new ActivitySessionResponse(
            updatedSession.SessionId,
            updatedSession.AssignmentId,
            updatedSession.Status.ToString(),
            updatedSession.ActivityType.ToString()
        );
    }
}
