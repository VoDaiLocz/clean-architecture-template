namespace Application.Common.Health;

public interface IPlatformHealthService
{
    PlatformHealthSnapshot Check();
}

public sealed record PlatformHealthSnapshot(
    PlatformHealthStatus Status,
    IReadOnlyList<PlatformDependencyHealth> Dependencies
);

public sealed record PlatformDependencyHealth(
    string Name,
    PlatformHealthStatus Status,
    string Message
);

public enum PlatformHealthStatus
{
    Healthy,
    Unhealthy,
}
