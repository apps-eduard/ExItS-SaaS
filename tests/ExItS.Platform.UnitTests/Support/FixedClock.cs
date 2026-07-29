using ExItS.Platform.Domain.Abstractions;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class FixedClock : IClock
{
    public FixedClock(DateTimeOffset utcNow)
    {
        if (utcNow.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("Clock must provide UTC.", nameof(utcNow));
        }

        UtcNow = utcNow;
    }

    public DateTimeOffset UtcNow { get; set; }
}
