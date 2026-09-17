namespace ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

/// <summary>
/// Pure evaluation of whether a connected supplier is ready to accept buyer POs.
/// Fulfillment mirrors seller Branch PO readiness: an effective branch method is
/// enabled+ready. PO fulfillment is satisfied when at least one branch channel
/// (Pickup or Delivery) is usable — incomplete sibling channels do not block.
/// Organization Offer Delivery only controls the DeliveryConfig checklist row;
/// it does not gate FulfillmentMethod or listing Delivery when the branch channel
/// is already enabled+ready. DeliveryEnabled here means organization Offer Delivery
/// (not per-customer override).
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

    /// <summary>Buyer-safe blocker categories (no internal field names).</summary>
    public const string BuyerBlockerFulfillment = "Fulfillment";
    public const string BuyerBlockerNoUsableMethod = "NoUsableMethod";
    public const string BuyerBlockerPayment = "Payment";
    public const string BuyerBlockerCatalog = "Catalog";
    public const string BuyerBlockerContact = "Contact";
    public const string BuyerBlockerCredit = "Credit";

    private static readonly string[] BuyerBlockerOrder =
    [
        BuyerBlockerFulfillment,
        BuyerBlockerNoUsableMethod,
        BuyerBlockerPayment,
        BuyerBlockerCatalog,
        BuyerBlockerContact,
        BuyerBlockerCredit,
    ];

    public sealed record Input(
        bool HasSellingBranch,
        bool PickupEnabled,
        /// <summary>Organization Offer Delivery is ON.</summary>
        bool DeliveryEnabled,
        /// <summary>Branch PickupEnabled and Platform PickupReady (setup, not open-now).</summary>
        bool PickupConfigured,
        /// <summary>
        /// Branch DeliveryEnabled and Platform DeliveryReady (setup). Org Offer Delivery is separate.
        /// </summary>
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
        // Branch channels (same as seller Branch PO panel): enabled + ready.
        var pickupUsable = input.PickupEnabled && input.PickupConfigured;
        var branchDeliveryUsable = input.DeliveryConfigured;

        var methods = new List<string>(2);
        if (pickupUsable)
        {
            methods.Add(FulfillmentPickup);
        }

        if (branchDeliveryUsable)
        {
            methods.Add(FulfillmentDelivery);
        }

        // At least one branch channel — Pickup-only or Delivery-only both satisfy.
        var hasBranchFulfillmentMethod = pickupUsable || branchDeliveryUsable;

        var requirements = new List<Requirement>
        {
            Item(
                SellingBranch,
                input.HasSellingBranch,
                "Selling/fulfillment location",
                "Choose the branch that fulfills purchase orders for this connection."),
            Item(
                FulfillmentMethod,
                input.HasSellingBranch && hasBranchFulfillmentMethod,
                "Fulfillment methods",
                "Enable and finish Pickup and/or Delivery so at least one method is ready."),
            Conditional(
                DeliveryConfig,
                applicable: input.DeliveryEnabled,
                complete: branchDeliveryUsable,
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

        // PickupConfig/DeliveryConfig stay visible on the supplier checklist, but overall
        // readiness follows FulfillmentMethod (at least one branch channel).
        var isReady = requirements.All(r =>
            r.Code is PickupConfig or DeliveryConfig
            || r.Status is StatusComplete or StatusNotApplicable);

        return new Result(isReady, methods, requirements);
    }

    /// <summary>
    /// Buyer projection: if no method remains after filtering (e.g. customer Block on
    /// Delivery-only), surface NoUsableMethod.
    /// </summary>
    public static Result EnsureBuyerHasUsableMethod(Result evaluated)
    {
        if (evaluated.SupportedFulfillmentMethods.Count > 0)
        {
            return evaluated;
        }

        var requirements = evaluated.Requirements
            .Select(r => r.Code == FulfillmentMethod
                ? r with
                {
                    Status = StatusMissing,
                    Detail = "Enable and finish Pickup and/or Delivery so buyers have at least one method.",
                }
                : r)
            .ToList();

        return new Result(IsReady: false, evaluated.SupportedFulfillmentMethods, requirements);
    }

    /// <summary>
    /// Maps missing requirements to buyer-safe blocker categories.
    /// Never returns internal requirement codes, titles, or configuration details.
    /// </summary>
    public static IReadOnlyList<string> MapBuyerSafeBlockerCategories(Result result)
    {
        if (result.IsReady)
        {
            return Array.Empty<string>();
        }

        var hasUsableMethod = result.SupportedFulfillmentMethods.Count > 0;
        var present = new HashSet<string>(StringComparer.Ordinal);
        foreach (var requirement in result.Requirements)
        {
            if (requirement.Status != StatusMissing)
            {
                continue;
            }

            // Incomplete sibling channel must not surface when another method already works.
            if (hasUsableMethod
                && requirement.Code is PickupConfig or DeliveryConfig)
            {
                continue;
            }

            var category = requirement.Code switch
            {
                SellingBranch => BuyerBlockerFulfillment,
                PickupConfig or DeliveryConfig => BuyerBlockerFulfillment,
                FulfillmentMethod => BuyerBlockerNoUsableMethod,
                PaymentMethods => BuyerBlockerPayment,
                SharedCatalog => BuyerBlockerCatalog,
                ResponsibleContact => BuyerBlockerContact,
                CreditPolicy => BuyerBlockerCredit,
                _ => null,
            };
            if (category is not null)
            {
                present.Add(category);
            }
        }

        return BuyerBlockerOrder.Where(present.Contains).ToArray();
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
