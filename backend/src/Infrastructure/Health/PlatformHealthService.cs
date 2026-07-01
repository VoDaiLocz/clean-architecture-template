using Application.Common.Health;
using Application.Common.Interfaces.Jobs;
using Application.Common.Interfaces.Repositories;
using Application.Common.Interfaces.Storage;

namespace Infrastructure.Health;

public sealed class PlatformHealthService : IPlatformHealthService
{
    private readonly IKnowledgeRepository repository;
    private readonly IObjectStorage storage;
    private readonly IBackgroundJobQueue jobQueue;

    public PlatformHealthService(
        IKnowledgeRepository repository,
        IObjectStorage storage,
        IBackgroundJobQueue jobQueue
    )
    {
        this.repository = repository;
        this.storage = storage;
        this.jobQueue = jobQueue;
    }

    public PlatformHealthSnapshot Check()
    {
        var dependencies = new[]
        {
            CheckDatabase(),
            CheckStorage(),
            CheckJobQueue(),
        };
        var status = dependencies.All(dependency => dependency.Status == PlatformHealthStatus.Healthy)
            ? PlatformHealthStatus.Healthy
            : PlatformHealthStatus.Unhealthy;

        return new PlatformHealthSnapshot(status, dependencies);
    }

    private PlatformDependencyHealth CheckDatabase()
    {
        try
        {
            _ = repository.Count("raw_sources");
            return Healthy("database", "Database is reachable.");
        }
        catch (Exception ex)
        {
            return Unhealthy("database", ex.Message);
        }
    }

    private PlatformDependencyHealth CheckStorage()
    {
        try
        {
            _ = storage.List("__health__");
            return Healthy("object-storage", "Object storage is reachable.");
        }
        catch (Exception ex)
        {
            return Unhealthy("object-storage", ex.Message);
        }
    }

    private PlatformDependencyHealth CheckJobQueue()
    {
        try
        {
            _ = jobQueue.TryLeaseNext();
            return Healthy("background-jobs", "Background job queue is reachable.");
        }
        catch (Exception ex)
        {
            return Unhealthy("background-jobs", ex.Message);
        }
    }

    private static PlatformDependencyHealth Healthy(string name, string message) =>
        new(name, PlatformHealthStatus.Healthy, message);

    private static PlatformDependencyHealth Unhealthy(string name, string message) =>
        new(name, PlatformHealthStatus.Unhealthy, message);
}
