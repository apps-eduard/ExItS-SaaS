using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public interface ISalesDocumentEducationVersionProvider
{
    string CurrentVersion { get; }
}

public sealed class SalesDocumentEducationVersionProvider : ISalesDocumentEducationVersionProvider
{
    public string CurrentVersion => SalesDocumentEducationVersions.Current;
}

public interface IOrganizationSalesDocumentAcknowledgmentRepository
{
    Task<OrganizationSalesDocumentAcknowledgment?> FindAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        string version,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationSalesDocumentAcknowledgment acknowledgment,
        CancellationToken cancellationToken = default);
}

public sealed record OrganizationSalesDocumentEducationStatusDto(
    Guid OrganizationId,
    string CurrentVersion,
    bool CurrentOwnerAcknowledged,
    DateTimeOffset? AcknowledgedAtUtc,
    Guid? AcknowledgedByUserId,
    bool RequiresOwnerAction,
    bool TransactionSummaryAvailable,
    bool TaxDocumentIssuanceEnabled,
    string DocumentMode);

public sealed class GetSalesDocumentEducationStatus(
    IOrganizationMembershipRepository memberships,
    IOrganizationSalesDocumentAcknowledgmentRepository acknowledgments,
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    ISalesDocumentEducationVersionProvider versions)
{
    public async Task<ApplicationResult<OrganizationSalesDocumentEducationStatusDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actorMembership = await memberships
            .FindActiveByUserAndOrganizationAsync(actorUserId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (actorMembership is null)
        {
            return ApplicationResult<OrganizationSalesDocumentEducationStatusDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "An active organization membership is required.");
        }

        var owner = await memberships
            .FindActiveOwnerByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var acknowledgment = owner is null
            ? null
            : await acknowledgments
                .FindAsync(organizationId, owner.UserId, versions.CurrentVersion, cancellationToken)
                .ConfigureAwait(false);
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        var acknowledged = owner is not null && acknowledgment is not null;
        return ApplicationResult<OrganizationSalesDocumentEducationStatusDto>.Success(new(
            organizationId.Value,
            versions.CurrentVersion,
            acknowledged,
            acknowledgment?.AcknowledgedAtUtc,
            acknowledgment?.UserId.Value,
            RequiresOwnerAction: !acknowledged,
            TransactionSummaryAvailable: true,
            TaxDocumentIssuanceEnabled: capability?.TaxDocumentIssuanceEnabled == true,
            DocumentMode: "TransactionSummary"));
    }
}

public sealed class AcknowledgeSalesDocumentEducation(
    IOrganizationMembershipRepository memberships,
    IOrganizationSalesDocumentAcknowledgmentRepository acknowledgments,
    IOrganizationSalesDocumentCapabilityRepository capabilities,
    ISalesDocumentEducationVersionProvider versions,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<OrganizationSalesDocumentEducationStatusDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId actorUserId,
        CancellationToken cancellationToken = default)
    {
        var owner = await memberships
            .FindActiveOwnerByOrganizationAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (owner is null || owner.UserId != actorUserId)
        {
            return ApplicationResult<OrganizationSalesDocumentEducationStatusDto>.Failure(
                ApplicationErrorCodes.SalesDocumentEducationOwnerRequired,
                "The current active Organization Owner must acknowledge this information.");
        }

        var acknowledgment = await acknowledgments
            .FindAsync(organizationId, actorUserId, versions.CurrentVersion, cancellationToken)
            .ConfigureAwait(false);
        if (acknowledgment is null)
        {
            acknowledgment = OrganizationSalesDocumentAcknowledgment.Create(
                organizationId,
                actorUserId,
                versions.CurrentVersion,
                clock.UtcNow,
                versions.CurrentVersion);
            await acknowledgments.AddAsync(acknowledgment, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await audit.WriteAsync(
                $"platform-user:{actorUserId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.OrganizationSalesDocumentEducationAcknowledged,
                nameof(OrganizationSalesDocumentAcknowledgment),
                acknowledgment.Id.ToString("D"),
                AuditOutcome.Succeeded,
                organizationId,
                summary: $"Organization Owner acknowledged sales-document education version {versions.CurrentVersion}.",
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<OrganizationSalesDocumentEducationStatusDto>.Success(new(
            organizationId.Value,
            versions.CurrentVersion,
            CurrentOwnerAcknowledged: true,
            acknowledgment.AcknowledgedAtUtc,
            acknowledgment.UserId.Value,
            RequiresOwnerAction: false,
            TransactionSummaryAvailable: true,
            TaxDocumentIssuanceEnabled: capability?.TaxDocumentIssuanceEnabled == true,
            DocumentMode: "TransactionSummary"));
    }
}
