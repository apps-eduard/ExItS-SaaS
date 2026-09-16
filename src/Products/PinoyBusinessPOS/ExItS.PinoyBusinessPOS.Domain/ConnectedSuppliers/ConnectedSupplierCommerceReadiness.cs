namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// Pure evaluation of whether a connected supplier is ready to accept buyer POs.
/// Conditional requirements: Delivery/Pickup/Utang config only when that capability is enabled.
/// DeliveryEnabled here means organization Offer Delivery (not per-customer override).
/// </summary>
public static class ConnectedSupplierCommerceReadiness
{
    public const string SellingBranch = "SellingBranch";
    public const string FulfillmentMethod = "FulfillmentMethod";
    public const string DeliveryConfig = "DeliveryConfig";
    public const string PickupConfig = "PickupConfig";
    public const string PaymentMethods = "PaymentMethods";
    public const string SharedCatalog = "SharedCatalog";
    public const string ResponsibleContact = "ResponsibleContact";
    public const string CreditPolicy = "CreditPolicy";

    public const string StatusComplete = "Complete";
    public const string StatusMissing = "Missing";
    public const string StatusNotApplicable = "NotApplicable";

    public const string FulfillmentPickup = "Pickup";
    public const string FulfillmentDelivery = "Delivery";

    public sealed record Input(
        bool HasSellingBranch,
        bool PickupEnabled,
        /// <summary>Organization Offer Delivery is ON.</summary>
        bool DeliveryEnabled,
        bool PickupConfigured,
        /// <summary>Delivery-capable selling branch is ready (config preserved even when org OFF).</summary>
        bool DeliveryConfigured,
        bool HasAcceptedPaymentMethod,
        bool UtangPaymentEnabled,
        /// <summary>
        /// Seller turned Allow credit ON for this connection (PendingApproval or Approved).
        /// When false (OFF / NotConfigured / Disabled), CreditPolicy requirement is N/A.
        /// </summary>
        bool CreditAllowRequested,
        bool HasValidCreditPolicy,
        bool HasSharedCatalog,
        bool HasResponsibleContact);

    public sealed record Requirement(string Code, string Status, string Title, string? Detail = null);

    public sealed record Result(
        bool IsReady,
        IReadOnlyList<string> SupportedFulfillmentMethods,
        IReadOnlyList<Requirement> Requirements);

    public static Result Evaluate(Input input)
    {
        // Buyer-usable methods: only include Delivery when org offers it AND branch is ready.
        var methods = new List<string>(2);
        if (input.PickupEnabled)
        {
            methods.Add(FulfillmentPickup);
        }

        if (input.DeliveryEnabled && input.DeliveryConfigured)
        {
            methods.Add(FulfillmentDelivery);
        }

        // FulfillmentMethod: at least one offered method (Pickup enabled OR org Delivery ON).
        // Pickup can independently satisfy this row; DeliveryConfig is separate when org Delivery ON.
        var hasOfferedMethod = input.PickupEnabled || input.DeliveryEnabled;

        var requirements = new List<Requirement>
        {
            Item(
                SellingBranch,
                input.HasSellingBranch,
                "Selling/fulfillment location",
                "Choose the branch that fulfills purchase orders for this connection."),
            Item(
                FulfillmentMethod,
                input.HasSellingBranch && hasOfferedMethod,
                "Fulfillment methods",
                "Enable Pickup and/or turn on Offer Delivery for the organization."),
            Conditional(
                DeliveryConfig,
                applicable: input.DeliveryEnabled,
                complete: input.DeliveryConfigured,
                title: "Delivery configuration",
                detail: "Finish delivery setup for a delivery-capable selling branch."),
            Conditional(
                PickupConfig,
                applicable: input.PickupEnabled,
                complete: input.PickupConfigured,
                title: "Pickup configuration",
                detail: "Finish pickup setup for the selling branch."),
            Item(
                PaymentMethods,
                input.HasAcceptedPaymentMethod,
                "Accepted payment methods",
                "Enable at least one payment method buyers can use on purchase orders."),
            Item(
                SharedCatalog,
                input.HasSharedCatalog,
                "Shared catalog",
                "Share at least one product with this business customer."),
            Item(
                ResponsibleContact,
                input.HasResponsibleContact,
                "Responsible contact",
                "Set a contact person, phone, or email for this connection."),
            Conditional(
                CreditPolicy,
                // Utang org method alone does not force credit; only when Allow credit is ON.
                applicable: input.UtangPaymentEnabled && input.CreditAllowRequested,
                complete: input.HasValidCreditPolicy,
                title: "Credit setup",
                detail: "Complete credit terms before this business can use Utang."),
        };

        var isReady = requirements.All(r =>
            r.Status is StatusComplete or StatusNotApplicable);

        return new Result(isReady, methods, requirements);
    }

    public static bool HasResponsibleContact(
        string? contactPersonName,
        string? contactPhone,
        string? contactEmail,
        RelationshipContactSource contactSource,
        Guid? organizationMemberId) =>
        !string.IsNullOrWhiteSpace(contactPersonName)
        || !string.IsNullOrWhiteSpace(contactPhone)
        || !string.IsNullOrWhiteSpace(contactEmail)
        || (contactSource == RelationshipContactSource.OrganizationMember
            && organizationMemberId is Guid id
            && id != Guid.Empty);

    public static bool HasSharedCatalog(
        CatalogSharingMode mode,
        int eligibleCount,
        int explicitSharedCount,
        int excludedCount)
    {
        if (mode == CatalogSharingMode.AllEligible)
        {
            return Math.Max(0, eligibleCount - excludedCount) > 0;
        }

        return explicitSharedCount > 0;
    }

    private static Requirement Item(string code, bool complete, string title, string detail) =>
        new(code, complete ? StatusComplete : StatusMissing, title, detail);

    private static Requirement Conditional(
        string code,
        bool applicable,
        bool complete,
        string title,
        string detail)
    {
        if (!applicable)
        {
            return new Requirement(code, StatusNotApplicable, title, null);
        }

        return Item(code, complete, title, detail);
    }
}
