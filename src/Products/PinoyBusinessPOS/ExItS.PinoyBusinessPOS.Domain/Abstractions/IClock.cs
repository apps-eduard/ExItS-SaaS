namespace ExItS.PinoyBusinessPOS.Domain.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
