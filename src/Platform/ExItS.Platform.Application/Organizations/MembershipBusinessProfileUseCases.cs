using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record MembershipBusinessProfileDto(
    Guid MembershipId,
    Guid OrganizationId,
    Guid UserId,
    string DisplayName,
    string Role,
    string? RoleDisplay,
    string? Department,
    string? JobTitle,
    string? WorkPhone,
    string? WorkEmail,
    bool IsBusinessContact,
    DateTimeOffset UpdatedAtUtc);

public sealed record UpdateOwnMembershipBusinessContactCommand(
    string? WorkPhone,
    string? WorkEmail,
    bool IsBusinessContact);

public sealed record UpdateManagedMembershipBusinessProfileCommand(
    string? Department,
    string? JobTitle,
    string? WorkPhone,
    string? WorkEmail,
    bool IsBusinessContact);

/// <summary>
/// Organization-specific business profile for a membership.
/// Never mutates PlatformUser personal identity or organization role/permissions.
/// </summary>
public sealed class MembershipBusinessProfileUseCases
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public MembershipBusinessProfileUseCases(
        IOrganizationMembershipRepository memberships,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _memberships = memberships;
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<MembershipBusinessProfileDto>> GetAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken = default)
    {
        var membership = await LoadMembershipAsync(organizationId, membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        return ApplicationResult<MembershipBusinessProfileDto>.Success(
            await MapAsync(membership, cancellationToken).ConfigureAwait(false));
    }

    public async Task<ApplicationResult<MembershipBusinessProfileDto>> UpdateOwnAsync(
        Guid organizationId,
        Guid membershipId,
        PlatformUserId actorUserId,
        UpdateOwnMembershipBusinessContactCommand command,
        CancellationToken cancellationToken = default)
    {
        var membership = await LoadMembershipAsync(organizationId, membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        if (membership.UserId != actorUserId)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(
                DomainErrorCodes.MembershipBusinessProfileSelfEditDenied,
                "You can only edit your own business contact fields.");
        }

        try
        {
            membership.UpdateOwnBusinessContact(
                command.WorkPhone,
                command.WorkEmail,
                command.IsBusinessContact,
                _clock.UtcNow);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<MembershipBusinessProfileDto>.Success(
            await MapAsync(membership, cancellationToken).ConfigureAwait(false));
    }

    public async Task<ApplicationResult<MembershipBusinessProfileDto>> UpdateManagedAsync(
        Guid organizationId,
        Guid membershipId,
        UpdateManagedMembershipBusinessProfileCommand command,
        string? actorReference = null,
        CancellationToken cancellationToken = default)
    {
        var membership = await LoadMembershipAsync(organizationId, membershipId, cancellationToken)
            .ConfigureAwait(false);
        if (membership is null)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(
                ApplicationErrorCodes.MembershipNotFound,
                "Organization membership was not found.");
        }

        try
        {
            membership.UpdateManagedBusinessProfile(
                command.Department,
                command.JobTitle,
                command.WorkPhone,
                command.WorkEmail,
                command.IsBusinessContact,
                _clock.UtcNow,
                actorReference);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<MembershipBusinessProfileDto>.Failure(ex.ErrorCode, ex.Message);
        }

        await _memberships.UpdateAsync(membership, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<MembershipBusinessProfileDto>.Success(
            await MapAsync(membership, cancellationToken).ConfigureAwait(false));
    }

    private async Task<OrganizationMembership?> LoadMembershipAsync(
        Guid organizationId,
        Guid membershipId,
        CancellationToken cancellationToken)
    {
        var membership = await _memberships
            .GetByIdAsync(OrganizationMembershipId.From(membershipId), cancellationToken)
            .ConfigureAwait(false);
        if (membership is null || membership.OrganizationId.Value != organizationId)
        {
            return null;
        }

        return membership;
    }

    private async Task<MembershipBusinessProfileDto> MapAsync(
        OrganizationMembership membership,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(membership.UserId, cancellationToken).ConfigureAwait(false);
        return new MembershipBusinessProfileDto(
            membership.Id.Value,
            membership.OrganizationId.Value,
            membership.UserId.Value,
            user?.DisplayName ?? string.Empty,
            membership.Role.ToString(),
            OrganizationRoleDisplay.ToDisplayLabel(membership.Role),
            membership.Department,
            membership.JobTitle,
            membership.WorkPhone,
            membership.WorkEmail,
            membership.IsBusinessContact,
            membership.UpdatedAtUtc);
    }
}
