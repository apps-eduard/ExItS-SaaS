using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalUtangInvitationDto(
    Guid Id,
    Guid DebtRelationshipId,
    Guid InviteeContactId,
    Guid InvitedByUserIdentityId,
    string? InviteTargetEmailMasked,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? DeclinedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    Guid? AcceptedByUserIdentityId,
    string? AcceptToken);

public sealed record CreatePersonalUtangInvitationRequest(Guid InviteeContactId);

public sealed record AcceptPersonalUtangInvitationRequest(string Token);

public sealed record AcceptPersonalUtangInvitationByIdRequest(Guid InvitationId);

public sealed record DeclinePersonalUtangInvitationByIdRequest(Guid InvitationId);

public sealed record PersonalUtangInvitationAcceptResultDto(
    Guid InvitationId,
    Guid DebtRelationshipId,
    Guid LinkedContactId,
    Guid LinkedUserIdentityId,
    bool CreatedOrganizationMembership,
    bool GrantedProductRole);

public sealed class CreatePersonalUtangInvitation
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalUtangInvitationRepository _invitations;
    private readonly IPlatformUserRepository _users;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePersonalUtangInvitation(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalContactRepository contacts,
        IPersonalUtangInvitationRepository invitations,
        IPlatformUserRepository users,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _contacts = contacts;
        _invitations = invitations;
        _users = users;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangInvitationDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid relationshipId,
        CreatePersonalUtangInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        var relationship = await _relationships.GetByIdAsync(
            PersonalDebtRelationshipId.From(relationshipId),
            cancellationToken).ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        if (!await PersonalUtangAccess.CanViewAsync(relationship, actingUserIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Not authorized for this personal debt relationship.");
        }

        var contactResult = await PersonalUtangAccess.RequireOwnedContactAsync(
            actingUserIdentityId,
            PersonalContactId.From(request.InviteeContactId),
            _contacts,
            cancellationToken).ConfigureAwait(false);
        if (!contactResult.IsSuccess)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                contactResult.ErrorCode!,
                contactResult.ErrorMessage!);
        }

        var contact = contactResult.Value!;
        string? inviteTargetEmail = contact.Email;
        if (contact.IsLinked)
        {
            // Identity association from People add is not Utang consent.
            // Still allow a relationship-scoped invitation; acceptance authorizes the shared ledger.
            if (contact.LinkedUserIdentityId is null)
            {
                return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                    ApplicationErrorCodes.PersonalUtangInvitationConflict,
                    "Contact link state is invalid.");
            }

            var linkedInvitee = await _users
                .GetByIdAsync(contact.LinkedUserIdentityId, cancellationToken)
                .ConfigureAwait(false);
            if (linkedInvitee is null || linkedInvitee.Status is not AccountStatus.Active)
            {
                return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                    ApplicationErrorCodes.UserNotFound,
                    "Linked ExItS user was not found.");
            }

            inviteTargetEmail = linkedInvitee.NormalizedEmail;
        }
        else if (contact.HasResolvedIdentity && contact.ResolvedUserIdentityId is not null)
        {
            // Resolved ExItS identity (People add) is not yet Utang-linked; still route the invite
            // inbox via that user's login email so accept-by-id can complete consent.
            var resolvedInvitee = await _users
                .GetByIdAsync(contact.ResolvedUserIdentityId, cancellationToken)
                .ConfigureAwait(false);
            if (resolvedInvitee is not null && resolvedInvitee.Status is AccountStatus.Active)
            {
                inviteTargetEmail = resolvedInvitee.NormalizedEmail;
            }
        }

        var isParticipantContact =
            relationship.CreditorContactId == contact.Id || relationship.DebtorContactId == contact.Id;
        if (!isParticipantContact)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Contact is not a participant on this relationship.");
        }

        var existing = await _invitations.FindPendingByRelationshipAndContactAsync(
            relationship.Id,
            contact.Id,
            cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            if (existing.IsExpired(_clock.UtcNow))
            {
                existing.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            else
            {
                return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                    ApplicationErrorCodes.PersonalUtangInvitationConflict,
                    "A pending invitation already exists for this contact.");
            }
        }

        try
        {
            // Anti-enumeration: never look up Platform Users by contact email/phone for unlinked contacts.
            var (invitation, acceptToken) = PersonalUtangInvitation.Create(
                relationship.Id,
                contact.Id,
                actingUserIdentityId,
                _clock.UtcNow,
                inviteTargetEmail: inviteTargetEmail,
                inviteTargetPhone: contact.Phone);

            await _invitations.AddAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangInvitationCreated,
                nameof(PersonalUtangInvitation),
                invitation.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang invitation created.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalUtangInvitationDto>.Success(ToDto(invitation, acceptToken));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static PersonalUtangInvitationDto ToDto(PersonalUtangInvitation invitation, string? acceptToken = null) =>
        new(
            invitation.Id.Value,
            invitation.DebtRelationshipId.Value,
            invitation.InviteeContactId.Value,
            invitation.InvitedByUserIdentityId.Value,
            MaskEmail(invitation.InviteTargetNormalizedEmail),
            invitation.Status.ToString(),
            invitation.CreatedAtUtc,
            invitation.UpdatedAtUtc,
            invitation.ExpiresAtUtc,
            invitation.AcceptedAtUtc,
            invitation.DeclinedAtUtc,
            invitation.RevokedAtUtc,
            invitation.AcceptedByUserIdentityId?.Value,
            acceptToken);

    private static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@', StringComparison.Ordinal))
        {
            return null;
        }

        var at = email.IndexOf('@');
        var local = email[..at];
        var domain = email[(at + 1)..];
        var maskedLocal = local.Length <= 1 ? "*" : local[0] + new string('*', Math.Min(local.Length - 1, 4));
        return $"{maskedLocal}@{domain}";
    }
}

public sealed class ListPersonalUtangInvitations
{
    private readonly IPersonalUtangInvitationRepository _invitations;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ListPersonalUtangInvitations(
        IPersonalUtangInvitationRepository invitations,
        IPlatformUserRepository users,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _users = users;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PersonalUtangInvitationDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default)
    {
        var sent = await _invitations.ListSentByUserAsync(userIdentityId, cancellationToken)
            .ConfigureAwait(false);
        var user = await _users.GetByIdAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        var inbox = user is null
            ? Array.Empty<PersonalUtangInvitation>()
            : await _invitations.ListPendingForEmailAsync(user.NormalizedEmail, cancellationToken)
                .ConfigureAwait(false);

        var merged = sent
            .Concat(inbox)
            .GroupBy(i => i.Id.Value)
            .Select(g => g.First())
            .OrderByDescending(i => i.CreatedAtUtc)
            .ToList();

        var expiredAny = false;
        foreach (var invitation in merged.Where(i => i.IsExpired(_clock.UtcNow)))
        {
            invitation.MarkExpired(_clock.UtcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            expiredAny = true;
        }

        if (expiredAny)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return merged.Select(i => CreatePersonalUtangInvitation.ToDto(i)).ToList();
    }
}

public sealed class ResendPersonalUtangInvitation
{
    private readonly IPersonalUtangInvitationRepository _invitations;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ResendPersonalUtangInvitation(
        IPersonalUtangInvitationRepository invitations,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangInvitationDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _invitations.GetByIdAsync(
            PersonalUtangInvitationId.From(invitationId),
            cancellationToken).ConfigureAwait(false);
        if (invitation is null || invitation.InvitedByUserIdentityId != actingUserIdentityId)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        try
        {
            if (invitation.IsExpired(_clock.UtcNow))
            {
                invitation.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                    DomainErrorCodes.PersonalUtangInvitationExpired,
                    "Invitation has expired.");
            }

            var token = invitation.Resend(_clock.UtcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangInvitationResent,
                nameof(PersonalUtangInvitation),
                invitation.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang invitation resent.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalUtangInvitationDto>.Success(
                CreatePersonalUtangInvitation.ToDto(invitation, token));
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.PersonalUtangInvitationRateLimited)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationRateLimited,
                ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokePersonalUtangInvitation
{
    private readonly IPersonalUtangInvitationRepository _invitations;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokePersonalUtangInvitation(
        IPersonalUtangInvitationRepository invitations,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangInvitationDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        var invitation = await _invitations.GetByIdAsync(
            PersonalUtangInvitationId.From(invitationId),
            cancellationToken).ConfigureAwait(false);
        if (invitation is null || invitation.InvitedByUserIdentityId != actingUserIdentityId)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        try
        {
            invitation.Revoke(_clock.UtcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangInvitationRevoked,
                nameof(PersonalUtangInvitation),
                invitation.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang invitation revoked.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalUtangInvitationDto>.Success(
                CreatePersonalUtangInvitation.ToDto(invitation));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeclinePersonalUtangInvitation
{
    private readonly IPersonalUtangInvitationRepository _invitations;
    private readonly IPlatformUserRepository _users;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeclinePersonalUtangInvitation(
        IPersonalUtangInvitationRepository invitations,
        IPlatformUserRepository users,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _users = users;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangInvitationDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        string token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        string tokenHash;
        try
        {
            tokenHash = PersonalUtangInvitation.HashToken(token);
        }
        catch (DomainException)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        var invitation = await _invitations.FindPendingByTokenHashAsync(tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (invitation is null)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        var user = await _users.GetByIdAsync(actingUserIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User was not found.");
        }

        try
        {
            if (invitation.IsExpired(_clock.UtcNow))
            {
                invitation.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                    DomainErrorCodes.PersonalUtangInvitationExpired,
                    "Invitation has expired.");
            }

            if (!string.IsNullOrWhiteSpace(invitation.InviteTargetNormalizedEmail)
                && !string.Equals(
                    user.NormalizedEmail,
                    invitation.InviteTargetNormalizedEmail,
                    StringComparison.Ordinal))
            {
                return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                    ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                    "Invitation was not found or is no longer pending.");
            }

            invitation.Decline(_clock.UtcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangInvitationDeclined,
                nameof(PersonalUtangInvitation),
                invitation.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang invitation declined.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalUtangInvitationDto>.Success(
                CreatePersonalUtangInvitation.ToDto(invitation));
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.PersonalUtangInvitationExpired)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AcceptPersonalUtangInvitation
{
    private readonly IPersonalUtangInvitationRepository _invitations;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPlatformUserRepository _users;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcceptPersonalUtangInvitation(
        IPersonalUtangInvitationRepository invitations,
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        IPlatformUserRepository users,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _contacts = contacts;
        _relationships = relationships;
        _users = users;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangInvitationAcceptResultDto>> ExecuteAsync(
        PlatformUserId acceptingUserIdentityId,
        AcceptPersonalUtangInvitationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        string tokenHash;
        try
        {
            tokenHash = PersonalUtangInvitation.HashToken(request.Token);
        }
        catch (DomainException)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        var invitation = await _invitations.FindPendingByTokenHashAsync(tokenHash, cancellationToken)
            .ConfigureAwait(false);
        if (invitation is null)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        var user = await _users.GetByIdAsync(acceptingUserIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User was not found.");
        }

        var contact = await _contacts.GetByIdAsync(invitation.InviteeContactId, cancellationToken)
            .ConfigureAwait(false);
        if (contact is null)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalContactNotFound,
                "Personal contact was not found.");
        }

        var relationship = await _relationships.GetByIdAsync(invitation.DebtRelationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        try
        {
            if (invitation.IsExpired(_clock.UtcNow))
            {
                invitation.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await _auditWriter.WriteAsync(
                    $"platform-user:{acceptingUserIdentityId.Value:D}",
                    AuditActorType.PlatformUser,
                    PlatformAuditActions.PersonalUtangInvitationExpired,
                    nameof(PersonalUtangInvitation),
                    invitation.Id.Value.ToString("D"),
                    AuditOutcome.Failed,
                    summary: "Personal Utang invitation expired before acceptance.",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                    DomainErrorCodes.PersonalUtangInvitationExpired,
                    "Invitation has expired.");
            }

            if (contact.IsLinked && contact.LinkedUserIdentityId != acceptingUserIdentityId)
            {
                return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                    ApplicationErrorCodes.PersonalContactLinkConflict,
                    "Contact is linked to a different ExItS identity.");
            }

            invitation.Accept(acceptingUserIdentityId, user.NormalizedEmail, _clock.UtcNow);
            if (!contact.IsLinked)
            {
                contact.LinkUser(acceptingUserIdentityId, _clock.UtcNow);
            }

            relationship.AuthorizeLinkedParticipant(
                contact.Id,
                acceptingUserIdentityId,
                _clock.UtcNow);

            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _contacts.UpdateAsync(contact, cancellationToken).ConfigureAwait(false);
            await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{acceptingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangInvitationAccepted,
                nameof(PersonalUtangInvitation),
                invitation.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang invitation accepted; participant linked. No organization membership or product role granted.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{acceptingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalContactLinked,
                nameof(PersonalContact),
                contact.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal contact linked after explicit invitation acceptance.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{acceptingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangParticipantAuthorized,
                nameof(PersonalDebtRelationship),
                relationship.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Shared Personal Utang relationship view authorized for linked participant.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Success(
                new PersonalUtangInvitationAcceptResultDto(
                    invitation.Id.Value,
                    relationship.Id.Value,
                    contact.Id.Value,
                    acceptingUserIdentityId.Value,
                    CreatedOrganizationMembership: false,
                    GrantedProductRole: false));
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.PersonalUtangInvitationExpired)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AcceptPersonalUtangInvitationById
{
    private readonly IPersonalUtangInvitationRepository _invitations;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPlatformUserRepository _users;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public AcceptPersonalUtangInvitationById(
        IPersonalUtangInvitationRepository invitations,
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        IPlatformUserRepository users,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _contacts = contacts;
        _relationships = relationships;
        _users = users;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangInvitationAcceptResultDto>> ExecuteAsync(
        PlatformUserId acceptingUserIdentityId,
        AcceptPersonalUtangInvitationByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.InvitationId == Guid.Empty)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        var invitation = await _invitations
            .GetByIdAsync(PersonalUtangInvitationId.From(request.InvitationId), cancellationToken)
            .ConfigureAwait(false);
        if (invitation is null || invitation.Status != PersonalUtangInvitationStatus.Pending)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        var user = await _users.GetByIdAsync(acceptingUserIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User was not found.");
        }

        if (string.IsNullOrWhiteSpace(invitation.InviteTargetNormalizedEmail)
            || !string.Equals(user.NormalizedEmail, invitation.InviteTargetNormalizedEmail, StringComparison.Ordinal))
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        var contact = await _contacts.GetByIdAsync(invitation.InviteeContactId, cancellationToken)
            .ConfigureAwait(false);
        if (contact is null)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalContactNotFound,
                "Personal contact was not found.");
        }

        var relationship = await _relationships.GetByIdAsync(invitation.DebtRelationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        try
        {
            if (invitation.IsExpired(_clock.UtcNow))
            {
                invitation.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                    DomainErrorCodes.PersonalUtangInvitationExpired,
                    "Invitation has expired.");
            }

            if (contact.IsLinked && contact.LinkedUserIdentityId != acceptingUserIdentityId)
            {
                return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(
                    ApplicationErrorCodes.PersonalContactLinkConflict,
                    "Contact is linked to a different ExItS identity.");
            }

            invitation.Accept(acceptingUserIdentityId, user.NormalizedEmail, _clock.UtcNow);
            if (!contact.IsLinked)
            {
                contact.LinkUser(acceptingUserIdentityId, _clock.UtcNow);
            }

            relationship.AuthorizeLinkedParticipant(
                contact.Id,
                acceptingUserIdentityId,
                _clock.UtcNow);

            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _contacts.UpdateAsync(contact, cancellationToken).ConfigureAwait(false);
            await _relationships.UpdateAsync(relationship, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{acceptingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangInvitationAccepted,
                nameof(PersonalUtangInvitation),
                invitation.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang invitation accepted by invitation id; participant linked.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Success(
                new PersonalUtangInvitationAcceptResultDto(
                    invitation.Id.Value,
                    relationship.Id.Value,
                    contact.Id.Value,
                    acceptingUserIdentityId.Value,
                    CreatedOrganizationMembership: false,
                    GrantedProductRole: false));
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.PersonalUtangInvitationExpired)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangInvitationAcceptResultDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class DeclinePersonalUtangInvitationById
{
    private readonly IPersonalUtangInvitationRepository _invitations;
    private readonly IPlatformUserRepository _users;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeclinePersonalUtangInvitationById(
        IPersonalUtangInvitationRepository invitations,
        IPlatformUserRepository users,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _invitations = invitations;
        _users = users;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalUtangInvitationDto>> ExecuteAsync(
        PlatformUserId decliningUserIdentityId,
        DeclinePersonalUtangInvitationByIdRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.InvitationId == Guid.Empty)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        var invitation = await _invitations
            .GetByIdAsync(PersonalUtangInvitationId.From(request.InvitationId), cancellationToken)
            .ConfigureAwait(false);
        if (invitation is null || invitation.Status != PersonalUtangInvitationStatus.Pending)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        var user = await _users.GetByIdAsync(decliningUserIdentityId, cancellationToken).ConfigureAwait(false);
        if (user is null)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.UserNotFound,
                "User was not found.");
        }

        if (string.IsNullOrWhiteSpace(invitation.InviteTargetNormalizedEmail)
            || !string.Equals(user.NormalizedEmail, invitation.InviteTargetNormalizedEmail, StringComparison.Ordinal))
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                ApplicationErrorCodes.PersonalUtangInvitationNotFound,
                "Invitation was not found or is no longer pending.");
        }

        try
        {
            if (invitation.IsExpired(_clock.UtcNow))
            {
                invitation.MarkExpired(_clock.UtcNow);
                await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PersonalUtangInvitationDto>.Failure(
                    DomainErrorCodes.PersonalUtangInvitationExpired,
                    "Invitation has expired.");
            }

            invitation.Decline(_clock.UtcNow);
            await _invitations.UpdateAsync(invitation, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{decliningUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalUtangInvitationDeclined,
                nameof(PersonalUtangInvitation),
                invitation.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang invitation declined by invitation id.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalUtangInvitationDto>.Success(
                CreatePersonalUtangInvitation.ToDto(invitation));
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.PersonalUtangInvitationExpired)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalUtangInvitationDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
