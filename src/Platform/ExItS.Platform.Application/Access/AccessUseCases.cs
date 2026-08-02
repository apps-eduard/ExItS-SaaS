using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Access;

/// <summary>
/// Product-entry and new-grant eligibility. New grants remain Trialing/Active only (P4-WP02).
/// PinoyBusinessPOS entry allows continuity states when view/repay grants are effective (P6-WP05).
/// Other products remain Trialing/Active only. Suspended and unknown always deny entry.
/// </summary>
public static class ProductAccessEligibility
{
    /// <summary>Eligibility to grant a new product-access assignment (unchanged from P4-WP02).</summary>
    public static bool IsSubscriptionEligible(SubscriptionStatus status) =>
        status is SubscriptionStatus.Trialing or SubscriptionStatus.Active;

    /// <summary>Alias kept for call sites that mean new-grant eligibility.</summary>
    public static bool IsEligibleForNewGrant(SubscriptionStatus status) =>
        IsSubscriptionEligible(status);

    public static bool CanEnterProduct(
        string productCode,
        SubscriptionStatus status,
        IEnumerable<EntitlementGrant> grants)
    {
        ArgumentNullException.ThrowIfNull(grants);

        if (string.Equals(productCode, ProductCode.PinoyBusinessPos, StringComparison.Ordinal))
        {
            return CanEnterPinoyBusinessPos(status, grants);
        }

        // Unrelated products: do not weaken — Trialing/Active only.
        return IsSubscriptionEligible(status);
    }

    public static bool CanEnterPinoyBusinessPos(
        SubscriptionStatus status,
        IEnumerable<EntitlementGrant> grants)
    {
        ArgumentNullException.ThrowIfNull(grants);

        return status switch
        {
            SubscriptionStatus.Trialing
                or SubscriptionStatus.Active
                or SubscriptionStatus.GracePeriod => true,
            SubscriptionStatus.PastDue
                or SubscriptionStatus.Cancelled
                or SubscriptionStatus.Expired => HasContinuityFeature(grants),
            SubscriptionStatus.Suspended => false,
            _ => false
        };
    }

    public static bool HasContinuityFeature(IEnumerable<EntitlementGrant> grants) =>
        grants.Any(g =>
            g.Enabled
            && (g.FeatureCode.Value is FeatureCode.CustomerCreditView
                or FeatureCode.CustomerCreditRepay));

    public static IReadOnlyList<string> EnabledFeatureCodes(IEnumerable<EntitlementGrant> grants) =>
        grants
            .Where(g => g.Enabled)
            .Select(g => g.FeatureCode.Value)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(c => c, StringComparer.Ordinal)
            .ToList();
}

public sealed record ProductAccessAssignmentDto(
    Guid Id,
    Guid UserId,
    Guid OrganizationId,
    Guid MembershipId,
    string ProductCode,
    string Status,
    DateTimeOffset GrantedAtUtc,
    string GrantedByActor,
    DateTimeOffset? RevokedAtUtc,
    string? RevokedByActor,
    string? Reason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);

public sealed record EffectiveProductAccessResult(
    bool Allowed,
    string ReasonCode,
    Guid UserId,
    Guid OrganizationId,
    string ProductCode,
    Guid? MembershipId,
    Guid? AssignmentId,
    Guid? SubscriptionId,
    Guid? SnapshotId,
    DateTimeOffset EvaluatedAtUtc,
    string? SubscriptionStatus = null,
    IReadOnlyList<string>? EnabledFeatureCodes = null);

public sealed class ProductAccessQueryService
{
    private readonly IProductAccessAssignmentRepository _assignments;

    public ProductAccessQueryService(IProductAccessAssignmentRepository assignments) => _assignments = assignments;

    public async Task<ProductAccessAssignmentDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetByIdAsync(ProductAccessAssignmentId.From(id), cancellationToken)
            .ConfigureAwait(false);
        return assignment is null ? null : Map(assignment);
    }

    public async Task<PagedResult<ProductAccessAssignmentDto>> ListByOrganizationAsync(
        Guid organizationId,
        ProductAccessStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _assignments
            .ListByOrganizationAsync(PlatformOrganizationId.From(organizationId), status, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return ToPaged(items, total, page, take);
    }

    public async Task<PagedResult<ProductAccessAssignmentDto>> ListByUserAsync(
        Guid userId,
        ProductAccessStatus? status,
        int? page,
        int? pageSize,
        CancellationToken cancellationToken = default)
    {
        var (skip, take) = CatalogPagination.Normalize(page, pageSize);
        var (items, total) = await _assignments
            .ListByUserAsync(PlatformUserId.From(userId), status, skip, take, cancellationToken)
            .ConfigureAwait(false);
        return ToPaged(items, total, page, take);
    }

    private static PagedResult<ProductAccessAssignmentDto> ToPaged(
        IReadOnlyList<ProductAccessAssignment> items,
        int total,
        int? page,
        int take) =>
        new(items.Select(Map).ToList(), total, Math.Max(page ?? 1, 1), take);

    public static ProductAccessAssignmentDto Map(ProductAccessAssignment assignment) =>
        new(
            assignment.Id.Value,
            assignment.UserId.Value,
            assignment.OrganizationId.Value,
            assignment.MembershipId.Value,
            assignment.ProductCode.Value,
            assignment.Status.ToString(),
            assignment.GrantedAtUtc,
            assignment.GrantedByActor,
            assignment.RevokedAtUtc,
            assignment.RevokedByActor,
            assignment.Reason,
            assignment.CreatedAtUtc,
            assignment.UpdatedAtUtc);
}

public sealed class GrantProductAccess
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IProductRepository _products;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IEntitlementSnapshotRepository _snapshots;
    private readonly IProductAccessAssignmentRepository _assignments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public GrantProductAccess(
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations,
        IOrganizationMembershipRepository memberships,
        IProductRepository products,
        ISubscriptionRepository subscriptions,
        IEntitlementSnapshotRepository snapshots,
        IProductAccessAssignmentRepository assignments,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _organizations = organizations;
        _memberships = memberships;
        _products = products;
        _subscriptions = subscriptions;
        _snapshots = snapshots;
        _assignments = assignments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductAccessAssignment>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        string productCode,
        string grantedByActor,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var code = ProductCode.Create(productCode);
            var utcNow = _clock.UtcNow;

            var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
            if (user is null)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.UserNotFound, "Platform User was not found.");
            }

            if (user.Status != AccountStatus.Active)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    DomainErrorCodes.UserNotActive, "Product access requires an active Platform User.");
            }

            var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);
            if (organization is null)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.OrganizationNotFound, "Platform Organization was not found.");
            }

            if (organization.Status != OrganizationStatus.Active)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    DomainErrorCodes.OrganizationNotActive, "Product access requires an active organization.");
            }

            var membership = await _memberships
                .FindActiveByUserAndOrganizationAsync(userId, organizationId, cancellationToken)
                .ConfigureAwait(false);
            if (membership is null)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.MembershipNotFound,
                    "An active organization membership is required before granting product access.");
            }

            var product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.ProductNotFound, "Product was not found.");
            }

            if (product.Status != ProductStatus.Active)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.ProductNotActive, "Product must be active to grant access.");
            }

            var subscription = await _subscriptions
                .GetCurrentForOrganizationProductAsync(organizationId, code, cancellationToken)
                .ConfigureAwait(false);
            if (subscription is null || !ProductAccessEligibility.IsSubscriptionEligible(subscription.Status))
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.SubscriptionIneligible,
                    "An eligible Trialing or Active subscription is required to grant product access.");
            }

            var snapshot = await _snapshots
                .GetLatestForOrganizationProductAsync(organizationId, code, cancellationToken)
                .ConfigureAwait(false);
            if (snapshot is null)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.EntitlementMissing,
                    "A current entitlement snapshot is required to grant product access.");
            }

            if (snapshot.RefreshByUtc < utcNow)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.EntitlementStale,
                    "The entitlement snapshot is stale and must be refreshed before granting access.");
            }

            if (snapshot.ExpiresAtUtc is { } expires && expires <= utcNow)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.EntitlementDenied,
                    "The entitlement snapshot has expired.");
            }

            if (!ProductAccessEligibility.IsSubscriptionEligible(snapshot.SubscriptionStatus))
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.EntitlementDenied,
                    "The entitlement snapshot subscription status does not permit commercial access.");
            }

            var existing = await _assignments
                .FindActiveByUserOrganizationProductAsync(userId, organizationId, code, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return ApplicationResult<ProductAccessAssignment>.Failure(
                    ApplicationErrorCodes.ProductAccessConflict,
                    "An active product-access assignment already exists for this user, organization, and product.");
            }

            var assignment = ProductAccessAssignment.Grant(
                userId,
                organizationId,
                membership.Id,
                code,
                grantedByActor,
                utcNow,
                reason);
            await _assignments.AddAsync(assignment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductAccessAssignment>.Success(assignment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductAccessAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokeProductAccess
{
    private readonly IProductAccessAssignmentRepository _assignments;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeProductAccess(
        IProductAccessAssignmentRepository assignments,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _assignments = assignments;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductAccessAssignment>> ExecuteAsync(
        ProductAccessAssignmentId assignmentId,
        string revokedByActor,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var assignment = await _assignments.GetByIdAsync(assignmentId, cancellationToken).ConfigureAwait(false);
        if (assignment is null)
        {
            return ApplicationResult<ProductAccessAssignment>.Failure(
                ApplicationErrorCodes.ProductAccessNotFound,
                "Product access assignment was not found.");
        }

        try
        {
            assignment.Revoke(revokedByActor, reason, _clock.UtcNow);
            await _assignments.UpdateAsync(assignment, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductAccessAssignment>.Success(assignment);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductAccessAssignment>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class EvaluateEffectiveProductAccess
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IProductRepository _products;
    private readonly IProductAccessAssignmentRepository _assignments;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IEntitlementSnapshotRepository _snapshots;
    private readonly IClock _clock;

    public EvaluateEffectiveProductAccess(
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations,
        IOrganizationMembershipRepository memberships,
        IProductRepository products,
        IProductAccessAssignmentRepository assignments,
        ISubscriptionRepository subscriptions,
        IEntitlementSnapshotRepository snapshots,
        IClock clock)
    {
        _users = users;
        _organizations = organizations;
        _memberships = memberships;
        _products = products;
        _assignments = assignments;
        _subscriptions = subscriptions;
        _snapshots = snapshots;
        _clock = clock;
    }

    public async Task<EffectiveProductAccessResult> ExecuteAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _clock.UtcNow;
        var code = ProductCode.Create(productCode);

        EffectiveProductAccessResult Denied(string reason, Guid? membershipId = null, Guid? assignmentId = null, Guid? subscriptionId = null, Guid? snapshotId = null) =>
            new(false, reason, userId.Value, organizationId.Value, code.Value, membershipId, assignmentId, subscriptionId, snapshotId, utcNow);

        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return Denied(EffectiveAccessReasonCodes.UserInactive);
        }

        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null || organization.Status != OrganizationStatus.Active)
        {
            return Denied(EffectiveAccessReasonCodes.OrganizationInactive);
        }

        var membership = await _memberships
            .FindActiveByUserAndOrganizationAsync(userId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            var current = await _memberships
                .FindCurrentByUserAndOrganizationAsync(userId, organizationId, cancellationToken)
                .ConfigureAwait(false);
            return current is null
                ? Denied(EffectiveAccessReasonCodes.MembershipMissing)
                : Denied(EffectiveAccessReasonCodes.MembershipInactive, current.Id.Value);
        }

        var product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Status != ProductStatus.Active)
        {
            return Denied(EffectiveAccessReasonCodes.ProductInactive, membership.Id.Value);
        }

        var assignment = await _assignments
            .FindActiveByUserOrganizationProductAsync(userId, organizationId, code, cancellationToken)
            .ConfigureAwait(false);
        if (assignment is null)
        {
            return Denied(EffectiveAccessReasonCodes.ProductAssignmentMissing, membership.Id.Value);
        }

        if (assignment.OrganizationId != membership.OrganizationId
            || assignment.MembershipId != membership.Id
            || assignment.UserId != membership.UserId)
        {
            return Denied(EffectiveAccessReasonCodes.ProductAssignmentInactive, membership.Id.Value, assignment.Id.Value);
        }

        var subscription = await _subscriptions
            .GetCurrentForOrganizationProductAsync(organizationId, code, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            return Denied(
                EffectiveAccessReasonCodes.SubscriptionIneligible,
                membership.Id.Value,
                assignment.Id.Value);
        }

        var snapshot = await _snapshots
            .GetLatestForOrganizationProductAsync(organizationId, code, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
        {
            return Denied(
                EffectiveAccessReasonCodes.EntitlementMissing,
                membership.Id.Value,
                assignment.Id.Value,
                subscription.Id.Value);
        }

        if (snapshot.RefreshByUtc < utcNow)
        {
            return Denied(
                EffectiveAccessReasonCodes.EntitlementStale,
                membership.Id.Value,
                assignment.Id.Value,
                subscription.Id.Value,
                snapshot.Id.Value);
        }

        if (snapshot.ExpiresAtUtc is { } expires && expires <= utcNow)
        {
            return Denied(
                EffectiveAccessReasonCodes.EntitlementDenied,
                membership.Id.Value,
                assignment.Id.Value,
                subscription.Id.Value,
                snapshot.Id.Value);
        }

        // Prefer snapshot commercial status + grants as effective truth for entry.
        if (!ProductAccessEligibility.CanEnterProduct(
                code.Value,
                snapshot.SubscriptionStatus,
                snapshot.Grants))
        {
            return new EffectiveProductAccessResult(
                false,
                EffectiveAccessReasonCodes.EntitlementDenied,
                userId.Value,
                organizationId.Value,
                code.Value,
                membership.Id.Value,
                assignment.Id.Value,
                subscription.Id.Value,
                snapshot.Id.Value,
                utcNow,
                snapshot.SubscriptionStatus.ToString(),
                ProductAccessEligibility.EnabledFeatureCodes(snapshot.Grants));
        }

        // Also require the live subscription status permits entry for this product
        // (guards against snapshot/subscription skew that would otherwise over-admit).
        if (!ProductAccessEligibility.CanEnterProduct(
                code.Value,
                subscription.Status,
                snapshot.Grants))
        {
            return new EffectiveProductAccessResult(
                false,
                EffectiveAccessReasonCodes.SubscriptionIneligible,
                userId.Value,
                organizationId.Value,
                code.Value,
                membership.Id.Value,
                assignment.Id.Value,
                subscription.Id.Value,
                snapshot.Id.Value,
                utcNow,
                subscription.Status.ToString(),
                ProductAccessEligibility.EnabledFeatureCodes(snapshot.Grants));
        }

        return new EffectiveProductAccessResult(
            true,
            EffectiveAccessReasonCodes.Allowed,
            userId.Value,
            organizationId.Value,
            code.Value,
            membership.Id.Value,
            assignment.Id.Value,
            subscription.Id.Value,
            snapshot.Id.Value,
            utcNow,
            snapshot.SubscriptionStatus.ToString(),
            ProductAccessEligibility.EnabledFeatureCodes(snapshot.Grants));
    }
}

public static class EffectiveAccessReasonCodes
{
    public const string Allowed = "allowed";
    public const string UserInactive = "user_inactive";
    public const string OrganizationInactive = "organization_inactive";
    public const string MembershipMissing = "membership_missing";
    public const string MembershipInactive = "membership_inactive";
    public const string ProductAssignmentMissing = "product_assignment_missing";
    public const string ProductAssignmentInactive = "product_assignment_inactive";
    public const string ProductInactive = "product_inactive";
    public const string SubscriptionIneligible = "subscription_ineligible";
    public const string EntitlementMissing = "entitlement_missing";
    public const string EntitlementStale = "entitlement_stale";
    public const string EntitlementDenied = "entitlement_denied";
    public const string ProductLocalRoleMissing = "product_local_role_missing";
}

/// <summary>
/// Entitlement enables the Organization product; product-local role authorizes the individual to operate it.
/// </summary>
public sealed record ProductAuthorizationResult(
    bool EntitlementAllowed,
    bool ProductAccessAssigned,
    bool ProductLocalRoleGranted,
    bool CanOperate,
    string ReasonCode,
    Guid UserId,
    Guid OrganizationId,
    string ProductCode,
    string? ProductLocalRoleCode,
    string? MappedPosRoleCode,
    Guid? MembershipId,
    Guid? AssignmentId,
    Guid? SubscriptionId,
    Guid? SnapshotId,
    Guid? ProductLocalRoleGrantId,
    DateTimeOffset EvaluatedAtUtc,
    string? SubscriptionStatus = null,
    IReadOnlyList<string>? EnabledFeatureCodes = null);

public sealed record EnabledProductDto(
    string ProductCode,
    string DisplayName,
    bool EntitlementActive,
    bool ProductAccessAssigned,
    bool ProductLocalRoleGranted,
    bool CanLaunch,
    string? ProductLocalRoleCode,
    string? MappedPosRoleCode,
    string? SubscriptionStatus,
    string ReasonCode);

public sealed record ProductLocalRoleGrantDto(
    Guid Id,
    Guid OrganizationId,
    Guid UserIdentityId,
    string ProductCode,
    string RoleCode,
    string MappedPosRoleCode,
    string Status,
    DateTimeOffset GrantedAtUtc,
    Guid GrantedByUserIdentityId,
    string Source,
    DateTimeOffset? RevokedAtUtc,
    Guid? RevokedByUserIdentityId,
    string? Reason);

public sealed class EvaluateProductAuthorization
{
    private readonly EvaluateEffectiveProductAccess _commercial;
    private readonly IProductLocalRoleGrantRepository _roleGrants;
    private readonly IClock _clock;

    public EvaluateProductAuthorization(
        EvaluateEffectiveProductAccess commercial,
        IProductLocalRoleGrantRepository roleGrants,
        IClock clock)
    {
        _commercial = commercial;
        _roleGrants = roleGrants;
        _clock = clock;
    }

    public async Task<ProductAuthorizationResult> ExecuteAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        string productCode,
        CancellationToken cancellationToken = default)
    {
        var commercial = await _commercial
            .ExecuteAsync(userId, organizationId, productCode, cancellationToken)
            .ConfigureAwait(false);

        var grant = await _roleGrants
            .FindActiveByUserOrganizationProductAsync(
                organizationId,
                userId,
                commercial.ProductCode,
                cancellationToken)
            .ConfigureAwait(false);

        var roleGranted = grant is not null;
        var entitlementAllowed = commercial.Allowed;
        var canOperate = entitlementAllowed && roleGranted;
        var reason = canOperate
            ? EffectiveAccessReasonCodes.Allowed
            : entitlementAllowed
                ? EffectiveAccessReasonCodes.ProductLocalRoleMissing
                : commercial.ReasonCode;

        return new ProductAuthorizationResult(
            EntitlementAllowed: entitlementAllowed,
            ProductAccessAssigned: commercial.AssignmentId is not null,
            ProductLocalRoleGranted: roleGranted,
            CanOperate: canOperate,
            ReasonCode: reason,
            UserId: userId.Value,
            OrganizationId: organizationId.Value,
            ProductCode: commercial.ProductCode,
            ProductLocalRoleCode: grant?.RoleCode,
            MappedPosRoleCode: grant?.MappedPosRoleCode,
            MembershipId: commercial.MembershipId,
            AssignmentId: commercial.AssignmentId,
            SubscriptionId: commercial.SubscriptionId,
            SnapshotId: commercial.SnapshotId,
            ProductLocalRoleGrantId: grant?.Id.Value,
            EvaluatedAtUtc: _clock.UtcNow,
            SubscriptionStatus: commercial.SubscriptionStatus,
            EnabledFeatureCodes: commercial.EnabledFeatureCodes);
    }
}

public sealed class DiscoverEnabledProducts
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IProductRepository _products;
    private readonly EvaluateProductAuthorization _authorize;

    public DiscoverEnabledProducts(
        IOrganizationMembershipRepository memberships,
        ISubscriptionRepository subscriptions,
        IProductRepository products,
        EvaluateProductAuthorization authorize)
    {
        _memberships = memberships;
        _subscriptions = subscriptions;
        _products = products;
        _authorize = authorize;
    }

    public async Task<ApplicationResult<IReadOnlyList<EnabledProductDto>>> ExecuteAsync(
        PlatformUserId userId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var membership = await _memberships
            .FindActiveByUserAndOrganizationAsync(userId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<IReadOnlyList<EnabledProductDto>>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Active organization membership is required for product discovery.");
        }

        var (subscriptions, _) = await _subscriptions
            .ListByOrganizationAsync(organizationId, status: null, skip: 0, take: 100, cancellationToken)
            .ConfigureAwait(false);

        // Deduplicate by stable product code so role grants / multiple subscription rows never duplicate UI.
        var productCodes = subscriptions
            .Select(s => s.ProductCode.Value)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .GroupBy(c => c.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var results = new List<EnabledProductDto>(productCodes.Count);
        var seenProductIds = new HashSet<Guid>();
        foreach (var code in productCodes)
        {
            var product = await _products.GetByCodeAsync(ProductCode.Create(code), cancellationToken)
                .ConfigureAwait(false);
            if (product is null || product.Status != ProductStatus.Active)
            {
                continue;
            }

            if (!seenProductIds.Add(product.Id.Value))
            {
                continue;
            }

            var auth = await _authorize
                .ExecuteAsync(userId, organizationId, code, cancellationToken)
                .ConfigureAwait(false);

            // Org entitlement is "active" when commercial entry is allowed, or only individual grants are missing.
            var entitlementActive = auth.EntitlementAllowed
                || auth.ReasonCode is EffectiveAccessReasonCodes.ProductAssignmentMissing
                    or EffectiveAccessReasonCodes.ProductLocalRoleMissing;

            results.Add(new EnabledProductDto(
                auth.ProductCode,
                product.DisplayName,
                EntitlementActive: entitlementActive,
                ProductAccessAssigned: auth.ProductAccessAssigned,
                ProductLocalRoleGranted: auth.ProductLocalRoleGranted,
                CanLaunch: auth.CanOperate,
                ProductLocalRoleCode: auth.ProductLocalRoleCode,
                MappedPosRoleCode: auth.MappedPosRoleCode,
                SubscriptionStatus: auth.SubscriptionStatus,
                ReasonCode: auth.ReasonCode));
        }

        return ApplicationResult<IReadOnlyList<EnabledProductDto>>.Success(results);
    }
}

public sealed class AssignProductLocalRole
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IProductRepository _products;
    private readonly IProductLocalRoleGrantRepository _grants;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AssignProductLocalRole(
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations,
        IOrganizationMembershipRepository memberships,
        IProductRepository products,
        IProductLocalRoleGrantRepository grants,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _users = users;
        _organizations = organizations;
        _memberships = memberships;
        _products = products;
        _grants = grants;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductLocalRoleGrant>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userIdentityId,
        string productCode,
        string roleCode,
        PlatformUserId grantedByUserIdentityId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status != AccountStatus.Active)
        {
            return ApplicationResult<ProductLocalRoleGrant>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "Target user was not found or is inactive.");
        }

        var organization = await _organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null || organization.Status != OrganizationStatus.Active)
        {
            return ApplicationResult<ProductLocalRoleGrant>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found or is inactive.");
        }

        var membership = await _memberships
            .FindActiveByUserAndOrganizationAsync(userIdentityId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<ProductLocalRoleGrant>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Active organization membership is required before assigning a product-local role.");
        }

        ProductCode code;
        try
        {
            code = ProductCode.Create(productCode);
            _ = ProductLocalRoleCodes.EnsureKnown(roleCode);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductLocalRoleGrant>.Failure(ex.ErrorCode, ex.Message);
        }

        var product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (product is null || product.Status != ProductStatus.Active)
        {
            return ApplicationResult<ProductLocalRoleGrant>.Failure(
                ApplicationErrorCodes.ProductNotFound,
                "Product was not found or is inactive.");
        }

        var existing = await _grants
            .FindActiveByUserOrganizationProductAsync(organizationId, userIdentityId, code.Value, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            if (string.Equals(existing.RoleCode, ProductLocalRoleCodes.EnsureKnown(roleCode), StringComparison.Ordinal))
            {
                return ApplicationResult<ProductLocalRoleGrant>.Failure(
                    ApplicationErrorCodes.ProductLocalRoleGrantConflict,
                    "An active product-local role grant already exists for this user and product.");
            }

            try
            {
                existing.Revoke(grantedByUserIdentityId, reason ?? "Replaced by new product-local role assignment.", _clock.UtcNow);
                await _grants.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
            }
            catch (DomainException ex)
            {
                return ApplicationResult<ProductLocalRoleGrant>.Failure(ex.ErrorCode, ex.Message);
            }
        }

        try
        {
            var grant = ProductLocalRoleGrant.Create(
                organizationId,
                userIdentityId,
                code.Value,
                roleCode,
                grantedByUserIdentityId,
                _clock.UtcNow,
                ProductLocalRoleGrant.AssignmentSource);
            await _grants.AddAsync(grant, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductLocalRoleGrant>.Success(grant);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductLocalRoleGrant>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokeProductLocalRole
{
    private readonly IProductLocalRoleGrantRepository _grants;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokeProductLocalRole(
        IProductLocalRoleGrantRepository grants,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _grants = grants;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<ProductLocalRoleGrant>> ExecuteAsync(
        ProductLocalRoleGrantId grantId,
        PlatformUserId revokedByUserIdentityId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var grant = await _grants.GetByIdAsync(grantId, cancellationToken).ConfigureAwait(false);
        if (grant is null)
        {
            return ApplicationResult<ProductLocalRoleGrant>.Failure(
                ApplicationErrorCodes.ProductLocalRoleGrantNotFound,
                "Product-local role grant was not found.");
        }

        try
        {
            grant.Revoke(revokedByUserIdentityId, reason, _clock.UtcNow);
            await _grants.UpdateAsync(grant, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<ProductLocalRoleGrant>.Success(grant);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<ProductLocalRoleGrant>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ProductLocalRoleGrantQueryService
{
    private readonly IProductLocalRoleGrantRepository _grants;

    public ProductLocalRoleGrantQueryService(IProductLocalRoleGrantRepository grants) => _grants = grants;

    public async Task<IReadOnlyList<ProductLocalRoleGrantDto>> ListByOrganizationAsync(
        Guid organizationId,
        ProductLocalRoleGrantStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var items = await _grants
            .ListByOrganizationAsync(PlatformOrganizationId.From(organizationId), status, cancellationToken)
            .ConfigureAwait(false);
        return items.Select(Map).ToList();
    }

    public static ProductLocalRoleGrantDto Map(ProductLocalRoleGrant grant) =>
        new(
            grant.Id.Value,
            grant.OrganizationId.Value,
            grant.UserIdentityId.Value,
            grant.ProductCode,
            grant.RoleCode,
            grant.MappedPosRoleCode,
            grant.Status.ToString(),
            grant.GrantedAtUtc,
            grant.GrantedByUserIdentityId.Value,
            grant.Source,
            grant.RevokedAtUtc,
            grant.RevokedByUserIdentityId?.Value,
            grant.Reason);
}
