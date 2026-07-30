using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Credit;

public sealed class CreditEntryDomainTests
{
    private static readonly PosOrganizationId OrgA = PosOrganizationId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly POSCustomerId CustomerA = POSCustomerId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-07-30T10:00:00Z");

    [Fact]
    public void Create_requires_positive_amount_and_remarks()
    {
        var entry = CreditEntry.Create(OrgA, CustomerA, 150.50m, "  Sari-sari goods  ", Now);
        Assert.Equal(150.50m, entry.Amount);
        Assert.Equal("Sari-sari goods", entry.Remarks);
        Assert.Equal(CreditEntryStatus.Active, entry.Status);
        Assert.Null(entry.ReversedAtUtc);
        Assert.Null(entry.ReversalReason);
    }

    [Fact]
    public void Create_rejects_zero_negative_or_excess_scale()
    {
        Assert.Equal(
            DomainErrorCodes.InvalidCreditAmount,
            Assert.Throws<DomainException>(() => CreditEntry.Create(OrgA, CustomerA, 0m, "x", Now)).ErrorCode);
        Assert.Equal(
            DomainErrorCodes.InvalidCreditAmount,
            Assert.Throws<DomainException>(() => CreditEntry.Create(OrgA, CustomerA, -1m, "x", Now)).ErrorCode);
        Assert.Equal(
            DomainErrorCodes.InvalidCreditAmount,
            Assert.Throws<DomainException>(() => CreditEntry.Create(OrgA, CustomerA, 1.001m, "x", Now)).ErrorCode);
    }

    [Fact]
    public void Create_rejects_blank_remarks()
    {
        var ex = Assert.Throws<DomainException>(() => CreditEntry.Create(OrgA, CustomerA, 10m, "   ", Now));
        Assert.Equal(DomainErrorCodes.InvalidCreditRemarks, ex.ErrorCode);
    }

    [Fact]
    public void Reverse_requires_reason_and_is_idempotent_guarded()
    {
        var entry = CreditEntry.Create(OrgA, CustomerA, 80m, "Utang for rice", Now);
        entry.Reverse("Customer returned goods", Now.AddMinutes(5));
        Assert.Equal(CreditEntryStatus.Reversed, entry.Status);
        Assert.Equal("Customer returned goods", entry.ReversalReason);
        Assert.Equal(Now.AddMinutes(5), entry.ReversedAtUtc);

        var again = Assert.Throws<DomainException>(() => entry.Reverse("again", Now.AddMinutes(6)));
        Assert.Equal(DomainErrorCodes.InvalidCreditEntryStatusTransition, again.ErrorCode);
    }

    [Fact]
    public void Amount_and_remarks_are_immutable_after_create()
    {
        var entry = CreditEntry.Create(OrgA, CustomerA, 25m, "Bread", Now);
        Assert.Equal(25m, entry.Amount);
        Assert.Equal("Bread", entry.Remarks);
        // No public setters / update methods exist for amount or remarks.
        Assert.Null(typeof(CreditEntry).GetProperty(nameof(CreditEntry.Amount))!.SetMethod);
        Assert.Null(typeof(CreditEntry).GetProperty(nameof(CreditEntry.Remarks))!.SetMethod);
    }
}
