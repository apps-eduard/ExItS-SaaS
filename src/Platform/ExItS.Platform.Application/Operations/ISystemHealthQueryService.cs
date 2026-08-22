namespace ExItS.Platform.Application.Operations;

public interface ISystemHealthQueryService
{
    Task<SystemHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default);
}

public interface IHostResourceMetrics
{
    HostResourceSnapshot Capture();
}

public interface IPosHealthProbe
{
    Task<ProbedDependencyHealth> ProbeLivenessAsync(CancellationToken cancellationToken = default);

    Task<ProbedDependencyHealth> ProbeReadinessAsync(CancellationToken cancellationToken = default);
}
