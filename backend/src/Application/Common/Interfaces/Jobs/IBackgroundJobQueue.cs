namespace Application.Common.Interfaces.Jobs;

public interface IBackgroundJobQueue
{
    string Enqueue(EnqueueBackgroundJobRequest request);

    BackgroundJobLease? TryLeaseNext();

    void RecordSuccess(string jobId);

    void RecordFailure(string jobId, string reason);

    BackgroundJob Get(string jobId);
}

public sealed record EnqueueBackgroundJobRequest
{
    public EnqueueBackgroundJobRequest(string jobType, string payloadRef)
    {
        if (string.IsNullOrWhiteSpace(jobType))
        {
            throw new ArgumentException("Job type is required.", nameof(jobType));
        }

        if (string.IsNullOrWhiteSpace(payloadRef))
        {
            throw new ArgumentException("Payload reference is required.", nameof(payloadRef));
        }

        JobType = jobType;
        PayloadRef = payloadRef;
    }

    public string JobType { get; }

    public string PayloadRef { get; }
}

public sealed record BackgroundJob(
    string JobId,
    string JobType,
    string PayloadRef,
    BackgroundJobStatus Status,
    int AttemptCount,
    string? FailureReason
);

public sealed record BackgroundJobLease(BackgroundJob Job);

public enum BackgroundJobStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
}
