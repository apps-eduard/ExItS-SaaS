using ExItS.PinoyBusinessPOS.Application.Returns;

namespace ExItS.PinoyBusinessPOS.UnitTests.Returns;

public sealed class ReturnBatchConcurrencyTests
{
    [Fact]
    public void IsMismatch_false_when_same_instant_differs_only_in_sub_milliseconds()
    {
        var current = DateTimeOffset.Parse("2026-09-21T12:00:00.1234567Z");
        var expected = DateTimeOffset.Parse("2026-09-21T12:00:00.1230000Z");

        Assert.False(ReturnBatchConcurrency.IsMismatch(current, expected));
    }

    [Fact]
    public void IsMismatch_true_when_milliseconds_differ()
    {
        var current = DateTimeOffset.Parse("2026-09-21T12:00:00.124Z");
        var expected = DateTimeOffset.Parse("2026-09-21T12:00:00.123Z");

        Assert.True(ReturnBatchConcurrency.IsMismatch(current, expected));
    }

    [Fact]
    public void IsMismatch_false_when_expected_null()
    {
        Assert.False(ReturnBatchConcurrency.IsMismatch(DateTimeOffset.UtcNow, null));
    }
}
