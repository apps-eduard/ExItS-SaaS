namespace ExItS.Platform.Domain.Abstractions;

/// <summary>
/// UTC time source for application/use-case boundaries. Domain methods accept DateTimeOffset;
/// they must not call DateTime.UtcNow directly.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
