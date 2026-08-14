using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public static class SalesDocumentCapabilityStatuses
{
    public const string NotEnabled = "NotEnabled";
    public const string Enabled = "Enabled";
}

public sealed record OrganizationSalesDocumentCapabilityDto(
    Guid OrganizationId,
    bool TransactionSummaryAvailable,
    string ComplianceEligibilityStatus,
    string TaxDocumentIssuanceStatus,
    bool TaxDocumentIssuanceEnabled,
    bool TaxDocumentImplementationAvailable,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedByActorReference);

public sealed record OrganizationComplianceStatusDto(
    Guid OrganizationId,
    string ComplianceEligibilityStatus,
    bool TaxDocumentIssuanceEnabled,
    string TaxDocumentIssuanceStatus,
    bool TaxDocumentImplementationAvailable,
    bool CurrentOwnerEducationAcknowledged,
    string EducationVersion,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedByActorReference);

public interface IOrganizationSalesDocumentCapabilityRepository
{
    Task<OrganizationSalesDocumentCapability?> GetByOrganizationIdAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationSalesDocumentCapability capability,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        OrganizationSalesDocumentCapability capability,
        CancellationToken cancellationToken = default);
}

public sealed class GetOrganizationSalesDocumentCapability(
    IOrganizationSalesDocumentCapabilityRepository capabilities)
{
    public async Task<ApplicationResult<OrganizationSalesDocumentCapabilityDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<OrganizationSalesDocumentCapabilityDto>.Success(ToDto(organizationId, capability));
    }

    internal static OrganizationSalesDocumentCapabilityDto ToDto(
        PlatformOrganizationId organizationId,
        OrganizationSalesDocumentCapability? capability)
    {
        var enabled = capability?.TaxDocumentIssuanceEnabled == true;
        return new(
            organizationId.Value,
            TransactionSummaryAvailable: true,
            capability?.ComplianceEligibilityStatus
                ?? OrganizationComplianceEligibilityStatuses.NotRequested,
            enabled ? SalesDocumentCapabilityStatuses.Enabled : SalesDocumentCapabilityStatuses.NotEnabled,
            enabled,
            TaxDocumentIssuanceRuntime.ImplementationAvailable,
            capability?.UpdatedAtUtc,
            capability?.UpdatedByActorReference);
    }
}

public sealed class EnsureOrganizationSalesDocumentCapability(
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<OrganizationSalesDocumentCapability> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (capability is not null)
        {
            return capability;
        }

        capability = OrganizationSalesDocumentCapability.CreateDefault(organizationId, clock.UtcNow);
        await capabilities.AddAsync(capability, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return capability;
    }
}

public sealed class EnsureTaxDocumentIssuanceAllowed(
    IOrganizationSalesDocumentCapabilityRepository capabilities)
{
    public async Task<ApplicationResult> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        if (!TaxDocumentIssuanceRuntime.ImplementationAvailable)
        {
            return ApplicationResult.Failure(
                ApplicationErrorCodes.TaxDocumentIssuanceNotImplemented,
                "Tax-document issuance is not implemented in ExItS yet.");
        }

        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        return capability?.TaxDocumentIssuanceEnabled == true
            ? ApplicationResult.Success()
            : ApplicationResult.Failure(
                ApplicationErrorCodes.TaxDocumentIssuanceNotEnabled,
                "Tax-document issuance is not available for this organization.");
    }
}

public sealed class RequestTaxDocumentIssuance(EnsureTaxDocumentIssuanceAllowed ensureAllowed)
{
    public Task<ApplicationResult> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        ensureAllowed.ExecuteAsync(organizationId, cancellationToken);
}

public sealed class GetOrganizationComplianceStatus(
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    IOrganizationMembershipRepository memberships,
    IOrganizationSalesDocumentAcknowledgmentRepository acknowledgments,
    ISalesDocumentEducationVersionProvider versions)
{
    public async Task<ApplicationResult<OrganizationComplianceStatusDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var owner = await memberships
            .FindActiveOwnerByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var acknowledgment = owner is null
            ? null
            : await acknowledgments
                .FindAsync(organizationId, owner.UserId, versions.CurrentVersion, cancellationToken)
                .ConfigureAwait(false);
        var enabled = capability?.TaxDocumentIssuanceEnabled == true;

        return ApplicationResult<OrganizationComplianceStatusDto>.Success(new(
            organizationId.Value,
            capability?.ComplianceEligibilityStatus
                ?? OrganizationComplianceEligibilityStatuses.NotRequested,
            enabled,
            enabled ? SalesDocumentCapabilityStatuses.Enabled : SalesDocumentCapabilityStatuses.NotEnabled,
            TaxDocumentIssuanceRuntime.ImplementationAvailable,
            acknowledgment is not null,
            versions.CurrentVersion,
            capability?.UpdatedAtUtc,
            capability?.UpdatedByActorReference));
    }
}

public sealed class RequestOrganizationComplianceReview(
    IOrganizationMembershipRepository memberships,
    EnsureOrganizationSalesDocumentCapability ensureCapability,
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    GetOrganizationComplianceStatus getStatus,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<OrganizationComplianceStatusDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId actorUserId,
        string actorReference,
        CancellationToken cancellationToken = default)
    {
        var owner = await memberships
            .FindActiveOwnerByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (owner is null || owner.UserId != actorUserId)
        {
            return ApplicationResult<OrganizationComplianceStatusDto>.Failure(
                ApplicationErrorCodes.ComplianceOwnerRequired,
                "Only the current active Organization Owner may request compliance review.");
        }

        var capability = await ensureCapability.ExecuteAsync(organizationId, cancellationToken).ConfigureAwait(false);
        try
        {
            capability.TransitionEligibility(
                OrganizationComplianceEligibilityStatuses.Requested,
                actorReference,
                clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return ApplicationResult<OrganizationComplianceStatusDto>.Failure(
                ApplicationErrorCodes.ComplianceInvalidTransition,
                ex.Message);
        }

        await capabilities.UpdateAsync(capability, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await audit.WriteAsync(
            $"platform-user:{actorUserId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.OrganizationComplianceRequested,
            nameof(OrganizationSalesDocumentCapability),
            organizationId.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId,
            summary: $"Compliance eligibility set to {capability.ComplianceEligibilityStatus}.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await getStatus.ExecuteAsync(organizationId, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class TransitionOrganizationComplianceEligibility(
    EnsureOrganizationSalesDocumentCapability ensureCapability,
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    IOrganizationMembershipRepository memberships,
    IOrganizationSalesDocumentAcknowledgmentRepository acknowledgments,
    ISalesDocumentEducationVersionProvider versions,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<OrganizationComplianceStatusDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string targetStatus,
        string actorReference,
        CancellationToken cancellationToken = default)
    {
        if (!OrganizationComplianceEligibilityStatuses.IsKnown(targetStatus))
        {
            return ApplicationResult<OrganizationComplianceStatusDto>.Failure(
                ApplicationErrorCodes.ComplianceInvalidTransition,
                $"Unknown compliance eligibility status '{targetStatus}'.");
        }

        var capability = await ensureCapability.ExecuteAsync(organizationId, cancellationToken).ConfigureAwait(false);
        var previous = capability.ComplianceEligibilityStatus;
        try
        {
            capability.TransitionEligibility(targetStatus, actorReference, clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return ApplicationResult<OrganizationComplianceStatusDto>.Failure(
                ApplicationErrorCodes.ComplianceInvalidTransition,
                ex.Message);
        }

        await capabilities.UpdateAsync(capability, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var action = ResolveAuditAction(targetStatus);
        await audit.WriteAsync(
            actorReference.StartsWith("platform-user:", StringComparison.Ordinal)
                ? actorReference
                : $"platform-user:{actorReference}",
            AuditActorType.PlatformUser,
            action,
            nameof(OrganizationSalesDocumentCapability),
            organizationId.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId,
            summary: $"Compliance eligibility {previous} → {capability.ComplianceEligibilityStatus}.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await new GetOrganizationComplianceStatus(
                capabilities,
                memberships,
                acknowledgments,
                versions)
            .ExecuteAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
    }

    private static string ResolveAuditAction(string targetStatus) =>
        targetStatus switch
        {
            OrganizationComplianceEligibilityStatuses.UnderReview =>
                PlatformAuditActions.OrganizationComplianceReviewStarted,
            OrganizationComplianceEligibilityStatuses.DocumentsRequired =>
                PlatformAuditActions.OrganizationComplianceDocumentsRequired,
            OrganizationComplianceEligibilityStatuses.Approved =>
                PlatformAuditActions.OrganizationComplianceApproved,
            OrganizationComplianceEligibilityStatuses.Rejected =>
                PlatformAuditActions.OrganizationComplianceRejected,
            OrganizationComplianceEligibilityStatuses.Suspended =>
                PlatformAuditActions.OrganizationComplianceSuspended,
            OrganizationComplianceEligibilityStatuses.Revoked =>
                PlatformAuditActions.OrganizationComplianceRevoked,
            OrganizationComplianceEligibilityStatuses.Requested =>
                PlatformAuditActions.OrganizationComplianceRequested,
            _ => PlatformAuditActions.OrganizationComplianceReviewStarted
        };
}

public sealed class SetOrganizationTaxDocumentIssuanceCapability(
    EnsureOrganizationSalesDocumentCapability ensureCapability,
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    IOrganizationMembershipRepository memberships,
    IOrganizationSalesDocumentAcknowledgmentRepository acknowledgments,
    ISalesDocumentEducationVersionProvider versions,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<OrganizationComplianceStatusDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        bool enabled,
        string actorReference,
        CancellationToken cancellationToken = default)
    {
        var capability = await ensureCapability.ExecuteAsync(organizationId, cancellationToken).ConfigureAwait(false);

        if (enabled)
        {
            var owner = await memberships
                .FindActiveOwnerByOrganizationAsync(organizationId, cancellationToken)
                .ConfigureAwait(false);
            if (owner is null)
            {
                return ApplicationResult<OrganizationComplianceStatusDto>.Failure(
                    ApplicationErrorCodes.ComplianceOwnerRequired,
                    "An active Organization Owner is required before enabling tax-document capability.");
            }

            var acknowledgment = await acknowledgments
                .FindAsync(organizationId, owner.UserId, versions.CurrentVersion, cancellationToken)
                .ConfigureAwait(false);
            if (acknowledgment is null)
            {
                return ApplicationResult<OrganizationComplianceStatusDto>.Failure(
                    ApplicationErrorCodes.SalesDocumentEducationOwnerRequired,
                    "The current Organization Owner must acknowledge sales-document education before issuance capability can be enabled.");
            }
        }

        try
        {
            capability.SetTaxDocumentIssuanceEnabled(enabled, actorReference, clock.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return ApplicationResult<OrganizationComplianceStatusDto>.Failure(
                ApplicationErrorCodes.ComplianceIssuancePreconditionFailed,
                ex.Message);
        }

        await capabilities.UpdateAsync(capability, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await audit.WriteAsync(
            actorReference.StartsWith("platform-user:", StringComparison.Ordinal)
                ? actorReference
                : $"platform-user:{actorReference}",
            AuditActorType.PlatformUser,
            enabled
                ? PlatformAuditActions.OrganizationTaxDocumentCapabilityEnabled
                : PlatformAuditActions.OrganizationTaxDocumentCapabilityDisabled,
            nameof(OrganizationSalesDocumentCapability),
            organizationId.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId,
            summary: enabled
                ? "Tax-document issuance capability enabled (implementation still unavailable)."
                : "Tax-document issuance capability disabled.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return await new GetOrganizationComplianceStatus(
                capabilities,
                memberships,
                acknowledgments,
                versions)
            .ExecuteAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
    }
}
