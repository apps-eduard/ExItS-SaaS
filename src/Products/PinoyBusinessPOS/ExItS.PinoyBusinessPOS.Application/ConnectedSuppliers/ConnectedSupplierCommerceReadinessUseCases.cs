using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;
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
    IReadOnlyList<ConnectedSupplierCommerceReadinessRequirementDto>? Requirements = null);

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
    private readonly IPosCommercialAccessAccessor _access;

    public ConnectedSupplierCommerceReadinessService(
        IConnectedSupplierRelationshipRepository relationships,
        IConnectedBuyerProductShareRepository shares,
        ICustomerOrderBranchDirectory branches,
        IOrganizationPaymentMethodSettingRepository paymentSettings,
        IBusinessCustomerCreditPolicyRepository creditPolicies,
        IPosCommercialAccessAccessor access)
    {
        _relationships = relationships;
        _shares = shares;
        _branches = branches;
        _paymentSettings = paymentSettings;
        _creditPolicies = creditPolicies;
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
        return ApplicationResult<ConnectedSupplierCommerceReadinessDto>.Success(
            ToDto(relationship.Id.Value, evaluated, includeRequirements: false));
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
            ToDto(relationship.Id.Value, evaluated, includeRequirements: true));
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
        if (evaluated.IsReady)
        {
            return ApplicationResult.Success();
        }

        return ApplicationResult.Failure(
            ConnectedSupplierErrorCodes.CommerceNotReady,
            forBuyerMessage ? BuyerNotReadyMessage : SupplierNotReadyMessage);
    }

    public async Task<ConnectedSupplierCommerceReadiness.Result> EvaluateAsync(
        ConnectedSupplierRelationship relationship,
        CancellationToken cancellationToken = default)
    {
        var supplierOrg = relationship.SupplierOrganizationId;
        var hasBranch = relationship.SupplierBranchId is Guid branchId && branchId != Guid.Empty;

        bool pickupEnabled = false;
        bool deliveryEnabled = false;
        bool pickupConfigured = false;
        bool deliveryConfigured = false;
        if (hasBranch)
        {
            var branch = await _branches
                .GetBranchAsync(supplierOrg.Value, relationship.SupplierBranchId!.Value, cancellationToken)
                .ConfigureAwait(false);
            if (branch is not null)
            {
                pickupEnabled = branch.PickupEnabled;
                deliveryEnabled = branch.DeliveryEnabled;
                pickupConfigured = branch.PickupOperational;
                deliveryConfigured = branch.DeliveryOperational
                    || branch.DeliveryPolicy is not null
                    || !string.IsNullOrWhiteSpace(relationship.DeliveryInstructions);
            }
        }

        var paymentSettings = await _paymentSettings
            .ListByOrganizationAsync(supplierOrg, cancellationToken)
            .ConfigureAwait(false);
        var enabledPoMethods = ResolveEnabledPoPaymentMethods(paymentSettings, _access);
        var hasPayment = enabledPoMethods.Count > 0;
        var utangEnabled = enabledPoMethods.Contains(PaymentMethodCatalog.Utang, StringComparer.OrdinalIgnoreCase);

        var hasValidCredit = false;
        if (utangEnabled)
        {
            var policy = await _creditPolicies
                .GetBySellerAndBuyerAsync(supplierOrg, relationship.BuyerOrganizationId, cancellationToken)
                .ConfigureAwait(false);
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
                DeliveryEnabled: deliveryEnabled,
                PickupConfigured: pickupConfigured,
                DeliveryConfigured: deliveryConfigured,
                HasAcceptedPaymentMethod: hasPayment,
                UtangPaymentEnabled: utangEnabled,
                HasValidCreditPolicy: hasValidCredit,
                HasSharedCatalog: hasCatalog,
                HasResponsibleContact: hasContact));
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

    private static ConnectedSupplierCommerceReadinessDto ToDto(
        Guid relationshipId,
        ConnectedSupplierCommerceReadiness.Result evaluated,
        bool includeRequirements)
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

        return new ConnectedSupplierCommerceReadinessDto(
            relationshipId,
            evaluated.IsReady,
            evaluated.SupportedFulfillmentMethods,
            requirements);
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
