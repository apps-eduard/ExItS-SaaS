using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Registers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Sales;

public sealed class SaleBuyerPartyOwnershipTests
{
    private static readonly PosOrganizationId SellerOrg = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid Actor = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 8, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly CashierShiftId Shift = CashierShiftId.New();
    private static readonly RegisterId Register = RegisterId.New();

    private static SaleLineDraft Draft() =>
        new(CatalogProductId.New(), "Tinapa", "SKU-1", null, UnitOfMeasure.Piece, 50m, 1m);

    private static Sale Checkout(SaleBuyerParty? buyer, POSCustomerId? customerId = null) =>
        Sale.Checkout(
            SellerOrg,
            SaleNumbers.Format(new DateOnly(2026, 8, 14), 1),
            SalePaymentMethod.Cash,
            [Draft()],
            Actor,
            Now,
            amountTendered: 100m,
            cashierShiftId: Shift,
            registerId: Register,
            customerId: customerId,
            buyerParty: buyer);

    [Fact]
    public void Walk_in_sale_is_owned_by_seller_organization()
    {
        var sale = Checkout(SaleBuyerParty.WalkIn());
        Assert.Equal(SellerOrg, sale.OrganizationId);
        Assert.Equal(SaleBuyerPartyKind.WalkIn, sale.BuyerParty.Kind);
        Assert.Null(sale.CustomerId);
        Assert.Equal(Actor, sale.RecordedBy);
    }

    [Fact]
    public void External_customer_sale_remains_seller_owned()
    {
        var customerId = POSCustomerId.New();
        var sale = Checkout(SaleBuyerParty.ExternalCustomer("Maria"), customerId);
        Assert.Equal(SellerOrg, sale.OrganizationId);
        Assert.Equal(SaleBuyerPartyKind.ExternalCustomer, sale.BuyerParty.Kind);
        Assert.Equal(customerId, sale.CustomerId);
    }

    [Fact]
    public void Personal_buyer_does_not_make_sale_personal_owned()
    {
        var sale = Checkout(SaleBuyerParty.Personal("EX-4827-1936", "Eduard"));
        Assert.Equal(SellerOrg, sale.OrganizationId);
        Assert.Equal(SaleBuyerPartyKind.Personal, sale.BuyerParty.Kind);
        Assert.Equal("EX-4827-1936", sale.BuyerParty.PersonalPublicUserId);
        Assert.Null(sale.CustomerId);
        Assert.NotEqual(sale.OrganizationId.Value, Actor);
    }

    [Fact]
    public void Organization_buyer_uses_org_identity_not_owner_user()
    {
        var buyerOrg = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var sale = Checkout(SaleBuyerParty.Organization(buyerOrg, "ORG001234", "ABC Trading"));
        Assert.Equal(SellerOrg, sale.OrganizationId);
        Assert.Equal(SaleBuyerPartyKind.Organization, sale.BuyerParty.Kind);
        Assert.Equal(buyerOrg, sale.BuyerParty.BuyerOrganizationId);
        Assert.Equal("ORG001234", sale.BuyerParty.BuyerPublicOrganizationId);
        Assert.Null(sale.BuyerParty.PersonalPublicUserId);
    }

    [Fact]
    public void Actor_user_is_not_transaction_owner()
    {
        var sale = Checkout(SaleBuyerParty.Personal("EX-4827-1936", "Eduard"));
        Assert.Equal(Actor, sale.RecordedBy);
        Assert.NotEqual(sale.OrganizationId.Value, sale.RecordedBy);
    }

    [Fact]
    public void Legacy_null_customer_defaults_to_walk_in_without_guessing_identity()
    {
        var sale = Checkout(buyer: null, customerId: null);
        Assert.Equal(SaleBuyerPartyKind.WalkIn, sale.BuyerParty.Kind);
        Assert.Null(sale.BuyerParty.PersonalPublicUserId);
        Assert.Null(sale.BuyerParty.BuyerOrganizationId);
    }

    [Fact]
    public void Legacy_customer_defaults_to_external_without_guessing_exits_identity()
    {
        var sale = Checkout(buyer: null, customerId: POSCustomerId.New());
        Assert.Equal(SaleBuyerPartyKind.ExternalCustomer, sale.BuyerParty.Kind);
        Assert.Null(sale.BuyerParty.PersonalPublicUserId);
        Assert.Null(sale.BuyerParty.BuyerOrganizationId);
    }

    [Fact]
    public void Walk_in_rejects_customer_id()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Checkout(SaleBuyerParty.WalkIn(), POSCustomerId.New()));
        Assert.Equal(DomainErrorCodes.InvalidSaleBuyerParty, ex.ErrorCode);
    }

    [Fact]
    public void Personal_public_id_snapshot_survives_rehydrate()
    {
        var original = Checkout(SaleBuyerParty.Personal("EX-4827-1936", "Eduard"));
        var rehydrated = Sale.Rehydrate(
            original.Id,
            original.OrganizationId,
            original.SaleNumber,
            original.Status,
            original.PaymentMethod,
            original.Subtotal,
            original.Total,
            original.TaxAmount,
            original.AmountTendered,
            original.ChangeAmount,
            original.GCashReference,
            original.RecordedAtUtc,
            original.RecordedBy,
            original.VoidedAtUtc,
            original.VoidedBy,
            original.VoidReason,
            original.UpdatedAtUtc,
            original.Lines,
            original.CustomerId,
            original.LinkedCreditEntryId,
            original.CashierShiftId,
            original.RegisterId,
            original.BuyerParty);

        Assert.Equal(SaleBuyerPartyKind.Personal, rehydrated.BuyerParty.Kind);
        Assert.Equal("EX-4827-1936", rehydrated.BuyerParty.PersonalPublicUserId);
        Assert.Equal("Eduard", rehydrated.BuyerParty.DisplayNameSnapshot);
        Assert.Equal(SellerOrg, rehydrated.OrganizationId);
    }
}
