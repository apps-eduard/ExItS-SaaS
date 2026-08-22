using ExItS.Platform.Infrastructure.Operations;

namespace ExItS.Platform.UnitTests.Operations;

public sealed class HostResourceMetricsCollectorTests
{
    [Fact]
    public void Capture_does_not_throw_or_leak_paths_or_environment()
    {
        var collector = new HostResourceMetricsCollector();
        var snapshot = collector.Capture();

        var serialized = System.Text.Json.JsonSerializer.Serialize(snapshot);
        Assert.DoesNotContain("Password=", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ConnectionString", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("docker.sock", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"C:\", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("/var/", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain(Environment.CurrentDirectory, serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("PATH=", serialized, StringComparison.Ordinal);

        if (snapshot.MemoryUsedBytes is not null && snapshot.MemoryTotalBytes is not null)
        {
            Assert.True(snapshot.MemoryUsedBytes >= 0);
            Assert.True(snapshot.MemoryTotalBytes > 0);
        }

        if (snapshot.StorageUsedBytes is not null && snapshot.StorageTotalBytes is not null)
        {
            Assert.True(snapshot.StorageTotalBytes > 0);
            Assert.True(snapshot.StorageUsedBytes >= 0);
        }
    }
}
