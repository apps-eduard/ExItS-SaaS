using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record OrganizationOnlineSupplierPaymentsCapabilityDto(
    Guid OrganizationId,
    string Status,
    DateTimeOffset? UpdatedAtUtc,
    string? UpdatedByActorReference,
    string? Reason);

public interface IOrganizationOnlineSupplierPaymentsCapabilityRepository
{
    Task<OrganizationOnlineSupplierPaymentsCapability?> GetByOrganizationIdAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        OrganizationOnlineSupplierPaymentsCapability capability,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        OrganizationOnlineSupplierPaymentsCapability capability,
        CancellationToken cancellationToken = default);
}

public sealed class GetOrganizationOnlineSupplierPaymentsCapability(
    IOrganizationOnlineSupplierPaymentsCapabilityRepository capabilities)
{
    public async Task<ApplicationResult<OrganizationOnlineSupplierPaymentsCapabilityDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var capability = await capabilities
            .GetByOrganizationIdAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);

        return ApplicationResult<OrganizationOnlineSupplierPaymentsCapabilityDto>.Success(
            ToDto(organizationId, capability));
    }

    internal static OrganizationOnlineSupplierPaymentsCapabilityDto ToDto(
        PlatformOrganizationId organizationId,
        OrganizationOnlineSupplierPaymentsCapability? capability) =>
        new(
            organizationId.Value,
            capability?.Status ?? OrganizationOnlineSupplierPaymentsStatuses.Disabled,
            capability?.UpdatedAtUtc,
            capability?.UpdatedByActorReference,
            capability?.Reason);
}

public sealed class EnsureOrganizationOnlineSupplierPaymentsCapability(
    IOrganizationOnlineSupplierPaymentsCapabilityRepository capabilities,
    IPlatformUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<OrganizationOnlineSupplierPaymentsCapability> ExecuteAsync(
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

        capability = OrganizationOnlineSupplierPaymentsCapability.CreateDefault(organizationId, clock.UtcNow);
        await capabilities.AddAsync(capability, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return capability;
    }
}

public sealed class SetOrganizationOnlineSupplierPaymentsCapability(
    EnsureOrganizationOnlineSupplierPaymentsCapability ensureCapability,
    IOrganizationOnlineSupplierPaymentsCapabilityRepository capabilities,
    IPlatformUnitOfWork unitOfWork,
    IClock clock,
    IAuditWriter audit)
{
    public async Task<ApplicationResult<OrganizationOnlineSupplierPaymentsCapabilityDto>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        string targetStatus,
        string actorReference,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (!OrganizationOnlineSupplierPaymentsStatuses.IsKnown(targetStatus))
        {
            return ApplicationResult<OrganizationOnlineSupplierPaymentsCapabilityDto>.Failure(
                ApplicationErrorCodes.OnlineSupplierPaymentsInvalidTransition,
                $"Unknown online supplier payments status '{targetStatus}'.");
        }

        var capability = await ensureCapability.ExecuteAsync(organizationId, cancellationToken)
            .ConfigureAwait(false);
        var previous = capability.Status;
        try
        {
            capability.Transition(targetStatus, actorReference, clock.UtcNow, reason);
        }
        catch (ArgumentException ex)
        {
            return ApplicationResult<OrganizationOnlineSupplierPaymentsCapabilityDto>.Failure(
                ApplicationErrorCodes.OnlineSupplierPaymentsInvalidTransition,
                ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return ApplicationResult<OrganizationOnlineSupplierPaymentsCapabilityDto>.Failure(
                ApplicationErrorCodes.OnlineSupplierPaymentsInvalidTransition,
                ex.Message);
        }

        await capabilities.UpdateAsync(capability, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await audit.WriteAsync(
            actorReference.StartsWith("platform-user:", StringComparison.Ordinal)
                ? actorReference
                : $"platform-user:{actorReference}",
            AuditActorType.PlatformUser,
            ResolveAuditAction(capability.Status),
            nameof(OrganizationOnlineSupplierPaymentsCapability),
            organizationId.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId,
            summary: $"Online supplier payments {previous} → {capability.Status}.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<OrganizationOnlineSupplierPaymentsCapabilityDto>.Success(
            GetOrganizationOnlineSupplierPaymentsCapability.ToDto(organizationId, capability));
    }

    private static string ResolveAuditAction(string status) =>
        status switch
        {
            OrganizationOnlineSupplierPaymentsStatuses.Available =>
                PlatformAuditActions.OrganizationOnlineSupplierPaymentsEnabled,
            OrganizationOnlineSupplierPaymentsStatuses.Suspended =>
                PlatformAuditActions.OrganizationOnlineSupplierPaymentsSuspended,
            _ => PlatformAuditActions.OrganizationOnlineSupplierPaymentsDisabled
        };
}
