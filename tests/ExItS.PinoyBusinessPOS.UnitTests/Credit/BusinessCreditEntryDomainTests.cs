using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Credit;

public sealed class BusinessCreditEntryDomainTests
{
    private static readonly PosOrganizationId Seller = PosOrganizationId.From(
        Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly PosOrganizationId Buyer = PosOrganizationId.From(
        Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-13T10:00:00Z");

    [Fact]
    public void Create_requires_positive_amount_and_remarks()
    {
        var entry = BusinessCreditEntry.Create(
            Seller,
            Buyer,
            150.50m,
            "  B2B goods  ",
            Now,
            connectionId: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"));

        Assert.Equal(150.50m, entry.Amount);
        Assert.Equal("B2B goods", entry.Remarks);
        Assert.Equal(CreditEntryStatus.Active, entry.Status);
        Assert.Equal(Seller, entry.SellerOrganizationId);
        Assert.Equal(Buyer, entry.BuyerOrganizationId);
        Assert.Null(entry.ReversedAtUtc);
    }

    [Fact]
    public void Reverse_requires_reason_and_guards_double_reverse()
    {
        var entry = BusinessCreditEntry.Create(Seller, Buyer, 80m, "Utang for rice", Now);
        entry.Reverse("Voided sale", Now.AddMinutes(5));
        Assert.Equal(CreditEntryStatus.Reversed, entry.Status);
        Assert.Equal("Voided sale", entry.ReversalReason);
        Assert.Equal(Now.AddMinutes(5), entry.ReversedAtUtc);

        var again = Assert.Throws<DomainException>(() => entry.Reverse("again", Now.AddMinutes(6)));
        Assert.Equal(DomainErrorCodes.InvalidCreditEntryStatusTransition, again.ErrorCode);
    }
}
