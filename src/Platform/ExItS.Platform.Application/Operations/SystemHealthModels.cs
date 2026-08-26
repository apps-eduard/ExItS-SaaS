namespace ExItS.Platform.Application.Operations;

public static class SystemHealthStatuses
{
    public const string Healthy = "Healthy";
    public const string Degraded = "Degraded";
    public const string Unhealthy = "Unhealthy";
    public const string Unavailable = "Unavailable";
    public const string Unknown = "Unknown";
    public const string NotAvailable = "NotAvailable";
}

public static class SystemHealthServiceNames
{
    public const string PlatformApi = "platform-api";
    public const string PosApi = "pos-api";
    public const string PlatformDatabase = "platform-database";
    public const string PosDatabase = "pos-database";
}

public static class SystemHealthBackupStatuses
{
    public const string NotAvailable = SystemHealthStatuses.NotAvailable;
}

public sealed record SystemHealthSnapshot(
    string OverallStatus,
    HostResourceSnapshot Host,
    IReadOnlyList<ServiceHealthSnapshot> Services,
    BuildMetadataSnapshot Build,
    BackupHealthSnapshot Backup);

public sealed record HostResourceSnapshot(
    double? CpuPercent,
    long? MemoryUsedBytes,
    long? MemoryTotalBytes,
    long? StorageUsedBytes,
    long? StorageFreeBytes,
    long? StorageTotalBytes,
    long? UptimeSeconds);

public sealed record ServiceHealthSnapshot(
    string Name,
    string Status,
    long? LatencyMs,
    DateTimeOffset CheckedAtUtc);

public sealed record BuildMetadataSnapshot(
    string Environment,
    string? ApplicationVersion,
    string? CommitSha);

public sealed record BackupHealthSnapshot(
    string Status,
    DateTimeOffset? LastSuccessfulAtUtc,
    long? AgeSeconds);

public sealed record ProbedDependencyHealth(
    string Status,
    long? LatencyMs,
    DateTimeOffset CheckedAtUtc);
