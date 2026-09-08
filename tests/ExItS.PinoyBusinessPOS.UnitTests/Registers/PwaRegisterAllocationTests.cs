using ExItS.PinoyBusinessPOS.Application.Registers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Registers;

public sealed class PwaRegisterAllocationTests
{
    [Fact]
    public void NextDisplayName_starts_at_0001_when_empty()
    {
        Assert.Equal("PWA-0001", PwaRegisterAllocation.NextDisplayName([]));
    }

    [Fact]
    public void NextDisplayName_uses_max_plus_one_and_ignores_manual_names()
    {
        Assert.Equal(
            "PWA-0003",
            PwaRegisterAllocation.NextDisplayName(["Front Counter", "PWA-0001", "PWA-0002"]));
    }

    [Fact]
    public void NextDisplayName_does_not_reuse_gaps()
    {
        Assert.Equal("PWA-0004", PwaRegisterAllocation.NextDisplayName(["PWA-0001", "PWA-0003"]));
    }

    [Fact]
    public void IsPwaDisplayName_matches_canonical_pattern_only()
    {
        Assert.True(PwaRegisterAllocation.IsPwaDisplayName("PWA-0001"));
        Assert.True(PwaRegisterAllocation.IsPwaDisplayName("pwa-0012"));
        Assert.False(PwaRegisterAllocation.IsPwaDisplayName("Front Counter"));
        Assert.False(PwaRegisterAllocation.IsPwaDisplayName("PWA-1"));
        Assert.False(PwaRegisterAllocation.IsPwaDisplayName("PWA-00001"));
    }
}
