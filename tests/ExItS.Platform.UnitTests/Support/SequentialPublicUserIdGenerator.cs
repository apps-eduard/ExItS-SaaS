using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.UnitTests.Support;

/// <summary>Deterministic unique ExItS IDs for unit tests (EX-0000-0001 …).</summary>
internal sealed class SequentialPublicUserIdGenerator : IPublicUserIdGenerator
{
    private int _sequence;

    public Task<string> GenerateUniqueAsync(CancellationToken cancellationToken = default)
    {
        var n = Interlocked.Increment(ref _sequence);
        var left = (n / 10_000) % 10_000;
        var right = n % 10_000;
        return Task.FromResult($"EX-{left:D4}-{right:D4}");
    }
}
