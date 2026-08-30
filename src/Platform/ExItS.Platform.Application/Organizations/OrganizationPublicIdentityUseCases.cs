using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Application.Organizations;

public sealed record OrganizationPublicIdentityDto(
    string PublicOrganizationId,
    string QrPayload,
    string DisplayName);

public sealed record ResolvePublicOrganizationIdRequest(
    string PublicOrganizationIdOrQrPayload,
    string? Purpose = null);

public sealed record ResolvedPublicOrganizationDto(
    string PublicOrganizationId,
    Guid OrganizationId,
    string DisplayName,
    string Status);

/// <summary>Returns the caller's organization business QR when they are an active member.</summary>
public sealed class GetOrganizationPublicIdentity(
    IPlatformOrganizationRepository organizations,
    IOrganizationMembershipRepository memberships)
{
    public async Task<ApplicationResult<OrganizationPublicIdentityDto>> ExecuteAsync(
        PlatformUserId actorUserId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var membership = await memberships
            .FindActiveByUserAndOrganizationAsync(actorUserId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<OrganizationPublicIdentityDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "You are not an active member of this organization.");
        }

        var organization = await organizations.GetByIdAsync(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization is null)
        {
            return ApplicationResult<OrganizationPublicIdentityDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "Organization was not found.");
        }

        if (string.IsNullOrWhiteSpace(organization.PublicOrganizationId))
        {
            return ApplicationResult<OrganizationPublicIdentityDto>.Failure(
                ApplicationErrorCodes.PublicOrganizationIdNotAssigned,
                "This organization does not yet have a public organization ID.");
        }

        try
        {
            var publicId = PublicOrganizationIdRules.Normalize(organization.PublicOrganizationId);
            return ApplicationResult<OrganizationPublicIdentityDto>.Success(new OrganizationPublicIdentityDto(
                publicId,
                PublicOrganizationIdRules.BuildQrPayload(publicId),
                organization.DisplayName));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<OrganizationPublicIdentityDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

/// <summary>
/// Exact-match public organization ID lookup. Never supports partial search.
/// Returns a generic not-found for unknown/non-active orgs. Does not grant membership.
/// </summary>
public sealed class ResolvePublicOrganizationId(
    IPlatformOrganizationRepository organizations,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<ResolvedPublicOrganizationDto>> ExecuteAsync(
        PlatformUserId actorUserId,
        ResolvePublicOrganizationIdRequest request,
        CancellationToken cancellationToken = default)
    {
        string normalized;
        try
        {
            normalized = PublicOrganizationIdRules.TryExtractFromQrPayload(request.PublicOrganizationIdOrQrPayload);
        }
        catch (DomainException)
        {
            return ApplicationResult<ResolvedPublicOrganizationDto>.Failure(
                DomainErrorCodes.InvalidPublicOrganizationId,
                "Public organization ID format is invalid.");
        }

        var purpose = string.IsNullOrWhiteSpace(request.Purpose)
            ? "unspecified"
            : request.Purpose.Trim().ToLowerInvariant();
        if (purpose.Length > 64)
        {
            purpose = purpose[..64];
        }

        var target = await organizations
            .GetByPublicOrganizationIdAsync(normalized, cancellationToken)
            .ConfigureAwait(false);

        await audit.WriteAsync(
            $"platform-user:{actorUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.OrganizationPublicIdResolved,
            "public_organization_id",
            normalized,
            target is null ? AuditOutcome.Denied : AuditOutcome.Succeeded,
            summary: $"purpose={purpose}",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (target is null || target.Status is not OrganizationStatus.Active)
        {
            return ApplicationResult<ResolvedPublicOrganizationDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "No active organization matched that public ID.");
        }

        return ApplicationResult<ResolvedPublicOrganizationDto>.Success(new ResolvedPublicOrganizationDto(
            target.PublicOrganizationId!,
            target.Id.Value,
            target.DisplayName,
            target.Status.ToString()));
    }
}

/// <summary>
/// Anonymous public store landing lookup by PublicOrganizationId only.
/// Returns minimal public-safe fields. Generic not-found for unknown/inactive orgs.
/// Does not grant membership, customer link, staff, or ownership.
/// OrderingAvailable uses Platform branch fulfillment readiness (not "active ⇒ ready").
/// </summary>
public sealed record PublicStoreLandingDto(
    string PublicOrganizationId,
    string DisplayName,
    bool OrderingAvailable);

public sealed class LookupPublicStoreLanding(
    IPlatformOrganizationRepository organizations,
    IOrganizationBranchRepository branches,
    IBranchOperatingHoursRepository hours,
    IBranchDeliveryPolicyRepository policies,
    EntitlementQueryService entitlements,
    IBranchFulfillmentReadinessEvaluator readinessEvaluator,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<PublicStoreLandingDto>> ExecuteAsync(
        string publicOrganizationIdOrPayload,
        CancellationToken cancellationToken = default)
    {
        string normalized;
        try
        {
            normalized = PublicOrganizationIdRules.TryExtractFromQrPayload(publicOrganizationIdOrPayload);
        }
        catch (DomainException)
        {
            return ApplicationResult<PublicStoreLandingDto>.Failure(
                DomainErrorCodes.InvalidPublicOrganizationId,
                "Store was not found.");
        }

        var target = await organizations
            .GetByPublicOrganizationIdAsync(normalized, cancellationToken)
            .ConfigureAwait(false);

        var foundActive = target is not null && target.Status is OrganizationStatus.Active;

        await audit.WriteAsync(
            "anonymous:public-store",
            AuditActorType.System,
            PlatformAuditActions.PublicStoreLandingLookedUp,
            "public_organization_id",
            normalized,
            foundActive ? AuditOutcome.Succeeded : AuditOutcome.Denied,
            summary: "purpose=public-store-landing",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!foundActive)
        {
            // Generic customer-friendly not-found (no suspension detail leakage).
            return ApplicationResult<PublicStoreLandingDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "This store is unavailable.");
        }

        var orderingAvailable = await EvaluateOrderingAvailableAsync(target!, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<PublicStoreLandingDto>.Success(new PublicStoreLandingDto(
            target!.PublicOrganizationId!,
            target.DisplayName,
            OrderingAvailable: orderingAvailable));
    }

    /// <summary>
    /// True when at least one Active branch is fulfillment-ready for customer ordering
    /// and has customer ordering enabled (not paused). Does not require open-now;
    /// authenticated storefront remains the operational authority.
    /// </summary>
    private async Task<bool> EvaluateOrderingAvailableAsync(
        PlatformOrganization organization,
        CancellationToken cancellationToken)
    {
        var caps = await ResolveCapabilitiesAsync(organization.Id, cancellationToken).ConfigureAwait(false);
        if (!caps.CanUseCustomerOrdering)
        {
            return false;
        }

        var orgBranches = await branches
            .ListByOrganizationAsync(organization.Id, cancellationToken)
            .ConfigureAwait(false);

        var now = clock.UtcNow;
        foreach (var branch in orgBranches)
        {
            if (branch.Status is not OrganizationBranchStatus.Active)
            {
                continue;
            }

            if (!branch.CustomerOrderingEnabled || branch.OnlineOrdersPaused)
            {
                continue;
            }

            var branchHours = await hours.GetByBranchIdAsync(branch.Id, cancellationToken).ConfigureAwait(false);
            var policy = await policies.GetByBranchIdAsync(branch.Id, cancellationToken).ConfigureAwait(false);
            var result = readinessEvaluator.Evaluate(new BranchFulfillmentReadinessInput(
                branch,
                branchHours,
                policy,
                organization.Profile.TimeZoneId,
                organization.Profile.ContactPhone,
                caps,
                now,
                HasActiveDeliveryServiceArea: false));

            if (result.CustomerOrderingReady)
            {
                return true;
            }
        }

        return false;
    }

    private async Task<BranchEntitlementCapabilities> ResolveCapabilitiesAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var snapshot = await entitlements
            .GetLatestAsync(organizationId.Value, ProductCode.PinoyBusinessPos, cancellationToken)
            .ConfigureAwait(false);
        if (snapshot is null)
        {
            return new BranchEntitlementCapabilities(false, false);
        }

        var canOrder = snapshot.Grants.Any(g =>
            g.Enabled
            && string.Equals(g.FeatureCode, FeatureCode.StoreCustomerOrdering, StringComparison.Ordinal));
        var canDelivery = canOrder && snapshot.Grants.Any(g =>
            g.Enabled
            && string.Equals(g.FeatureCode, FeatureCode.StoreDeliveryOrders, StringComparison.Ordinal));
        return new BranchEntitlementCapabilities(canOrder, canDelivery);
    }
}
