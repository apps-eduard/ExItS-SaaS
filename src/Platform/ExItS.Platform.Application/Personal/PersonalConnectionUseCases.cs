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

public sealed record PersonalConnectionRequestDto(
    Guid Id,
    Guid RequesterUserIdentityId,
    Guid TargetUserIdentityId,
    Guid RequesterContactId,
    string RequesterDisplayName,
    string? RequesterPublicUserId,
    string? TargetPublicUserId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset? AcceptedAtUtc,
    DateTimeOffset? DeclinedAtUtc,
    DateTimeOffset? RevokedAtUtc,
    string Direction);

internal static class PersonalConnectionSupport
{
    public const string NotificationRelatedType = "PersonalConnectionRequest";

    public static async Task<bool> IsBlockedEitherWayAsync(
        PlatformUserId userA,
        PlatformUserId userB,
        IPersonalContactRepository contacts,
        CancellationToken cancellationToken)
    {
        var aBlockedB = await contacts
            .FindActiveBlockedByOwnerForUserAsync(userA, userB, cancellationToken)
            .ConfigureAwait(false);
        if (aBlockedB is not null)
        {
            return true;
        }

        var bBlockedA = await contacts
            .FindActiveBlockedByOwnerForUserAsync(userB, userA, cancellationToken)
            .ConfigureAwait(false);
        return bBlockedA is not null;
    }

    public static async Task UnlinkSymmetricAsync(
        PlatformUserId ownerA,
        PlatformUserId peerB,
        IPersonalContactRepository contacts,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var contactA = await contacts
            .FindActiveByOwnerAndResolvedUserAsync(ownerA, peerB, cancellationToken)
            .ConfigureAwait(false);
        if (contactA is not null && contactA.IsConnected)
        {
            contactA.Unlink(clock.UtcNow);
            await contacts.UpdateAsync(contactA, cancellationToken).ConfigureAwait(false);
        }

        var contactB = await contacts
            .FindActiveByOwnerAndResolvedUserAsync(peerB, ownerA, cancellationToken)
            .ConfigureAwait(false);
        if (contactB is not null && contactB.IsConnected)
        {
            contactB.Unlink(clock.UtcNow);
            await contacts.UpdateAsync(contactB, cancellationToken).ConfigureAwait(false);
        }
    }

    public static async Task InvalidatePendingRequestsBetweenAsync(
        PlatformUserId userA,
        PlatformUserId userB,
        IPersonalConnectionRequestRepository requests,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var pending = await requests
            .ListPendingBetweenUsersAsync(userA, userB, cancellationToken)
            .ConfigureAwait(false);
        foreach (var request in pending)
        {
            request.InvalidatePending(clock.UtcNow);
            await requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static async Task<(string DisplayName, string? PublicUserId)> ResolveUserPresentationAsync(
        PlatformUserId userIdentityId,
        IPlatformUserRepository users,
        GetOrAssignPublicIdentity publicIdentity,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        var displayName = user?.DisplayName ?? "Someone";
        string? publicUserId = null;
        var identity = await publicIdentity.ExecuteAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        if (identity.IsSuccess)
        {
            publicUserId = identity.Value!.PublicUserId;
        }

        return (displayName, publicUserId);
    }
}

public sealed class ListPersonalConnectionRequests
{
    private readonly IPersonalConnectionRequestRepository _requests;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPlatformUserRepository _users;
    private readonly GetOrAssignPublicIdentity _publicIdentity;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ListPersonalConnectionRequests(
        IPersonalConnectionRequestRepository requests,
        IPersonalContactRepository contacts,
        IPlatformUserRepository users,
        GetOrAssignPublicIdentity publicIdentity,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _contacts = contacts;
        _users = users;
        _publicIdentity = publicIdentity;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PersonalConnectionRequestDto>> ExecuteAsync(
        PlatformUserId userIdentityId,
        CancellationToken cancellationToken = default)
    {
        var list = await _requests.ListForUserAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        var expiredAny = false;
        foreach (var request in list.Where(r => r.IsExpired(_clock.UtcNow)))
        {
            request.MarkExpired(_clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            expiredAny = true;
        }

        if (expiredAny)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            list = await _requests.ListForUserAsync(userIdentityId, cancellationToken).ConfigureAwait(false);
        }

        var result = new List<PersonalConnectionRequestDto>();
        foreach (var request in list)
        {
            result.Add(await ToDtoAsync(request, userIdentityId, cancellationToken).ConfigureAwait(false));
        }

        return result;
    }

    internal async Task<PersonalConnectionRequestDto> ToDtoAsync(
        PersonalConnectionRequest request,
        PlatformUserId viewerUserIdentityId,
        CancellationToken cancellationToken)
    {
        var (requesterName, requesterPublic) = await PersonalConnectionSupport
            .ResolveUserPresentationAsync(
                request.RequesterUserIdentityId,
                _users,
                _publicIdentity,
                cancellationToken)
            .ConfigureAwait(false);

        var (_, targetPublic) = await PersonalConnectionSupport
            .ResolveUserPresentationAsync(
                request.TargetUserIdentityId,
                _users,
                _publicIdentity,
                cancellationToken)
            .ConfigureAwait(false);

        var direction = request.TargetUserIdentityId == viewerUserIdentityId ? "Received" : "Sent";
        return new PersonalConnectionRequestDto(
            request.Id.Value,
            request.RequesterUserIdentityId.Value,
            request.TargetUserIdentityId.Value,
            request.RequesterContactId.Value,
            requesterName,
            requesterPublic,
            targetPublic,
            request.Status.ToString(),
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.ExpiresAtUtc,
            request.AcceptedAtUtc,
            request.DeclinedAtUtc,
            request.RevokedAtUtc,
            direction);
    }
}

public sealed class RequestPersonalConnection
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalConnectionRequestRepository _requests;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IPlatformUserRepository _users;
    private readonly GetOrAssignPublicIdentity _publicIdentity;
    private readonly ListPersonalConnectionRequests _lister;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RequestPersonalConnection(
        IPersonalContactRepository contacts,
        IPersonalConnectionRequestRepository requests,
        IPersonalInAppNotificationRepository notifications,
        IPlatformUserRepository users,
        GetOrAssignPublicIdentity publicIdentity,
        ListPersonalConnectionRequests lister,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _requests = requests;
        _notifications = notifications;
        _users = users;
        _publicIdentity = publicIdentity;
        _lister = lister;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalConnectionRequestDto>> ExecuteAsync(
        PlatformUserId requesterUserIdentityId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var contactResult = await PersonalUtangAccess.RequireOwnedContactAsync(
            requesterUserIdentityId,
            PersonalContactId.From(contactId),
            _contacts,
            cancellationToken).ConfigureAwait(false);
        if (!contactResult.IsSuccess)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                contactResult.ErrorCode!,
                contactResult.ErrorMessage!);
        }

        var contact = contactResult.Value!;
        if (!contact.HasResolvedIdentity || contact.ResolvedUserIdentityId is null)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                ApplicationErrorCodes.PersonalContactNotFound,
                "Contact must resolve to an ExItS identity before requesting connection.");
        }

        if (contact.IsConnected)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                ApplicationErrorCodes.PersonalConnectionRequestConflict,
                "Contact is already connected.");
        }

        if (contact.IsBlocked)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                ApplicationErrorCodes.PersonalConnectionBlocked,
                "Unblock this person before requesting connection.");
        }

        var targetUserIdentityId = contact.ResolvedUserIdentityId;
        if (await PersonalConnectionSupport.IsBlockedEitherWayAsync(
                requesterUserIdentityId,
                targetUserIdentityId,
                _contacts,
                cancellationToken).ConfigureAwait(false))
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                ApplicationErrorCodes.PersonalConnectionBlocked,
                "Connection is blocked.");
        }

        var existingPending = await _requests
            .FindPendingBetweenUsersAsync(requesterUserIdentityId, targetUserIdentityId, cancellationToken)
            .ConfigureAwait(false);
        if (existingPending is not null)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                ApplicationErrorCodes.PersonalConnectionRequestConflict,
                "A pending connection request already exists.");
        }

        try
        {
            var request = PersonalConnectionRequest.Create(
                requesterUserIdentityId,
                targetUserIdentityId,
                contact.Id,
                _clock.UtcNow);

            await _requests.AddAsync(request, cancellationToken).ConfigureAwait(false);

            var (requesterName, _) = await PersonalConnectionSupport
                .ResolveUserPresentationAsync(
                    requesterUserIdentityId,
                    _users,
                    _publicIdentity,
                    cancellationToken)
                .ConfigureAwait(false);

            var notification = PersonalInAppNotification.Create(
                targetUserIdentityId,
                "Connection request",
                $"{requesterName} sent you a connection request",
                PersonalConnectionSupport.NotificationRelatedType,
                _clock.UtcNow,
                request.Id.Value.ToString("D"));
            await _notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{requesterUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalConnectionRequestCreated,
                nameof(PersonalConnectionRequest),
                request.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal connection request created.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalConnectionRequestDto>.Success(
                await _lister.ToDtoAsync(request, requesterUserIdentityId, cancellationToken).ConfigureAwait(false));
        }
        catch (PersistenceConflictException ex) when (
            ex.ErrorCode == ApplicationErrorCodes.PersonalConnectionRequestConflict)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class AcceptPersonalConnectionRequest
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalConnectionRequestRepository _requests;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IPlatformUserRepository _users;
    private readonly GetOrAssignPublicIdentity _publicIdentity;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    private readonly ListPersonalConnectionRequests _lister;

    public AcceptPersonalConnectionRequest(
        IPersonalContactRepository contacts,
        IPersonalConnectionRequestRepository requests,
        IPersonalInAppNotificationRepository notifications,
        IPlatformUserRepository users,
        GetOrAssignPublicIdentity publicIdentity,
        ListPersonalConnectionRequests lister,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _requests = requests;
        _notifications = notifications;
        _users = users;
        _publicIdentity = publicIdentity;
        _lister = lister;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalConnectionRequestDto>> ExecuteAsync(
        PlatformUserId acceptingUserIdentityId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests
            .GetByIdAsync(PersonalConnectionRequestId.From(requestId), cancellationToken)
            .ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                ApplicationErrorCodes.PersonalConnectionRequestNotFound,
                "Connection request was not found.");
        }

        if (request.TargetUserIdentityId != acceptingUserIdentityId)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                ApplicationErrorCodes.PersonalConnectionUnauthorized,
                "Not authorized for this connection request.");
        }

        try
        {
            if (request.IsExpired(_clock.UtcNow))
            {
                request.MarkExpired(_clock.UtcNow);
                await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                    DomainErrorCodes.PersonalConnectionRequestExpired,
                    "Connection request has expired.");
            }

            if (request.Status != PersonalConnectionRequestStatus.Pending)
            {
                return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                    DomainErrorCodes.InvalidPersonalConnectionRequestStatusTransition,
                    "Connection request is no longer pending.");
            }

            if (await PersonalConnectionSupport.IsBlockedEitherWayAsync(
                    request.RequesterUserIdentityId,
                    acceptingUserIdentityId,
                    _contacts,
                    cancellationToken).ConfigureAwait(false))
            {
                return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                    ApplicationErrorCodes.PersonalConnectionBlocked,
                    "Connection is blocked.");
            }

            request.Accept(acceptingUserIdentityId, _clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);

            var requesterContact = await _contacts
                .GetByIdAsync(request.RequesterContactId, cancellationToken)
                .ConfigureAwait(false);
            if (requesterContact is null)
            {
                return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                    ApplicationErrorCodes.PersonalContactNotFound,
                    "Requester contact was not found.");
            }

            requesterContact.LinkUser(acceptingUserIdentityId, _clock.UtcNow);
            await _contacts.UpdateAsync(requesterContact, cancellationToken).ConfigureAwait(false);

            var (requesterPlatformName, requesterPublicUserId) = await PersonalConnectionSupport
                .ResolveUserPresentationAsync(
                    request.RequesterUserIdentityId,
                    _users,
                    _publicIdentity,
                    cancellationToken)
                .ConfigureAwait(false);

            await EnsureReciprocalContactAsync(
                acceptingUserIdentityId,
                request.RequesterUserIdentityId,
                requesterPlatformName,
                requesterPublicUserId,
                cancellationToken).ConfigureAwait(false);

            var accepter = await _users.GetByIdAsync(acceptingUserIdentityId, cancellationToken).ConfigureAwait(false);
            var accepterName = accepter?.DisplayName ?? "Someone";
            var notification = PersonalInAppNotification.Create(
                request.RequesterUserIdentityId,
                "Connection accepted",
                $"{accepterName} accepted your connection request",
                PersonalConnectionSupport.NotificationRelatedType,
                _clock.UtcNow,
                request.Id.Value.ToString("D"));
            await _notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{acceptingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalConnectionRequestAccepted,
                nameof(PersonalConnectionRequest),
                request.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal connection request accepted.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalConnectionRequestDto>.Success(
                await _lister.ToDtoAsync(request, acceptingUserIdentityId, cancellationToken).ConfigureAwait(false));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task EnsureReciprocalContactAsync(
        PlatformUserId ownerUserIdentityId,
        PlatformUserId peerUserIdentityId,
        string peerDisplayName,
        string? peerPublicUserId,
        CancellationToken cancellationToken)
    {
        var existing = await _contacts
            .FindActiveByOwnerAndResolvedUserAsync(ownerUserIdentityId, peerUserIdentityId, cancellationToken)
            .ConfigureAwait(false);

        var publicUserId = peerPublicUserId ?? string.Empty;
        if (string.IsNullOrWhiteSpace(publicUserId))
        {
            var publicIdentity = await _publicIdentity.ExecuteAsync(peerUserIdentityId, cancellationToken).ConfigureAwait(false);
            publicUserId = publicIdentity.IsSuccess ? publicIdentity.Value!.PublicUserId : string.Empty;
        }

        if (existing is null)
        {
            var reciprocal = PersonalContact.Create(
                ownerUserIdentityId,
                peerDisplayName,
                phone: null,
                email: null,
                _clock.UtcNow);
            if (!string.IsNullOrWhiteSpace(publicUserId))
            {
                reciprocal.ResolveIdentity(peerUserIdentityId, publicUserId, _clock.UtcNow);
            }

            reciprocal.LinkUser(peerUserIdentityId, _clock.UtcNow);
            await _contacts.AddAsync(reciprocal, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (!existing.HasResolvedIdentity && !string.IsNullOrWhiteSpace(publicUserId))
        {
            existing.ResolveIdentity(peerUserIdentityId, publicUserId, _clock.UtcNow);
        }

        if (!existing.IsConnected)
        {
            existing.LinkUser(peerUserIdentityId, _clock.UtcNow);
        }

        await _contacts.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
    }
}

public sealed class DeclinePersonalConnectionRequest
{
    private readonly IPersonalConnectionRequestRepository _requests;
    private readonly ListPersonalConnectionRequests _lister;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeclinePersonalConnectionRequest(
        IPersonalConnectionRequestRepository requests,
        ListPersonalConnectionRequests lister,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _lister = lister;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalConnectionRequestDto>> ExecuteAsync(
        PlatformUserId decliningUserIdentityId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests
            .GetByIdAsync(PersonalConnectionRequestId.From(requestId), cancellationToken)
            .ConfigureAwait(false);
        if (request is null)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                ApplicationErrorCodes.PersonalConnectionRequestNotFound,
                "Connection request was not found.");
        }

        try
        {
            request.Decline(decliningUserIdentityId, _clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{decliningUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalConnectionRequestDeclined,
                nameof(PersonalConnectionRequest),
                request.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal connection request declined.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalConnectionRequestDto>.Success(
                await _lister.ToDtoAsync(request, decliningUserIdentityId, cancellationToken).ConfigureAwait(false));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class RevokePersonalConnectionRequest
{
    private readonly IPersonalConnectionRequestRepository _requests;
    private readonly ListPersonalConnectionRequests _lister;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RevokePersonalConnectionRequest(
        IPersonalConnectionRequestRepository requests,
        ListPersonalConnectionRequests lister,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _lister = lister;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalConnectionRequestDto>> ExecuteAsync(
        PlatformUserId requesterUserIdentityId,
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests
            .GetByIdAsync(PersonalConnectionRequestId.From(requestId), cancellationToken)
            .ConfigureAwait(false);
        if (request is null || request.RequesterUserIdentityId != requesterUserIdentityId)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(
                ApplicationErrorCodes.PersonalConnectionRequestNotFound,
                "Connection request was not found.");
        }

        try
        {
            request.Revoke(_clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{requesterUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalConnectionRequestRevoked,
                nameof(PersonalConnectionRequest),
                request.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal connection request revoked.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalConnectionRequestDto>.Success(
                await _lister.ToDtoAsync(request, requesterUserIdentityId, cancellationToken).ConfigureAwait(false));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalConnectionRequestDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class UnlinkPersonalContact
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UnlinkPersonalContact(
        IPersonalContactRepository contacts,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalContactDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var contactResult = await PersonalUtangAccess.RequireOwnedContactAsync(
            ownerUserIdentityId,
            PersonalContactId.From(contactId),
            _contacts,
            cancellationToken).ConfigureAwait(false);
        if (!contactResult.IsSuccess)
        {
            return ApplicationResult<PersonalContactDto>.Failure(
                contactResult.ErrorCode!,
                contactResult.ErrorMessage!);
        }

        var contact = contactResult.Value!;
        if (!contact.IsConnected || contact.ResolvedUserIdentityId is null)
        {
            return ApplicationResult<PersonalContactDto>.Failure(
                ApplicationErrorCodes.PersonalContactNotFound,
                "Contact is not connected.");
        }

        var peer = contact.ResolvedUserIdentityId;
        await PersonalConnectionSupport.UnlinkSymmetricAsync(
            ownerUserIdentityId,
            peer,
            _contacts,
            _clock,
            cancellationToken).ConfigureAwait(false);

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{ownerUserIdentityId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PersonalContactUnlinked,
            nameof(PersonalContact),
            contact.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Personal contact unlinked.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var refreshed = await _contacts.GetByIdAsync(contact.Id, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PersonalContactDto>.Success(CreatePersonalContact.ToDto(refreshed!));
    }
}

public sealed class BlockPersonalContact
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalConnectionRequestRepository _requests;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public BlockPersonalContact(
        IPersonalContactRepository contacts,
        IPersonalConnectionRequestRepository requests,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _requests = requests;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalContactDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var contactResult = await PersonalUtangAccess.RequireOwnedContactAsync(
            ownerUserIdentityId,
            PersonalContactId.From(contactId),
            _contacts,
            cancellationToken).ConfigureAwait(false);
        if (!contactResult.IsSuccess)
        {
            return ApplicationResult<PersonalContactDto>.Failure(
                contactResult.ErrorCode!,
                contactResult.ErrorMessage!);
        }

        var contact = contactResult.Value!;
        if (!contact.HasResolvedIdentity)
        {
            return ApplicationResult<PersonalContactDto>.Failure(
                ApplicationErrorCodes.PersonalContactNotFound,
                "Only identified contacts can be blocked.");
        }

        if (contact.ResolvedUserIdentityId is not null)
        {
            await PersonalConnectionSupport.UnlinkSymmetricAsync(
                ownerUserIdentityId,
                contact.ResolvedUserIdentityId,
                _contacts,
                _clock,
                cancellationToken).ConfigureAwait(false);

            await PersonalConnectionSupport.InvalidatePendingRequestsBetweenAsync(
                ownerUserIdentityId,
                contact.ResolvedUserIdentityId,
                _requests,
                _clock,
                cancellationToken).ConfigureAwait(false);
        }

        contact.Block(_clock.UtcNow);
        await _contacts.UpdateAsync(contact, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{ownerUserIdentityId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PersonalContactBlocked,
            nameof(PersonalContact),
            contact.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Personal contact blocked.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PersonalContactDto>.Success(CreatePersonalContact.ToDto(contact));
    }
}

public sealed class UnblockPersonalContact
{
    private readonly IPersonalContactRepository _contacts;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UnblockPersonalContact(
        IPersonalContactRepository contacts,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _contacts = contacts;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalContactDto>> ExecuteAsync(
        PlatformUserId ownerUserIdentityId,
        Guid contactId,
        CancellationToken cancellationToken = default)
    {
        var contactResult = await PersonalUtangAccess.RequireOwnedContactAsync(
            ownerUserIdentityId,
            PersonalContactId.From(contactId),
            _contacts,
            cancellationToken).ConfigureAwait(false);
        if (!contactResult.IsSuccess)
        {
            return ApplicationResult<PersonalContactDto>.Failure(
                contactResult.ErrorCode!,
                contactResult.ErrorMessage!);
        }

        var contact = contactResult.Value!;
        contact.Unblock(_clock.UtcNow);
        await _contacts.UpdateAsync(contact, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await _auditWriter.WriteAsync(
            $"platform-user:{ownerUserIdentityId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PersonalContactUnblocked,
            nameof(PersonalContact),
            contact.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            summary: "Personal contact unblocked.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PersonalContactDto>.Success(CreatePersonalContact.ToDto(contact));
    }
}
