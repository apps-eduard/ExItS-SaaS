using System.Diagnostics;
using ExItS.Platform.Application.Operations;

namespace ExItS.Platform.Infrastructure.Operations;

/// <summary>
/// Process and GC-visible host metrics. Never returns filesystem paths or environment values.
/// Failures yield null metric fields rather than throwing.
/// </summary>
internal sealed class HostResourceMetricsCollector : IHostResourceMetrics
{
    public HostResourceSnapshot Capture()
    {
        double? cpuPercent = null;
        long? memoryUsed = null;
        long? memoryTotal = null;
        long? storageUsed = null;
        long? storageFree = null;
        long? storageTotal = null;
        long? uptimeSeconds = null;

        try
        {
            using var process = Process.GetCurrentProcess();
            var wall = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            if (wall.TotalMilliseconds > 0)
            {
                var cpuRatio = process.TotalProcessorTime.TotalMilliseconds
                    / (Environment.ProcessorCount * wall.TotalMilliseconds);
                cpuPercent = Math.Clamp(cpuRatio * 100d, 0d, 100d);
            }

            if (wall.TotalSeconds >= 0)
            {
                uptimeSeconds = (long)Math.Max(0, wall.TotalSeconds);
            }
        }
        catch
        {
            // Process metrics are optional; never fail the operations endpoint.
        }

        try
        {
            var gc = GC.GetGCMemoryInfo();
            if (gc.TotalAvailableMemoryBytes > 0)
            {
                memoryTotal = gc.TotalAvailableMemoryBytes;
                memoryUsed = gc.MemoryLoadBytes > 0
                    ? Math.Min(gc.MemoryLoadBytes, gc.TotalAvailableMemoryBytes)
                    : null;
            }
        }
        catch
        {
            // Memory metrics are optional.
        }

        try
        {
            var root = Path.GetPathRoot(AppContext.BaseDirectory);
            if (!string.IsNullOrWhiteSpace(root))
            {
                var drive = new DriveInfo(root);
                if (drive.IsReady && drive.TotalSize > 0)
                {
                    storageTotal = drive.TotalSize;
                    storageFree = Math.Max(0, drive.AvailableFreeSpace);
                    storageUsed = Math.Max(0, drive.TotalSize - drive.AvailableFreeSpace);
                }
            }
        }
        catch
        {
            // Storage metrics are optional; path is never included in the snapshot.
        }

        return new HostResourceSnapshot(
            cpuPercent,
            memoryUsed,
            memoryTotal,
            storageUsed,
            storageFree,
            storageTotal,
            uptimeSeconds);
    }
}
