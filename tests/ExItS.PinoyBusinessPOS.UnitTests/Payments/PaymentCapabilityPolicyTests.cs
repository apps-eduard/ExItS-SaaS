using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Payments;

public sealed class PaymentCapabilityPolicyTests
{
    private static readonly string[] StarterGrants =
    [
        PosFeatureCodes.StoreBasicPayments,
        PosFeatureCodes.StoreSalesCreate,
    ];

    private static readonly string[] ProGrants =
    [
        PosFeatureCodes.StoreBasicPayments,
        PosFeatureCodes.StorePaymentManagement,
        PosFeatureCodes.StoreSalesCreate,
    ];

    private static readonly string[] ProPlusGrants =
    [
        PosFeatureCodes.StoreBasicPayments,
        PosFeatureCodes.StorePaymentManagement,
        PosFeatureCodes.StoreOnlinePayments,
        PosFeatureCodes.StoreSalesCreate,
    ];

    [Fact]
    public void Starter_allows_basic_checkout_methods_only()
    {
        Assert.True(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.Cash, PosSubscriptionStatuses.Active, StarterGrants));
        Assert.True(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.ManualGCash, PosSubscriptionStatuses.Active, StarterGrants));
        Assert.True(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.Utang, PosSubscriptionStatuses.Active, StarterGrants));
        Assert.False(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.BankTransfer, PosSubscriptionStatuses.Active, StarterGrants));
        Assert.False(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.Check, PosSubscriptionStatuses.Active, StarterGrants));
        Assert.False(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.Card, PosSubscriptionStatuses.Active, StarterGrants));
    }

    [Fact]
    public void Growth_matches_starter_payment_methods()
    {
        Assert.False(PaymentCapabilityPolicy.HasCapability(
            PaymentCapability.PaymentManagement, PosSubscriptionStatuses.Active, StarterGrants));
        Assert.False(PaymentCapabilityPolicy.HasCapability(
            PaymentCapability.OnlinePayments, PosSubscriptionStatuses.Active, StarterGrants));
    }

    [Fact]
    public void Pro_allows_manual_management_methods_not_online_card()
    {
        Assert.True(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.BankTransfer, PosSubscriptionStatuses.Active, ProGrants));
        Assert.True(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.Check, PosSubscriptionStatuses.Active, ProGrants));
        Assert.True(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.ManualMaya, PosSubscriptionStatuses.Active, ProGrants));
        Assert.False(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.Card, PosSubscriptionStatuses.Active, ProGrants));
        Assert.False(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.GCash, PosSubscriptionStatuses.Active, ProGrants));
        Assert.False(PaymentCapabilityPolicy.HasCapability(
            PaymentCapability.OnlinePayments, PosSubscriptionStatuses.Active, ProGrants));
    }

    [Fact]
    public void ProPlus_grants_online_payments_capability_without_usable_checkout_online_methods()
    {
        Assert.True(PaymentCapabilityPolicy.HasCapability(
            PaymentCapability.OnlinePayments, PosSubscriptionStatuses.Active, ProPlusGrants));
        Assert.False(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.Card, PosSubscriptionStatuses.Active, ProPlusGrants));
        Assert.False(PaymentCapabilityPolicy.IsCheckoutMethodAllowedByEntitlement(
            SalePaymentMethod.GCash, PosSubscriptionStatuses.Active, ProPlusGrants));
    }
}

public sealed class PaymentMethodCatalogTests
{
    [Fact]
    public void Catalog_covers_required_channels()
    {
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.Cash);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.ManualGCash);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.Utang);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.BankTransfer);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.Check);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.ManualMaya);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.OnlineGCash);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.OnlineMaya);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.QrPh);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.Card);
        Assert.Contains(PaymentMethodCatalog.All, d => d.MethodCode == PaymentMethodCatalog.OnlineBanking);
    }

    [Fact]
    public void Online_channels_are_coming_soon_and_not_checkout_sale_methods()
    {
        foreach (var def in PaymentMethodCatalog.All.Where(d => d.RequiredCapability == PaymentCapability.OnlinePayments))
        {
            Assert.Equal(PaymentMethodAvailability.ComingSoon, def.Availability);
            Assert.False(def.IsCheckoutSaleMethod);
            Assert.Equal(PaymentIntegrationMode.Online, def.IntegrationMode);
        }
    }
}
