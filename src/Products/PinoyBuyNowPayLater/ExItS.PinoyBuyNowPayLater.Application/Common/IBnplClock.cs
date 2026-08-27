namespace ExItS.PinoyBuyNowPayLater.Application.Common;

public interface IBnplClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemBnplClock : IBnplClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
