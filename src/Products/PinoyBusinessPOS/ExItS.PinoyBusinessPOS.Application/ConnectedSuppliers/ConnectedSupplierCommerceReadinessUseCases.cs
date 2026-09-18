using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

public sealed record ConnectedSupplierCommerceReadinessRequirementDto(
    string Code,
    string Status,
    string Title,
    string? Detail = null,
    string? ActionPath = null);

public sealed record ConnectedSupplierCommerceReadinessDto(
    Guid RelationshipId,
    bool IsReady,
    IReadOnlyList<string> SupportedFulfillmentMethods,
    IReadOnlyList<ConnectedSupplierCommerceReadinessRequirementDto>? Requirements = null,
    /// <summary>
    /// Buyer-safe blocker categories only (Fulfillment, Payment, …). Empty when ready.
    /// </summary>
    IReadOnlyList<string>? BlockerCategories = null,
    bool AllowPayBeforeFulfillment = true,
    bool AllowPayOnDeliveryOrReceipt = true,
    bool AllowSupplierCredit = false,
    string DefaultPaymentTiming = "PayBeforeFulfillment");

/// <summary>
/// Authoritative commerce readiness for connected supplier PO acceptance.
/// Buyer projections omit requirement details; supplier projections include the checklist.
/// </summary>
public sealed class ConnectedSupplierCommerceReadinessService
{
    public const string BuyerNotReadyMessage =
        "This supplier is not currently ready to accept purchase orders from your organization. Please contact your supplier.";

    public const string SupplierNotReadyMessage =
        "Complete supplier commerce setup before accepting purchase orders.";

    private static readonly string[] PoPaymentMethodCodes =
    [
        PaymentMethodCatalog.Cash,
        PaymentMethodCatalog.BankTransfer,
        PaymentMethodCatalog.ManualGCash,
        PaymentMethodCatalog.Utang,
    ];

    private readonly IConnectedSupplierRelationshipRepository _relationships;
    private readonly IConnectedBuyerProductShareRepository _shares;
    private readonly ICustomerOrderBranchDirectory _branches;
    private readonly IOrganizationPaymentMethodSettingRepository _paymentSettings;
    private readonly IBusinessCustomerCreditPolicyRepository _creditPolicies;
    private readonly IOrganizationFulfillmentSettingsRepository _fulfillmentSettings;
    private readonly IOrganizationConnectedCommerceSettingsRepository? _connectedCommerceSettings;
    private readonly IPosCommercialAccessAccessor _access;

    public ConnectedSupplierCommerceReadinessService(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        ICustomerOrderBranchDirectory branches,
        IOrganizationPaymentMethodSettingRepository paymentSettings,
        IBusinessCustomerCreditPolicyRepository creditPolicies,
        IPosCommercialAccessAccessor access,
        IOrganizationFulfillmentSettingsRepository? fulfillmentSettings = null,
        IOrganizationConnectedCommerceSettingsRepository? connectedCommerceSettings = null)
    {
        _relationships = relationships;
        _shares = shares;
        _branches = branches;
        _paymentSettings = paymentSettings;
        _creditPolicies = creditPolicies;
        _fulfillmentSettings = fulfillmentSettings ?? new NullOrganizationFulfillmentSettingsRepository();
        _connectedCommerceSettings = connectedCommerceSettings;
        _access = access;
    }

    public async Task<ApplicationResult<ConnectedSupplierCommerceReadinessDto>> GetForBuyerAsync(
        Guid buyerOrganizationId,
        Guid relationshipId,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewPurchasing);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierCommerceReadinessDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var relationship = await _relationships
            .GetAsync(ConnectedSupplierRelationshipId.From(relationshipId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null
            || relationship.BuyerOrganizationId != PosOrganizationId.From(buyerOrganizationId)
            || relationship.Status != ConnectedSupplierRelationshipStatus.Active)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierCommerceReadinessDto>(
                ConnectedSupplierErrorCodes.NotFound, "Relationship was not found.");
        }

        var evaluated = await EvaluateAsync(relationship, cancellationToken).ConfigureAwait(false);
        var forBuyer = ApplyBuyerDeliveryFilter(evaluated, relationship);
        forBuyer = ConnectedSupplierCommerceReadiness.EnsureBuyerHasUsableMethod(forBuyer);
        return ApplicationResult<ConnectedSupplierCommerceReadinessDto>.Success(
            await ToDtoAsync(relationship, forBuyer, includeRequirements: false, cancellationToken)
                .ConfigureAwait(false));
    }

    public async Task<ApplicationResult<ConnectedSupplierCommerceReadinessDto>> GetForSupplierAsync(
        Guid supplierOrganizationId,
        Guid connectionId,
        CancellationToken cancellationToken = default)
    {
        var gate = ConnectedSupplierUseCaseGuard.Access(_access, UtangCapability.ViewSuppliers);
        if (!gate.IsSuccess)
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierCommerceReadinessDto>(
                gate.ErrorCode!, gate.ErrorMessage!);
        }

        var relationship = await _relationships
            .GetAsync(ConnectedSupplierRelationshipId.From(connectionId), cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null
            || relationship.SupplierOrganizationId != PosOrganizationId.From(supplierOrganizationId))
        {
            return ConnectedSupplierUseCaseGuard.Failure<ConnectedSupplierCommerceReadinessDto>(
                ConnectedSupplierErrorCodes.NotFound, "Business customer relationship was not found.");
        }

        var evaluated = await EvaluateAsync(relationship, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<ConnectedSupplierCommerceReadinessDto>.Success(
            await ToDtoAsync(relationship, evaluated, includeRequirements: true, cancellationToken)
                .ConfigureAwait(false));
    }

    /// <summary>
    /// Server gate for buyer submit / supplier accept. Returns failure when not ready.
    /// Buyer callers must not surface requirement details.
    /// </summary>
    public async Task<ApplicationResult> EnsureReadyAsync(
        ConnectedSupplierRelationship relationship,
        bool forBuyerMessage,
        CancellationToken cancellationToken = default)
    {
        var evaluated = await EvaluateAsync(relationship, cancellationToken).ConfigureAwait(false);
        if (forBuyerMessage)
        {
            evaluated = ApplyBuyerDeliveryFilter(evaluated, relationship);
            evaluated = ConnectedSupplierCommerceReadiness.EnsureBuyerHasUsableMethod(evaluated);
        }

        if (evaluated.IsReady)
        {
            return ApplicationResult.Success();
        }

        return ApplicationResult.Failure(
            ConnectedSupplierErrorCodes.CommerceNotReady,
            forBuyerMessage ? BuyerNotReadyMessage : SupplierNotReadyMessage);
    }

    /// <summary>
    /// Server gate for buyer selecting Delivery. Rejects when EffectiveDelivery is false.
    /// </summary>
    public async Task<ApplicationResult> EnsureFulfillmentMethodAllowedAsync(
        ConnectedSupplierRelationship relationship,
        string? fulfillmentMethod,
        bool forBuyerMessage,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fulfillmentMethod))
        {
            return ApplicationResult.Success();
        }

        var normalized = fulfillmentMethod.Trim();
        if (!normalized.Equals(ConnectedSupplierCommerceReadiness.FulfillmentDelivery, StringComparison.OrdinalIgnoreCase))
        {
            return ApplicationResult.Success();
        }

        var evaluated = await EvaluateAsync(relationship, cancellationToken).ConfigureAwait(false);
        var forBuyer = ApplyBuyerDeliveryFilter(evaluated, relationship);
        if (forBuyer.SupportedFulfillmentMethods.Contains(
                ConnectedSupplierCommerceReadiness.FulfillmentDelivery,
                StringComparer.OrdinalIgnoreCase))
        {
            return ApplicationResult.Success();
        }

        return ApplicationResult.Failure(
            ConnectedSupplierErrorCodes.CommerceNotReady,
            forBuyerMessage
                ? "Delivery is not available for this supplier connection."
                : "Delivery is not available for this business customer.");
    }

    public async Task<ConnectedSupplierCommerceReadiness.Result> EvaluateAsync(
        ConnectedSupplierRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        var supplierOrg = relationship.SupplierOrganizationId;
        var hasBranch = relationship.SupplierBranchId is Guid branchId && branchId != Guid.Empty;

        var fulfillmentSettings = await _fulfillmentSettings
            .GetAsync(supplierOrg, cancellationToken)
            .ConfigureAwait(false);
        // Missing row = Offer Delivery OFF (default).
        var orgOfferDelivery = fulfillmentSettings?.OfferDelivery == true;

        bool pickupEnabled = false;
        bool pickupConfigured = false;
        bool deliveryConfigured = false;
        if (hasBranch)
        {
            var branch = await _branches
                .GetCommerceBranchAsync(
                    supplierOrg.Value,
                    relationship.SupplierBranchId!.Value,
                    relationship.SupplierPublicOrganizationIdSnapshot,
                    cancellationToken)
                .ConfigureAwait(false);
            if (branch is not null)
            {
                pickupEnabled = branch.PickupEnabled;
                // Setup completeness (not open-now operational). Closed store must not
                // keep Supplier Readiness stuck on Pickup configuration.
                pickupConfigured = branch.PickupReady;
                // Canonical: enabled + Platform DeliveryReady (same as seller Branch PO panel).
                // Do not use DeliveryOperational (open-now) or ad-hoc policy/instructions checks.
                deliveryConfigured = branch.DeliveryEnabled && branch.DeliveryReady;
            }
        }

        var paymentSettings = await _paymentSettings
            .ListByOrganizationAsync(supplierOrg, cancellationToken)
            .ConfigureAwait(false);
        var enabledPoMethods = ResolveEnabledPoPaymentMethods(paymentSettings, _access);
        var hasPayment = enabledPoMethods.Count > 0;
        var utangEnabled = enabledPoMethods.Contains(PaymentMethodCatalog.Utang, StringComparer.OrdinalIgnoreCase);

        var creditAllowRequested = false;
        var hasValidCredit = false;
        if (utangEnabled)
        {
            var policy = await _creditPolicies
                .GetBySellerAndBuyerAsync(supplierOrg, relationship.BuyerOrganizationId, cancellationToken)
                .ConfigureAwait(false);
            // Allow credit ON = PendingApproval (needs setup) or Approved (complete).
            // OFF = missing / Disabled / NotConfigured → CreditPolicy N/A.
            creditAllowRequested = policy is not null
                && (policy.Status == CustomerCreditPolicyStatus.PendingApproval
                    || policy.Status == CustomerCreditPolicyStatus.Approved);
            hasValidCredit = policy is not null && policy.PermitsNewUtang;
        }

        var eligibleCount = await _shares
            .CountEligibleSupplierProductsAsync(supplierOrg, cancellationToken)
            .ConfigureAwait(false);
        var statsMap = await _shares
            .ListShareStatsByRelationshipsAsync([relationship.Id.Value], cancellationToken)
            .ConfigureAwait(false);
        var stats = statsMap.GetValueOrDefault(relationship.Id.Value, new BuyerRelationshipShareStats(0, 0, 0));
        var hasCatalog = ConnectedSupplierCommerceReadiness.HasSharedCatalog(
            relationship.CatalogSharingMode,
            eligibleCount,
            stats.ExplicitSharedCount,
            stats.ExcludedCount);

        var hasContact = ConnectedSupplierCommerceReadiness.HasResponsibleContact(
            relationship.ContactPersonName,
            relationship.ContactPhone,
            relationship.ContactEmail,
            relationship.ContactSource,
            relationship.OrganizationMemberId);

        return ConnectedSupplierCommerceReadiness.Evaluate(
            new ConnectedSupplierCommerceReadiness.Input(
                HasSellingBranch: hasBranch,
                PickupEnabled: pickupEnabled,
                DeliveryEnabled: orgOfferDelivery,
                PickupConfigured: pickupConfigured,
                DeliveryConfigured: deliveryConfigured,
                HasAcceptedPaymentMethod: hasPayment,
                UtangPaymentEnabled: utangEnabled,
                CreditAllowRequested: creditAllowRequested,
                HasValidCreditPolicy: hasValidCredit,
                HasSharedCatalog: hasCatalog,
                HasResponsibleContact: hasContact));
    }

    /// <summary>
    /// Buyer-facing methods: strip Delivery when the customer override Blocks it.
    /// Branch Delivery enabled+ready is enough to list Delivery (org Offer Delivery is not
    /// required for method availability). When Delivery was the only usable method and
    /// Block applies, the buyer projection becomes not-ready with NoUsableMethod.
    /// </summary>
    internal static ConnectedSupplierCommerceReadiness.Result ApplyBuyerDeliveryFilter(
        ConnectedSupplierCommerceReadiness.Result evaluated,
        ConnectedSupplierRelationship relationship)
    {
        var readyBranch = evaluated.SupportedFulfillmentMethods.Contains(
            ConnectedSupplierCommerceReadiness.FulfillmentDelivery,
            StringComparer.OrdinalIgnoreCase);
        // Branch channel is the offer signal for buyer method listing; org Offer Delivery
        // remains a separate supplier checklist concern (DeliveryConfig).
        var allowed = EffectiveDeliveryAllowance.IsAllowed(
            orgOfferDelivery: readyBranch,
            readyDeliveryBranchExists: readyBranch,
            relationship.CustomerDeliveryOverride);

        if (allowed
            || !readyBranch)
        {
            return evaluated;
        }

        var filtered = evaluated.SupportedFulfillmentMethods
            .Where(m => !m.Equals(
                ConnectedSupplierCommerceReadiness.FulfillmentDelivery,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (filtered.Length > 0)
        {
            // Pickup (or another non-delivery method) still works for this buyer.
            return new ConnectedSupplierCommerceReadiness.Result(
                evaluated.IsReady,
                filtered,
                evaluated.Requirements);
        }

        // Delivery was the only usable method and the customer override blocks it.
        var requirements = evaluated.Requirements
            .Select(r => r.Code == ConnectedSupplierCommerceReadiness.FulfillmentMethod
                ? r with
                {
                    Status = ConnectedSupplierCommerceReadiness.StatusMissing,
                    Detail = "Enable and finish Pickup and/or Delivery so buyers have at least one method.",
                }
                : r)
            .ToList();
        return new ConnectedSupplierCommerceReadiness.Result(
            IsReady: false,
            filtered,
            requirements);
    }

    private sealed class NullOrganizationFulfillmentSettingsRepository : IOrganizationFulfillmentSettingsRepository
    {
        public Task<OrganizationFulfillmentSettings?> GetAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganizationFulfillmentSettings?>(null);

        public Task AddAsync(
            OrganizationFulfillmentSettings settings,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task UpdateAsync(
            OrganizationFulfillmentSettings settings,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    internal static IReadOnlyList<string> ResolveEnabledPoPaymentMethods(
        IReadOnlyList<OrganizationPaymentMethodSetting> settings,
        IPosCommercialAccessAccessor access)
    {
        var byCode = settings.ToDictionary(s => s.MethodCode, StringComparer.OrdinalIgnoreCase);
        var featureCodes = access.Current.EnabledFeatureCodes;
        var status = access.Current.SubscriptionStatus;
        var enabled = new List<string>(PoPaymentMethodCodes.Length);

        foreach (var code in PoPaymentMethodCodes)
        {
            var def = PaymentMethodCatalog.Find(code);
            if (def is null || def.Availability == PaymentMethodAvailability.ComingSoon)
            {
                continue;
            }

            var entitled = PaymentCapabilityPolicy.HasCapabilityOrDefaultBasic(
                def.RequiredCapability,
                status,
                featureCodes);
            byCode.TryGetValue(code, out var setting);
            var isEnabled = setting?.IsEnabled ?? entitled;
            if (entitled && isEnabled)
            {
                enabled.Add(def.MethodCode);
            }
        }

        return enabled;
    }

    private async Task<ConnectedSupplierCommerceReadinessDto> ToDtoAsync(
        ConnectedSupplierRelationship relationship,
        ConnectedSupplierCommerceReadiness.Result evaluated,
        bool includeRequirements,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ConnectedSupplierCommerceReadinessRequirementDto>? requirements = null;
        if (includeRequirements)
        {
            requirements = evaluated.Requirements.Select(r => new ConnectedSupplierCommerceReadinessRequirementDto(
                r.Code,
                r.Status,
                r.Title,
                r.Detail,
                ActionPathFor(r.Code))).ToList();
        }

        var blockerCategories = ConnectedSupplierCommerceReadiness.MapBuyerSafeBlockerCategories(evaluated);
        var settings = _connectedCommerceSettings is null
            ? OrganizationConnectedCommerceSettings.CreateDefault(relationship.SupplierOrganizationId, DateTimeOffset.UtcNow)
            : (await _connectedCommerceSettings
                .GetAsync(relationship.SupplierOrganizationId, cancellationToken)
                .ConfigureAwait(false)
                ?? OrganizationConnectedCommerceSettings.CreateDefault(
                    relationship.SupplierOrganizationId,
                    DateTimeOffset.UtcNow));
        var timing = ConnectedPoPaymentTimingResolver.Resolve(settings, relationship);

        return new ConnectedSupplierCommerceReadinessDto(
            relationship.Id.Value,
            evaluated.IsReady,
            evaluated.SupportedFulfillmentMethods,
            requirements,
            blockerCategories,
            timing.AllowPayBeforeFulfillment,
            timing.AllowPayOnDeliveryOrReceipt,
            timing.AllowSupplierCredit,
            timing.DefaultPaymentTiming.ToString());
    }

    private static string? ActionPathFor(string code) =>
        code switch
        {
            ConnectedSupplierCommerceReadiness.SellingBranch => "/suppliers/connected",
            ConnectedSupplierCommerceReadiness.FulfillmentMethod => "/org/branches",
            ConnectedSupplierCommerceReadiness.DeliveryConfig => "/org/branches",
            ConnectedSupplierCommerceReadiness.PickupConfig => "/org/branches",
            ConnectedSupplierCommerceReadiness.PaymentMethods => "/org/payment-methods",
            ConnectedSupplierCommerceReadiness.SharedCatalog => null,
            ConnectedSupplierCommerceReadiness.ResponsibleContact => null,
            ConnectedSupplierCommerceReadiness.CreditPolicy => null,
            _ => null,
        };
}

public sealed class GetBuyerConnectedSupplierCommerceReadiness(
    ConnectedSupplierCommerceReadinessService readiness)
{
    public Task<ApplicationResult<ConnectedSupplierCommerceReadinessDto>> ExecuteAsync(
        Guid orgId,
        Guid relationshipId,
        CancellationToken ct = default) =>
        readiness.GetForBuyerAsync(orgId, relationshipId, ct);
}

public sealed class GetSupplierConnectedSupplierCommerceReadiness(
    ConnectedSupplierCommerceReadinessService readiness)
{
    public Task<ApplicationResult<ConnectedSupplierCommerceReadinessDto>> ExecuteAsync(
        Guid orgId,
        Guid connectionId,
        CancellationToken ct = default) =>
        readiness.GetForSupplierAsync(orgId, connectionId, ct);
}
