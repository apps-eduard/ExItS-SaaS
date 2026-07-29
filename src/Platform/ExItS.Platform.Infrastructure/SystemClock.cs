using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;

namespace ExItS.Platform.Infrastructure;

/// <summary>System UTC clock for application boundaries.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
