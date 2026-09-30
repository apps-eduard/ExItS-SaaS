using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

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
    IOrganizationCustomerOrderingAvailability orderingAvailability,
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

        var orderingAvailable = await orderingAvailability
            .IsAvailableAsync(target!, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<PublicStoreLandingDto>.Success(new PublicStoreLandingDto(
            target!.PublicOrganizationId!,
            target.DisplayName,
            OrderingAvailable: orderingAvailable));
    }
}

/// <summary>
/// Public-safe Active branch locations for a business (B2B supplier connect / storefront routing).
/// Does not grant membership, staff access, or permissions.
/// </summary>
public sealed record PublicStoreBranchLocationDto(
    Guid BranchId,
    string Name,
    string Code,
    bool IsPrimary);

public sealed record PublicStoreBranchesDto(
    string PublicOrganizationId,
    string DisplayName,
    IReadOnlyList<PublicStoreBranchLocationDto> Branches);

public sealed class LookupPublicStoreBranches(
    IPlatformOrganizationRepository organizations,
    IOrganizationBranchRepository branches,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<PublicStoreBranchesDto>> ExecuteAsync(
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
            return ApplicationResult<PublicStoreBranchesDto>.Failure(
                DomainErrorCodes.InvalidPublicOrganizationId,
                "Store was not found.");
        }

        var target = await organizations
            .GetByPublicOrganizationIdAsync(normalized, cancellationToken)
            .ConfigureAwait(false);

        var foundActive = target is not null && target.Status is OrganizationStatus.Active;

        await audit.WriteAsync(
            "anonymous:public-store-branches",
            AuditActorType.System,
            PlatformAuditActions.PublicStoreLandingLookedUp,
            "public_organization_id",
            normalized,
            foundActive ? AuditOutcome.Succeeded : AuditOutcome.Denied,
            summary: "purpose=public-store-branches",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!foundActive)
        {
            return ApplicationResult<PublicStoreBranchesDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "This store is unavailable.");
        }

        var orgBranches = await branches
            .ListByOrganizationAsync(target!.Id, cancellationToken)
            .ConfigureAwait(false);

        var active = orgBranches
            .Where(b => b.Status == OrganizationBranchStatus.Active)
            .OrderByDescending(b => b.IsPrimary)
            .ThenBy(b => b.Name, StringComparer.OrdinalIgnoreCase)
            .Select(b => new PublicStoreBranchLocationDto(
                b.Id.Value,
                b.Name,
                b.Code,
                b.IsPrimary))
            .ToList();

        return ApplicationResult<PublicStoreBranchesDto>.Success(new PublicStoreBranchesDto(
            target.PublicOrganizationId!,
            target.DisplayName,
            active));
    }
}

/// <summary>
/// Public commerce-safe fulfillment flags for one Active branch.
/// Used by connected buyers who are not seller-org members and cannot call ListBranches.
/// </summary>
public sealed record PublicStoreBranchCommerceFulfillmentDto(
    Guid BranchId,
    string Name,
    bool PickupEnabled,
    bool DeliveryEnabled,
    bool PickupReady,
    bool DeliveryReady,
    bool CustomerOrderingEnabled);

public sealed class LookupPublicStoreBranchCommerceFulfillment(
    IPlatformOrganizationRepository organizations,
    IOrganizationBranchRepository branches,
    GetBranchFulfillmentReadiness readiness,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<PublicStoreBranchCommerceFulfillmentDto>> ExecuteAsync(
        string publicOrganizationIdOrPayload,
        Guid branchId,
        CancellationToken cancellationToken = default)
    {
        if (branchId == Guid.Empty)
        {
            return ApplicationResult<PublicStoreBranchCommerceFulfillmentDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        string normalized;
        try
        {
            normalized = PublicOrganizationIdRules.TryExtractFromQrPayload(publicOrganizationIdOrPayload);
        }
        catch (DomainException)
        {
            return ApplicationResult<PublicStoreBranchCommerceFulfillmentDto>.Failure(
                DomainErrorCodes.InvalidPublicOrganizationId,
                "Store was not found.");
        }

        var target = await organizations
            .GetByPublicOrganizationIdAsync(normalized, cancellationToken)
            .ConfigureAwait(false);

        var foundActive = target is not null && target.Status is OrganizationStatus.Active;

        await audit.WriteAsync(
            "anonymous:public-store-branch-commerce-fulfillment",
            AuditActorType.System,
            PlatformAuditActions.PublicStoreLandingLookedUp,
            "public_organization_id",
            normalized,
            foundActive ? AuditOutcome.Succeeded : AuditOutcome.Denied,
            summary: $"purpose=public-store-branch-commerce-fulfillment;branchId={branchId:D}",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        if (!foundActive)
        {
            return ApplicationResult<PublicStoreBranchCommerceFulfillmentDto>.Failure(
                ApplicationErrorCodes.OrganizationNotFound,
                "This store is unavailable.");
        }

        var branch = await branches
            .GetByIdAsync(OrganizationBranchId.From(branchId), cancellationToken)
            .ConfigureAwait(false);
        if (branch is null
            || branch.OrganizationId != target!.Id
            || branch.Status != OrganizationBranchStatus.Active)
        {
            return ApplicationResult<PublicStoreBranchCommerceFulfillmentDto>.Failure(
                ApplicationErrorCodes.BranchNotFound,
                "Branch was not found.");
        }

        var readinessResult = await readiness
            .ExecuteAsync(target.Id, branch.Id, cancellationToken)
            .ConfigureAwait(false);
        if (!readinessResult.IsSuccess || readinessResult.Value is null)
        {
            return ApplicationResult<PublicStoreBranchCommerceFulfillmentDto>.Failure(
                readinessResult.ErrorCode ?? ApplicationErrorCodes.BranchNotFound,
                readinessResult.ErrorMessage ?? "Branch was not found.");
        }

        var dto = readinessResult.Value;
        return ApplicationResult<PublicStoreBranchCommerceFulfillmentDto>.Success(
            new PublicStoreBranchCommerceFulfillmentDto(
                branch.Id.Value,
                branch.Name,
                dto.PickupEnabled,
                dto.DeliveryEnabled,
                dto.PickupReady,
                dto.DeliveryReady,
                dto.CustomerOrderingEnabled));
    }
}
