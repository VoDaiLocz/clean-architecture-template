using Application.Common.Interfaces.Jobs;

namespace Infrastructure.Jobs;

public sealed class InMemoryBackgroundJobQueue : IBackgroundJobQueue
{
    private readonly BackgroundJobRetryPolicy retryPolicy;
    private readonly List<BackgroundJob> jobs = [];

    public InMemoryBackgroundJobQueue(BackgroundJobRetryPolicy retryPolicy)
    {
        this.retryPolicy = retryPolicy;
    }

    public string Enqueue(EnqueueBackgroundJobRequest request)
    {
        var jobId = $"job-{jobs.Count + 1:000000}";
        jobs.Add(new BackgroundJob(
            jobId,
            request.JobType,
            request.PayloadRef,
            BackgroundJobStatus.Queued,
            AttemptCount: 0,
            FailureReason: null
        ));

        return jobId;
    }

    public BackgroundJobLease? TryLeaseNext()
    {
        var index = jobs.FindIndex(job => job.Status == BackgroundJobStatus.Queued);
        if (index < 0)
        {
            return null;
        }

        var leased = jobs[index] with
        {
            Status = BackgroundJobStatus.Running,
            AttemptCount = jobs[index].AttemptCount + 1,
        };
        jobs[index] = leased;

        return new BackgroundJobLease(leased);
    }

    public void RecordSuccess(string jobId)
    {
        Update(jobId, job => job with
        {
            Status = BackgroundJobStatus.Succeeded,
            FailureReason = null,
        });
    }

    public void RecordFailure(string jobId, string reason)
    {
        Update(jobId, job => job.AttemptCount >= retryPolicy.MaxAttempts
            ? job with { Status = BackgroundJobStatus.Failed, FailureReason = reason }
            : job with { Status = BackgroundJobStatus.Queued, FailureReason = reason });
    }

    public BackgroundJob Get(string jobId)
    {
        return jobs.Single(job => job.JobId == jobId);
    }

    private void Update(string jobId, Func<BackgroundJob, BackgroundJob> update)
    {
        var index = jobs.FindIndex(job => job.JobId == jobId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Unknown background job: {jobId}");
        }

        jobs[index] = update(jobs[index]);
    }
}

public sealed record BackgroundJobRetryPolicy
{
    public BackgroundJobRetryPolicy(int maxAttempts)
    {
        if (maxAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAttempts), "Max attempts must be at least 1.");
        }

        MaxAttempts = maxAttempts;
    }

    public int MaxAttempts { get; }
}
