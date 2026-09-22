namespace ExItS.PinoyBusinessPOS.Application.Returns;

/// <summary>
/// Optimistic concurrency for return batches.
/// Compare at millisecond precision so JSON DateTimeOffset round-trips do not false-conflict
/// against PostgreSQL/EF values that retain sub-millisecond ticks.
/// </summary>
internal static class ReturnBatchConcurrency
{
    internal static bool IsMismatch(DateTimeOffset current, DateTimeOffset? expected) =>
        expected is not null
        && current.ToUnixTimeMilliseconds() != expected.Value.ToUnixTimeMilliseconds();
}
