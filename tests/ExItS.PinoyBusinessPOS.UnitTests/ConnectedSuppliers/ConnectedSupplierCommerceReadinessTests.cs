using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.UnitTests.ConnectedSuppliers;

public sealed class ConnectedSupplierCommerceReadinessTests
{
    [Fact]
    public void Ready_when_all_required_complete_without_utang()
    {
        var result = ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: true,
                PickupEnabled: true,
                DeliveryEnabled: false,
                PickupConfigured: true,
                DeliveryConfigured: false,
                HasAcceptedPaymentMethod: true,
                UtangPaymentEnabled: false,
                CreditAllowRequested: false,
                HasValidCreditPolicy: false,
                HasSharedCatalog: true,
                HasResponsibleContact: true));

        Assert.True(result.IsReady);
        Assert.Equal([ConnectedSupplierCommerceReadiness.FulfillmentPickup], result.SupportedFulfillmentMethods);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusNotApplicable,
            result.Requirements.Single(r => r.Code == ConnectedSupplierCommerceReadiness.DeliveryConfig).Status);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusNotApplicable,
            result.Requirements.Single(r => r.Code == ConnectedSupplierCommerceReadiness.CreditPolicy).Status);
    }

    [Fact]
    public void Delivery_config_required_only_when_org_offer_delivery_on()
    {
        var missing = ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: true,
                PickupEnabled: false,
                DeliveryEnabled: true,
                PickupConfigured: false,
                DeliveryConfigured: false,
                HasAcceptedPaymentMethod: true,
                UtangPaymentEnabled: false,
                CreditAllowRequested: false,
                HasValidCreditPolicy: false,
                HasSharedCatalog: true,
                HasResponsibleContact: true));

        Assert.False(missing.IsReady);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusMissing,
            missing.Requirements.Single(r => r.Code == ConnectedSupplierCommerceReadiness.DeliveryConfig).Status);
        Assert.Empty(missing.SupportedFulfillmentMethods);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusNotApplicable,
            missing.Requirements.Single(r => r.Code == ConnectedSupplierCommerceReadiness.PickupConfig).Status);

        var ready = ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: true,
                PickupEnabled: false,
                DeliveryEnabled: true,
                PickupConfigured: false,
                DeliveryConfigured: true,
                HasAcceptedPaymentMethod: true,
                UtangPaymentEnabled: false,
                CreditAllowRequested: false,
                HasValidCreditPolicy: false,
                HasSharedCatalog: true,
                HasResponsibleContact: true));

        Assert.True(ready.IsReady);
        Assert.Equal([ConnectedSupplierCommerceReadiness.FulfillmentDelivery], ready.SupportedFulfillmentMethods);
    }

    [Fact]
    public void Org_delivery_off_makes_delivery_config_not_applicable()
    {
        var result = ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: true,
                PickupEnabled: true,
                DeliveryEnabled: false,
                PickupConfigured: true,
                DeliveryConfigured: true,
                HasAcceptedPaymentMethod: true,
                UtangPaymentEnabled: false,
                CreditAllowRequested: false,
                HasValidCreditPolicy: false,
                HasSharedCatalog: true,
                HasResponsibleContact: true));

        Assert.True(result.IsReady);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusNotApplicable,
            result.Requirements.Single(r => r.Code == ConnectedSupplierCommerceReadiness.DeliveryConfig).Status);
        Assert.Equal([ConnectedSupplierCommerceReadiness.FulfillmentPickup], result.SupportedFulfillmentMethods);
    }

    [Fact]
    public void Pickup_can_satisfy_fulfillment_method_while_delivery_incomplete()
    {
        var result = ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: true,
                PickupEnabled: true,
                DeliveryEnabled: true,
                PickupConfigured: true,
                DeliveryConfigured: false,
                HasAcceptedPaymentMethod: true,
                UtangPaymentEnabled: false,
                CreditAllowRequested: false,
                HasValidCreditPolicy: false,
                HasSharedCatalog: true,
                HasResponsibleContact: true));

        Assert.False(result.IsReady);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusComplete,
            result.Requirements.Single(r => r.Code == ConnectedSupplierCommerceReadiness.FulfillmentMethod).Status);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusMissing,
            result.Requirements.Single(r => r.Code == ConnectedSupplierCommerceReadiness.DeliveryConfig).Status);
        Assert.Equal([ConnectedSupplierCommerceReadiness.FulfillmentPickup], result.SupportedFulfillmentMethods);
    }

    [Fact]
    public void Utang_requires_credit_policy_when_enabled()
    {
        var allowOff = ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: true,
                PickupEnabled: true,
                DeliveryEnabled: true,
                PickupConfigured: true,
                DeliveryConfigured: true,
                HasAcceptedPaymentMethod: true,
                UtangPaymentEnabled: true,
                CreditAllowRequested: false,
                HasValidCreditPolicy: false,
                HasSharedCatalog: true,
                HasResponsibleContact: true));

        Assert.True(allowOff.IsReady);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusNotApplicable,
            allowOff.Requirements.Single(r => r.Code == ConnectedSupplierCommerceReadiness.CreditPolicy).Status);

        var withoutCredit = ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: true,
                PickupEnabled: true,
                DeliveryEnabled: true,
                PickupConfigured: true,
                DeliveryConfigured: true,
                HasAcceptedPaymentMethod: true,
                UtangPaymentEnabled: true,
                CreditAllowRequested: true,
                HasValidCreditPolicy: false,
                HasSharedCatalog: true,
                HasResponsibleContact: true));

        Assert.False(withoutCredit.IsReady);
        Assert.Equal(
            ConnectedSupplierCommerceReadiness.StatusMissing,
            withoutCredit.Requirements.Single(r => r.Code == ConnectedSupplierCommerceReadiness.CreditPolicy).Status);

        var withCredit = ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: true,
                PickupEnabled: true,
                DeliveryEnabled: true,
                PickupConfigured: true,
                DeliveryConfigured: true,
                HasAcceptedPaymentMethod: true,
                UtangPaymentEnabled: true,
                CreditAllowRequested: true,
                HasValidCreditPolicy: true,
                HasSharedCatalog: true,
                HasResponsibleContact: true));

        Assert.True(withCredit.IsReady);
        Assert.Equal(
            [
                ConnectedSupplierCommerceReadiness.FulfillmentPickup,
                ConnectedSupplierCommerceReadiness.FulfillmentDelivery
            ],
            withCredit.SupportedFulfillmentMethods);
    }

    [Fact]
    public void Missing_branch_payment_catalog_or_contact_blocks_readiness()
    {
        var result = ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: false,
                PickupEnabled: false,
                DeliveryEnabled: false,
                PickupConfigured: false,
                DeliveryConfigured: false,
                HasAcceptedPaymentMethod: false,
                UtangPaymentEnabled: false,
                CreditAllowRequested: false,
                HasValidCreditPolicy: false,
                HasSharedCatalog: false,
                HasResponsibleContact: false));

        Assert.False(result.IsReady);
        Assert.Empty(result.SupportedFulfillmentMethods);
        Assert.Contains(result.Requirements, r =>
            r.Code == ConnectedSupplierCommerceReadiness.SellingBranch
            && r.Status == ConnectedSupplierCommerceReadiness.StatusMissing);
        Assert.Contains(result.Requirements, r =>
            r.Code == ConnectedSupplierCommerceReadiness.FulfillmentMethod
            && r.Status == ConnectedSupplierCommerceReadiness.StatusMissing);
        Assert.Contains(result.Requirements, r =>
            r.Code == ConnectedSupplierCommerceReadiness.PaymentMethods
            && r.Status == ConnectedSupplierCommerceReadiness.StatusMissing);
        Assert.Contains(result.Requirements, r =>
            r.Code == ConnectedSupplierCommerceReadiness.SharedCatalog
            && r.Status == ConnectedSupplierCommerceReadiness.StatusMissing);
        Assert.Contains(result.Requirements, r =>
            r.Code == ConnectedSupplierCommerceReadiness.ResponsibleContact
            && r.Status == ConnectedSupplierCommerceReadiness.StatusMissing);
    }

    [Theory]
    [InlineData(CatalogSharingMode.SelectedOnly, 10, 0, 0, false)]
    [InlineData(CatalogSharingMode.SelectedOnly, 10, 1, 0, true)]
    [InlineData(CatalogSharingMode.AllEligible, 0, 0, 0, false)]
    [InlineData(CatalogSharingMode.AllEligible, 5, 0, 5, false)]
    [InlineData(CatalogSharingMode.AllEligible, 5, 0, 1, true)]
    public void Shared_catalog_respects_sharing_mode(
        CatalogSharingMode mode,
        int eligible,
        int explicitShared,
        int excluded,
        bool expected)
    {
        Assert.Equal(
            expected,
            ConnectedSupplierCommerceReadiness.HasSharedCatalog(mode, eligible, explicitShared, excluded));
    }

    [Fact]
    public void Responsible_contact_accepts_name_phone_email_or_member()
    {
        Assert.True(ConnectedSupplierCommerceReadiness.HasResponsibleContact(
            "Ana", null, null, RelationshipContactSource.Custom, null));
        Assert.True(ConnectedSupplierCommerceReadiness.HasResponsibleContact(
            null, "0917", null, RelationshipContactSource.Custom, null));
        Assert.True(ConnectedSupplierCommerceReadiness.HasResponsibleContact(
            null, null, "a@b.c", RelationshipContactSource.Custom, null));
        Assert.True(ConnectedSupplierCommerceReadiness.HasResponsibleContact(
            null, null, null, RelationshipContactSource.OrganizationMember, Guid.NewGuid()));
        Assert.False(ConnectedSupplierCommerceReadiness.HasResponsibleContact(
            null, null, null, RelationshipContactSource.Custom, null));
    }
}
