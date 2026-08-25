using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Organizations;

public sealed record CustomerLinkReminderDto(
    Guid RequestId,
    Guid OrganizationId,
    Guid BusinessCustomerId,
    string Status,
    int ReminderCount,
    DateTimeOffset? LastRemindedAtUtc,
    DateTimeOffset? NextReminderEligibleAtUtc);

public sealed record PersonalBlockedBusinessDto(
    Guid OrganizationId,
    string OrganizationDisplayName,
    DateTimeOffset BlockedAtUtc);

public sealed record PersonalLinkedMerchantDisconnectDto(
    Guid OrganizationId,
    int RevokedLinkCount,
    bool BlockActivated);

/// <summary>Shared helpers for Personal↔Organization connection blocks.</summary>
public static class CustomerConnectionBlockSupport
{
    public const string OrgUnavailableMessage =
        "Customer connection requests are unavailable for this customer.";

    public static async Task<bool> IsBlockedAsync(
        IPersonalOrganizationConnectionBlockRepository blocks,
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken)
    {
        var active = await blocks
            .FindActiveByPersonalAndOrganizationAsync(personalUserIdentityId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        return active is not null;
    }

    public static ApplicationResult<T> UnavailableFailure<T>() =>
        ApplicationResult<T>.Failure(
            ApplicationErrorCodes.CustomerConnectionUnavailable,
            OrgUnavailableMessage);

    public static async Task<PersonalOrganizationConnectionBlock> ActivateOrCreateAsync(
        IPersonalOrganizationConnectionBlockRepository blocks,
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        DateTimeOffset utcNow,
        CustomerLinkRequestId? sourceRequestId,
        CancellationToken cancellationToken)
    {
        var existing = await blocks
            .FindByPersonalAndOrganizationAsync(personalUserIdentityId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is null)
        {
            var created = PersonalOrganizationConnectionBlock.Create(
                personalUserIdentityId,
                organizationId,
                utcNow,
                sourceRequestId);
            await blocks.AddAsync(created, cancellationToken).ConfigureAwait(false);
            return created;
        }

        existing.Activate(utcNow, sourceRequestId);
        await blocks.UpdateAsync(existing, cancellationToken).ConfigureAwait(false);
        return existing;
    }
}

public sealed class RemindCustomerLinkRequest
{
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IPersonalOrganizationConnectionBlockRepository _blocks;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPersonalAccountSettingsRepository _personalSettings;
    private readonly IPersonalInAppNotificationRepository _personalNotifications;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public RemindCustomerLinkRequest(
        ICustomerLinkRequestRepository requests,
        IPersonalOrganizationConnectionBlockRepository blocks,
        IPlatformOrganizationRepository organizations,
        IPersonalAccountSettingsRepository personalSettings,
        IPersonalInAppNotificationRepository personalNotifications,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _blocks = blocks;
        _organizations = organizations;
        _personalSettings = personalSettings;
        _personalNotifications = personalNotifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<CustomerLinkReminderDto>> ExecuteAsync(
        CustomerLinkRequestId requestId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (request is null || request.OrganizationId != organizationId)
        {
            return ApplicationResult<CustomerLinkReminderDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        try
        {
            if (request.IsExpired(_clock.UtcNow))
            {
                request.MarkExpired(_clock.UtcNow);
                await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                return ApplicationResult<CustomerLinkReminderDto>.Failure(
                    DomainErrorCodes.CustomerLinkRequestExpired,
                    "Customer link request has expired.");
            }

            if (request.Status != CustomerLinkRequestStatus.Pending)
            {
                return ApplicationResult<CustomerLinkReminderDto>.Failure(
                    ApplicationErrorCodes.CustomerLinkRequestNotPending,
                    "Only pending customer connection requests can be reminded.");
            }

            if (request.TargetUserIdentityId is PlatformUserId target
                && await CustomerConnectionBlockSupport
                    .IsBlockedAsync(_blocks, target, organizationId, cancellationToken)
                    .ConfigureAwait(false))
            {
                return CustomerConnectionBlockSupport.UnavailableFailure<CustomerLinkReminderDto>();
            }

            var count = request.RecordReminder(_clock.UtcNow);
            await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);

            if (request.TargetUserIdentityId is PlatformUserId recipient)
            {
                await CreateReminderNotificationAsync(request, recipient, count, cancellationToken)
                    .ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<CustomerLinkReminderDto>.Success(Map(request));
        }
        catch (DomainException ex)
        {
            var code = ex.ErrorCode == DomainErrorCodes.CustomerLinkReminderTooSoon
                ? ApplicationErrorCodes.CustomerLinkReminderTooSoon
                : ex.ErrorCode;
            return ApplicationResult<CustomerLinkReminderDto>.Failure(code, ex.Message);
        }
    }

    private async Task CreateReminderNotificationAsync(
        CustomerLinkRequest request,
        PlatformUserId targetUserId,
        int reminderSequence,
        CancellationToken cancellationToken)
    {
        var settings = await _personalSettings.GetByUserAsync(targetUserId, cancellationToken)
            .ConfigureAwait(false);
        var inAppEnabled = settings?.InAppNotificationsEnabled ?? true;
        if (!inAppEnabled)
        {
            return;
        }

        var merchantName = "a business";
        var org = await _organizations.GetByIdAsync(request.OrganizationId, cancellationToken)
            .ConfigureAwait(false);
        if (org is not null && !string.IsNullOrWhiteSpace(org.DisplayName))
        {
            merchantName = org.DisplayName;
        }

        var relatedId = $"{request.Id.Value:D}:{reminderSequence}";
        var existing = await _personalNotifications
            .FindByRecipientRelatedAsync(
                targetUserId,
                CustomerLinkNotificationTypes.PersonalCustomerLinkReminder,
                relatedId,
                cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var notification = PersonalInAppNotification.Create(
            targetUserId,
            title: "Customer connection reminder",
            preview: $"{merchantName} is waiting for you to review a customer connection request.",
            relatedType: CustomerLinkNotificationTypes.PersonalCustomerLinkReminder,
            utcNow: _clock.UtcNow,
            relatedId: relatedId);
        await _personalNotifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
    }

    internal static CustomerLinkReminderDto Map(CustomerLinkRequest request) =>
        new(
            request.Id.Value,
            request.OrganizationId.Value,
            request.BusinessCustomerId.Value,
            request.Status.ToString(),
            request.ReminderCount,
            request.LastRemindedAtUtc,
            request.NextReminderEligibleAtUtc);
}

public sealed class BlockBusinessFromCustomerLinkRequest
{
    private readonly ICustomerLinkRequestRepository _requests;
    private readonly IPersonalOrganizationConnectionBlockRepository _blocks;
    private readonly IPersonalInAppNotificationRepository _personalNotifications;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public BlockBusinessFromCustomerLinkRequest(
        ICustomerLinkRequestRepository requests,
        IPersonalOrganizationConnectionBlockRepository blocks,
        IPersonalInAppNotificationRepository personalNotifications,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _requests = requests;
        _blocks = blocks;
        _personalNotifications = personalNotifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalBlockedBusinessDto>> ExecuteAsync(
        CustomerLinkRequestId requestId,
        PlatformUserId personalUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var request = await _requests.GetByIdAsync(requestId, cancellationToken).ConfigureAwait(false);
        if (request is null
            || request.TargetUserIdentityId is null
            || request.TargetUserIdentityId != personalUserIdentityId)
        {
            return ApplicationResult<PersonalBlockedBusinessDto>.Failure(
                ApplicationErrorCodes.CustomerLinkRequestNotFound,
                "Customer link request was not found.");
        }

        try
        {
            if (request.Status == CustomerLinkRequestStatus.Pending)
            {
                if (request.IsExpired(_clock.UtcNow))
                {
                    request.MarkExpired(_clock.UtcNow);
                }
                else
                {
                    // End pending without creating a "decline" org notification about block.
                    request.Decline(_clock.UtcNow);
                }

                await _requests.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
                await MarkPendingNotificationsReadAsync(request, personalUserIdentityId, cancellationToken)
                    .ConfigureAwait(false);
            }

            await CustomerConnectionBlockSupport
                .ActivateOrCreateAsync(
                    _blocks,
                    personalUserIdentityId,
                    request.OrganizationId,
                    _clock.UtcNow,
                    request.Id,
                    cancellationToken)
                .ConfigureAwait(false);

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalBlockedBusinessDto>.Success(
                new PersonalBlockedBusinessDto(
                    request.OrganizationId.Value,
                    string.Empty,
                    _clock.UtcNow));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalBlockedBusinessDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task MarkPendingNotificationsReadAsync(
        CustomerLinkRequest request,
        PlatformUserId personalUserIdentityId,
        CancellationToken cancellationToken)
    {
        var relatedId = request.Id.Value.ToString("D");
        var pending = await _personalNotifications
            .FindByRecipientRelatedAsync(
                personalUserIdentityId,
                CustomerLinkNotificationTypes.PersonalPendingRequest,
                relatedId,
                cancellationToken)
            .ConfigureAwait(false);
        if (pending is not null && !pending.IsRead)
        {
            pending.MarkRead(_clock.UtcNow);
            await _personalNotifications.UpdateAsync(pending, cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class ListPersonalBlockedBusinesses
{
    private readonly IPersonalOrganizationConnectionBlockRepository _blocks;
    private readonly IPlatformOrganizationRepository _organizations;

    public ListPersonalBlockedBusinesses(
        IPersonalOrganizationConnectionBlockRepository blocks,
        IPlatformOrganizationRepository organizations)
    {
        _blocks = blocks;
        _organizations = organizations;
    }

    public async Task<IReadOnlyList<PersonalBlockedBusinessDto>> ExecuteAsync(
        PlatformUserId personalUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var blocks = await _blocks
            .ListActiveByPersonalUserAsync(personalUserIdentityId, cancellationToken)
            .ConfigureAwait(false);
        var results = new List<PersonalBlockedBusinessDto>(blocks.Count);
        foreach (var block in blocks)
        {
            var org = await _organizations.GetByIdAsync(block.OrganizationId, cancellationToken)
                .ConfigureAwait(false);
            var name = org?.DisplayName;
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Business";
            }

            results.Add(new PersonalBlockedBusinessDto(
                block.OrganizationId.Value,
                name,
                block.BlockedAtUtc));
        }

        return results;
    }
}

public sealed class UnblockPersonalOrganizationConnection
{
    private readonly IPersonalOrganizationConnectionBlockRepository _blocks;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public UnblockPersonalOrganizationConnection(
        IPersonalOrganizationConnectionBlockRepository blocks,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _blocks = blocks;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalBlockedBusinessDto>> ExecuteAsync(
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default)
    {
        var block = await _blocks
            .FindByPersonalAndOrganizationAsync(personalUserIdentityId, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (block is null)
        {
            // Idempotent: nothing to unblock.
            return ApplicationResult<PersonalBlockedBusinessDto>.Success(
                new PersonalBlockedBusinessDto(organizationId.Value, string.Empty, _clock.UtcNow));
        }

        block.Unblock(_clock.UtcNow);
        await _blocks.UpdateAsync(block, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PersonalBlockedBusinessDto>.Success(
            new PersonalBlockedBusinessDto(
                organizationId.Value,
                string.Empty,
                block.BlockedAtUtc));
    }
}

public sealed class DisconnectPersonalLinkedMerchant
{
    private readonly ILinkedCustomerAppUserRepository _links;
    private readonly IBusinessCustomerRepository _customers;
    private readonly IPersonalOrganizationConnectionBlockRepository _blocks;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DisconnectPersonalLinkedMerchant(
        ILinkedCustomerAppUserRepository links,
        IBusinessCustomerRepository customers,
        IPersonalOrganizationConnectionBlockRepository blocks,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _links = links;
        _customers = customers;
        _blocks = blocks;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public Task<ApplicationResult<PersonalLinkedMerchantDisconnectDto>> ExecuteAsync(
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreAsync(personalUserIdentityId, organizationId, activateBlock: false, cancellationToken);

    public Task<ApplicationResult<PersonalLinkedMerchantDisconnectDto>> ExecuteAndBlockAsync(
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        CancellationToken cancellationToken = default) =>
        ExecuteCoreAsync(personalUserIdentityId, organizationId, activateBlock: true, cancellationToken);

    private async Task<ApplicationResult<PersonalLinkedMerchantDisconnectDto>> ExecuteCoreAsync(
        PlatformUserId personalUserIdentityId,
        PlatformOrganizationId organizationId,
        bool activateBlock,
        CancellationToken cancellationToken)
    {
        try
        {
            var activeLinks = await _links
                .ListActiveByUserAndOrganizationAsync(personalUserIdentityId, organizationId, cancellationToken)
                .ConfigureAwait(false);

            var revoked = 0;
            foreach (var link in activeLinks)
            {
                if (link.Status == LinkedCustomerAppUserStatus.Revoked)
                {
                    continue;
                }

                link.Revoke(_clock.UtcNow);
                var customer = await _customers.GetByIdAsync(link.BusinessCustomerId, cancellationToken)
                    .ConfigureAwait(false);
                if (customer is not null
                    && customer.OrganizationId == organizationId
                    && customer.LinkedUserIdentityId == personalUserIdentityId)
                {
                    customer.UnlinkAppUser(_clock.UtcNow);
                    await _customers.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
                }

                await _links.UpdateAsync(link, cancellationToken).ConfigureAwait(false);
                revoked++;
            }

            if (activateBlock)
            {
                await CustomerConnectionBlockSupport
                    .ActivateOrCreateAsync(
                        _blocks,
                        personalUserIdentityId,
                        organizationId,
                        _clock.UtcNow,
                        sourceRequestId: null,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return ApplicationResult<PersonalLinkedMerchantDisconnectDto>.Success(
                new PersonalLinkedMerchantDisconnectDto(
                    organizationId.Value,
                    revoked,
                    activateBlock));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalLinkedMerchantDisconnectDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
