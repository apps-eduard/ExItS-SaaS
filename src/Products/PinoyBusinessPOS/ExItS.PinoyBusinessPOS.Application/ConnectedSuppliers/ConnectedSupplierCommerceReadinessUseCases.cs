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
    string DefaultPaymentTiming = "PayBeforeFulfillment",
    /// <summary>Organization Offer Delivery master switch.</summary>
    bool OrgOfferDelivery = false,
    /// <summary>Selected supplier branch Pickup enabled+ready.</summary>
    bool BranchPickupReady = false,
    /// <summary>Selected supplier branch Delivery enabled+ready (config), independent of Offer Delivery.</summary>
    bool BranchDeliveryReady = false,
    /// <summary>Relationship CustomerDeliveryOverride = Block.</summary>
    bool RelationshipDeliveryBlocked = false,
    /// <summary>
    /// Buyer-safe Delivery unavailability reason when Delivery is not selectable:
    /// OrgOfferOff | BranchNotReady | RelationshipBlocked.
    /// </summary>
    string? DeliveryUnavailableReason = null,
    /// <summary>Buyer-safe Pickup unavailability reason: BranchNotReady when not selectable.</summary>
    string? PickupUnavailableReason = null);

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

        var snapshot = await EvaluateSnapshotAsync(relationship, cancellationToken).ConfigureAwait(false);
        var forBuyer = ApplyBuyerDeliveryFilter(snapshot);
        forBuyer = forBuyer with
        {
            Result = ConnectedSupplierCommerceReadiness.EnsureBuyerHasUsableMethod(forBuyer.Result),
        };
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

        var snapshot = await EvaluateSnapshotAsync(relationship, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<ConnectedSupplierCommerceReadinessDto>.Success(
            await ToDtoAsync(relationship, snapshot, includeRequirements: true, cancellationToken)
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
        var snapshot = await EvaluateSnapshotAsync(relationship, cancellationToken).ConfigureAwait(false);
        var evaluated = snapshot.Result;
        if (forBuyerMessage)
        {
            snapshot = ApplyBuyerDeliveryFilter(snapshot);
            evaluated = ConnectedSupplierCommerceReadiness.EnsureBuyerHasUsableMethod(snapshot.Result);
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

        var snapshot = await EvaluateSnapshotAsync(relationship, cancellationToken).ConfigureAwait(false);
        var forBuyer = ApplyBuyerDeliveryFilter(snapshot);
        if (forBuyer.Options.DeliverySelectable)
        {
            return ApplicationResult.Success();
        }

        return ApplicationResult.Failure(
            ConnectedSupplierErrorCodes.CommerceNotReady,
            forBuyerMessage
                ? forBuyer.Options.DeliveryUnavailableReason switch
                {
                    ConnectedSupplierCommerceReadiness.DeliveryUnavailableRelationshipBlocked
                        => "Delivery is not available for this business relationship.",
                    ConnectedSupplierCommerceReadiness.DeliveryUnavailableOrgOfferOff
                        => "Delivery is currently disabled by this supplier.",
                    _
                        => "Delivery is not available for this supplier connection.",
                }
                : "Delivery is not available for this business customer.");
    }

    public async Task<ConnectedSupplierCommerceReadiness.Result> EvaluateAsync(
        ConnectedSupplierRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        var snapshot = await EvaluateSnapshotAsync(relationship, cancellationToken).ConfigureAwait(false);
        return snapshot.Result;
    }

    internal sealed record EvaluationSnapshot(
        ConnectedSupplierCommerceReadiness.Result Result,
        ConnectedSupplierCommerceReadiness.BuyerFulfillmentOptions Options);

    private async Task<EvaluationSnapshot> EvaluateSnapshotAsync(
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
        bool deliveryEnabled = false;
        bool deliveryReady = false;
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
                deliveryEnabled = branch.DeliveryEnabled;
                deliveryReady = branch.DeliveryReady;
            }
        }

        var deliveryConfigured = deliveryEnabled && deliveryReady;

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

        var result = ConnectedSupplierCommerceReadiness.Evaluate(
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

        var options = ConnectedSupplierCommerceReadiness.ResolveBuyerFulfillmentOptions(
            orgOfferDelivery: orgOfferDelivery,
            branchPickupEnabled: pickupEnabled,
            branchPickupReady: pickupConfigured,
            branchDeliveryEnabled: deliveryEnabled,
            branchDeliveryReady: deliveryReady,
            customerOverride: relationship.CustomerDeliveryOverride);

        // Align selectable methods with Evaluate when relationship does not Block
        // (Evaluate already applied org+branch). Options refine Block + reasons.
        return new EvaluationSnapshot(result, options);
    }

    /// <summary>
    /// Buyer-facing methods: strip Delivery when the customer override Blocks it.
    /// Uses canonical EffectiveDeliveryAllowance (org Offer Delivery + branch ready + override).
    /// </summary>
    internal static EvaluationSnapshot ApplyBuyerDeliveryFilter(EvaluationSnapshot snapshot)
    {
        var options = snapshot.Options;
        if (options.DeliverySelectable)
        {
            return snapshot;
        }

        var filtered = snapshot.Result.SupportedFulfillmentMethods
            .Where(m => !m.Equals(
                ConnectedSupplierCommerceReadiness.FulfillmentDelivery,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (filtered.Length == snapshot.Result.SupportedFulfillmentMethods.Count)
        {
            // Delivery was never listed (org/branch) — keep result; options still carry reason.
            return snapshot with { Options = options with { SelectableMethods = filtered } };
        }

        if (filtered.Length > 0)
        {
            return snapshot with
            {
                Result = new ConnectedSupplierCommerceReadiness.Result(
                    snapshot.Result.IsReady,
                    filtered,
                    snapshot.Result.Requirements),
                Options = options with { SelectableMethods = filtered },
            };
        }

        // Delivery was the only usable method and the customer override blocks it.
        var requirements = snapshot.Result.Requirements
            .Select(r => r.Code == ConnectedSupplierCommerceReadiness.FulfillmentMethod
                ? r with
                {
                    Status = ConnectedSupplierCommerceReadiness.StatusMissing,
                    Detail = "Enable and finish Pickup and/or Delivery so buyers have at least one method.",
                }
                : r)
            .ToList();
        return snapshot with
        {
            Result = new ConnectedSupplierCommerceReadiness.Result(
                IsReady: false,
                filtered,
                requirements),
            Options = options with { SelectableMethods = filtered },
        };
    }

    /// <summary>
    /// Legacy helper: strips Delivery from an Evaluate result when the relationship Blocks it.
    /// Prefers reconstructing options from listed methods (Evaluate already applied org+branch).
    /// </summary>
    internal static ConnectedSupplierCommerceReadiness.Result ApplyBuyerDeliveryFilter(
        ConnectedSupplierCommerceReadiness.Result evaluated,
        ConnectedSupplierRelationship relationship)
    {
        var hasPickup = evaluated.SupportedFulfillmentMethods.Contains(
            ConnectedSupplierCommerceReadiness.FulfillmentPickup,
            StringComparer.OrdinalIgnoreCase);
        var hasDelivery = evaluated.SupportedFulfillmentMethods.Contains(
            ConnectedSupplierCommerceReadiness.FulfillmentDelivery,
            StringComparer.OrdinalIgnoreCase);
        // When Delivery is listed, org offer + branch ready were both true at Evaluate time.
        var options = ConnectedSupplierCommerceReadiness.ResolveBuyerFulfillmentOptions(
            orgOfferDelivery: hasDelivery,
            branchPickupEnabled: hasPickup,
            branchPickupReady: hasPickup,
            branchDeliveryEnabled: hasDelivery,
            branchDeliveryReady: hasDelivery,
            customerOverride: relationship.CustomerDeliveryOverride);

        return ApplyBuyerDeliveryFilter(new EvaluationSnapshot(evaluated, options)).Result;
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
        EvaluationSnapshot snapshot,
        bool includeRequirements,
        CancellationToken cancellationToken)
    {
        var evaluated = snapshot.Result;
        var options = snapshot.Options;
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

        // SelectableMethods is the canonical Block-aware projection for Create PO + submit.
        return new ConnectedSupplierCommerceReadinessDto(
            relationship.Id.Value,
            evaluated.IsReady,
            options.SelectableMethods,
            requirements,
            blockerCategories,
            timing.AllowPayBeforeFulfillment,
            timing.AllowPayOnDeliveryOrReceipt,
            timing.AllowSupplierCredit,
            timing.DefaultPaymentTiming.ToString(),
            options.OrgOfferDelivery,
            options.BranchPickupReady,
            options.BranchDeliveryReady,
            options.RelationshipDeliveryBlocked,
            options.DeliveryUnavailableReason,
            options.PickupUnavailableReason);
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
