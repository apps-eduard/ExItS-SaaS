using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.UnitTests.Common;

public sealed class PosDocumentNumbersTests
{
    [Fact]
    public void Format_pads_to_three_digits_and_expands_past_999()
    {
        var day = new DateOnly(2026, 9, 8);
        Assert.Equal("260908-001", PosDocumentNumbers.Format(day, 1));
        Assert.Equal("260908-002", PosDocumentNumbers.Format(day, 2));
        Assert.Equal("260908-999", PosDocumentNumbers.Format(day, 999));
        Assert.Equal("260908-1000", PosDocumentNumbers.Format(day, 1000));
    }

    [Fact]
    public void Format_next_day_starts_independent_sequence_display()
    {
        Assert.Equal("260908-001", PosDocumentNumbers.Format(new DateOnly(2026, 9, 8), 1));
        Assert.Equal("260909-001", PosDocumentNumbers.Format(new DateOnly(2026, 9, 9), 1));
    }

    [Fact]
    public void FormatChild_keeps_root_and_appends_Rn()
    {
        Assert.Equal("260908-004-R1", PosDocumentNumbers.FormatChild("260908-004", 1));
        Assert.Equal("260908-004-R1", PosDocumentNumbers.Normalize(" 260908-004-r1 "));
        Assert.Equal("260908-004", PosDocumentNumbers.NormalizeRoot("260908-004"));
        Assert.Throws<DomainException>(() => PosDocumentNumbers.NormalizeRoot("260908-004-R1"));
    }

    [Fact]
    public void Invalid_patterns_are_rejected()
    {
        foreach (var invalid in new[] { "", "SALE-260908-001", "260908-1", "260908", "26-001", "260908-001-X1" })
        {
            Assert.Throws<DomainException>(() => PosDocumentNumbers.Normalize(invalid));
        }

        Assert.Throws<DomainException>(() => PosDocumentNumbers.Format(new DateOnly(2026, 9, 8), 0));
        Assert.Throws<DomainException>(() => PosDocumentNumbers.FormatChild("260908-001", 0));
    }
}
