using System.Diagnostics;
using System.Reflection;
using ExItS.Platform.Application.Operations;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace ExItS.Platform.Infrastructure.Operations;

internal sealed class SystemHealthQueryService(
    HealthCheckService healthChecks,
    IHostResourceMetrics hostMetrics,
    IPosHealthProbe posHealthProbe,
    IHostEnvironment hostEnvironment) : ISystemHealthQueryService
{
    public const string PlatformDatabaseCheckName = "platform-database";

    public async Task<SystemHealthSnapshot> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var platformApiTask = ProbePlatformApiAsync(cancellationToken);
        var platformDbTask = ProbePlatformDatabaseAsync(cancellationToken);
        var posApiTask = posHealthProbe.ProbeLivenessAsync(cancellationToken);
        var posDbTask = posHealthProbe.ProbeReadinessAsync(cancellationToken);

        await Task.WhenAll(platformApiTask, platformDbTask, posApiTask, posDbTask).ConfigureAwait(false);

        var platformApi = await platformApiTask.ConfigureAwait(false);
        var platformDb = await platformDbTask.ConfigureAwait(false);
        var posApi = await posApiTask.ConfigureAwait(false);
        var posDb = await posDbTask.ConfigureAwait(false);

        var services = new ServiceHealthSnapshot[]
        {
            ToService(SystemHealthServiceNames.PlatformApi, platformApi),
            ToService(SystemHealthServiceNames.PosApi, posApi),
            ToService(SystemHealthServiceNames.PlatformDatabase, platformDb),
            ToService(SystemHealthServiceNames.PosDatabase, posDb)
        };

        var host = CaptureHostSafely();
        var build = ReadBuildMetadata();
        var backup = new BackupHealthSnapshot(
            SystemHealthBackupStatuses.NotAvailable,
            LastSuccessfulAtUtc: null,
            AgeSeconds: null);

        var overall = SystemHealthStatusRules.Aggregate(services.Select(s => s.Status));

        return new SystemHealthSnapshot(overall, host, services, build, backup);
    }

    private HostResourceSnapshot CaptureHostSafely()
    {
        try
        {
            return hostMetrics.Capture();
        }
        catch
        {
            return new HostResourceSnapshot(null, null, null, null, null, null, null);
        }
    }

    private async Task<ProbedDependencyHealth> ProbePlatformApiAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // Matches public /health: liveness with no dependency checks.
            _ = await healthChecks.CheckHealthAsync(_ => false, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();
            return new ProbedDependencyHealth(
                SystemHealthStatuses.Healthy,
                Math.Max(0, (long)stopwatch.Elapsed.TotalMilliseconds),
                DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            stopwatch.Stop();
            return new ProbedDependencyHealth(
                SystemHealthStatuses.Unhealthy,
                Math.Max(0, (long)stopwatch.Elapsed.TotalMilliseconds),
                DateTimeOffset.UtcNow);
        }
    }

    private async Task<ProbedDependencyHealth> ProbePlatformDatabaseAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var report = await healthChecks
                .CheckHealthAsync(check => check.Name == PlatformDatabaseCheckName, cancellationToken)
                .ConfigureAwait(false);
            stopwatch.Stop();
            var latency = Math.Max(0, (long)stopwatch.Elapsed.TotalMilliseconds);

            if (!report.Entries.TryGetValue(PlatformDatabaseCheckName, out var entry))
            {
                return new ProbedDependencyHealth(SystemHealthStatuses.Unknown, latency, DateTimeOffset.UtcNow);
            }

            var status = entry.Status switch
            {
                HealthStatus.Healthy => SystemHealthStatuses.Healthy,
                HealthStatus.Degraded => SystemHealthStatuses.Degraded,
                HealthStatus.Unhealthy => SystemHealthStatuses.Unhealthy,
                _ => SystemHealthStatuses.Unknown
            };

            return new ProbedDependencyHealth(status, latency, DateTimeOffset.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            stopwatch.Stop();
            return new ProbedDependencyHealth(
                SystemHealthStatuses.Unhealthy,
                Math.Max(0, (long)stopwatch.Elapsed.TotalMilliseconds),
                DateTimeOffset.UtcNow);
        }
    }

    private BuildMetadataSnapshot ReadBuildMetadata()
    {
        var environment = hostEnvironment.EnvironmentName;
        string? version = null;
        string? commitSha = null;

        try
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
            var assemblyVersion = assembly.GetName().Version?.ToString();

            if (!string.IsNullOrWhiteSpace(informational))
            {
                var plus = informational.IndexOf('+', StringComparison.Ordinal);
                if (plus >= 0)
                {
                    version = plus == 0 ? assemblyVersion : informational[..plus];
                    var suffix = informational[(plus + 1)..];
                    var dirty = suffix.IndexOf(".dirty", StringComparison.OrdinalIgnoreCase);
                    commitSha = dirty >= 0 ? suffix[..dirty] : suffix;
                    if (string.IsNullOrWhiteSpace(commitSha))
                    {
                        commitSha = null;
                    }
                }
                else
                {
                    version = informational;
                }
            }

            version ??= assemblyVersion;
            if (string.IsNullOrWhiteSpace(version))
            {
                version = null;
            }
        }
        catch
        {
            version = null;
            commitSha = null;
        }

        return new BuildMetadataSnapshot(environment, version, commitSha);
    }

    private static ServiceHealthSnapshot ToService(string name, ProbedDependencyHealth probe) =>
        new(name, probe.Status, probe.LatencyMs, probe.CheckedAtUtc);
}
