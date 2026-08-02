using ExItS.Platform.Application.Identity;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class InMemoryStaffNumberGenerator : IStaffNumberGenerator
{
    private int _sequence;

    public Task<string> GenerateNextAsync(CancellationToken cancellationToken = default)
    {
        _sequence++;
        return Task.FromResult($"STF-{_sequence:D6}");
    }
}
