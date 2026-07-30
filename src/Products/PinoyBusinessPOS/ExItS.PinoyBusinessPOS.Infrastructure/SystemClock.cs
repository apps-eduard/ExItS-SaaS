using ExItS.PinoyBusinessPOS.Domain.Abstractions;

namespace ExItS.PinoyBusinessPOS.Infrastructure;

internal sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
