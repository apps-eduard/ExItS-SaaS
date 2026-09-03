using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Organizations;

public sealed record PublishOrganizationBusinessNotificationRequest(
    Guid RecipientOrganizationId,
    string RelatedType,
    string RelatedId,
    string Title,
    string Preview,
    /// <summary>Operational branch the notification is addressed to. Null publishes organization-wide.</summary>
    Guid? TargetBranchId = null);

public sealed record PublishOrganizationBusinessNotificationResult(
    Guid RecipientOrganizationId,
    string RelatedType,
    string RelatedId,
    int CreatedCount,
    int SkippedExistingCount,
    Guid? TargetBranchId = null);

public sealed record MarkRelatedOrganizationNotificationsReadRequest(
    string RelatedType,
    string RelatedId);

public sealed record MarkRelatedOrganizationNotificationsReadResult(
    string RelatedType,
    string RelatedId,
    int MarkedCount);

/// <summary>
/// Cross-org product publish into the shared Organization in-app notification inbox.
/// Recipients are active Owner and Administrator memberships of the recipient organization.
/// </summary>
public sealed class PublishOrganizationBusinessNotification
{
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IOrganizationInAppNotificationRepository _notifications;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishOrganizationBusinessNotification(
        IOrganizationMembershipRepository memberships,
        IOrganizationInAppNotificationRepository notifications,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _memberships = memberships;
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PublishOrganizationBusinessNotificationResult>> ExecuteAsync(
        PlatformOrganizationId sourceOrganizationId,
        PublishOrganizationBusinessNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!OrganizationBusinessNotificationTypes.IsPublishable(request.RelatedType))
        {
            return ApplicationResult<PublishOrganizationBusinessNotificationResult>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "This notification type cannot be published.");
        }

        if (string.IsNullOrWhiteSpace(request.RelatedId)
            || string.IsNullOrWhiteSpace(request.Title)
            || string.IsNullOrWhiteSpace(request.Preview))
        {
            return ApplicationResult<PublishOrganizationBusinessNotificationResult>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Notification title, preview, and related id are required.");
        }

        PlatformOrganizationId recipientOrganizationId;
        try
        {
            recipientOrganizationId = PlatformOrganizationId.From(request.RecipientOrganizationId);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PublishOrganizationBusinessNotificationResult>.Failure(ex.ErrorCode, ex.Message);
        }

        if (recipientOrganizationId == sourceOrganizationId
            && !OrganizationBusinessNotificationTypes.AllowsSameOrganization(request.RelatedType))
        {
            return ApplicationResult<PublishOrganizationBusinessNotificationResult>.Failure(
                ApplicationErrorCodes.CrossOrganizationMismatch,
                "Notifications cannot be published to the same organization.");
        }

        var relatedType = request.RelatedType.Trim();
        var relatedId = request.RelatedId.Trim();

        // Only types declared branch-targetable may be narrowed; everything else stays organization-wide
        // even when a caller supplies a branch, so unrelated inbox items never disappear from a workspace.
        var targetBranchId = OrganizationBusinessNotificationTypes.IsBranchTargetable(relatedType)
            && request.TargetBranchId is { } branch
            && branch != Guid.Empty
                ? request.TargetBranchId
                : null;

        var recipients = await _memberships
            .ListActiveBusinessInboxRecipientsAsync(recipientOrganizationId, cancellationToken)
            .ConfigureAwait(false);

        var created = 0;
        var skipped = 0;
        var utcNow = _clock.UtcNow;
        foreach (var membership in recipients)
        {
            var existing = await _notifications
                .FindByRecipientRelatedAsync(membership.UserId, relatedType, relatedId, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                skipped++;
                continue;
            }

            var notification = OrganizationInAppNotification.Create(
                recipientOrganizationId,
                membership.UserId,
                request.Title,
                request.Preview,
                relatedType,
                utcNow,
                relatedId,
                id: null,
                targetBranchId);
            await _notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
            created++;
        }

        if (created > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult<PublishOrganizationBusinessNotificationResult>.Success(
            new PublishOrganizationBusinessNotificationResult(
                recipientOrganizationId.Value,
                relatedType,
                relatedId,
                created,
                skipped,
                targetBranchId));
    }
}

/// <summary>Marks all organization notifications for a related type/id as read (action resolved).</summary>
public sealed class MarkRelatedOrganizationNotificationsRead
{
    private readonly IOrganizationInAppNotificationRepository _notifications;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public MarkRelatedOrganizationNotificationsRead(
        IOrganizationInAppNotificationRepository notifications,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<MarkRelatedOrganizationNotificationsReadResult>> ExecuteAsync(
        PlatformOrganizationId organizationId,
        MarkRelatedOrganizationNotificationsReadRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RelatedType) || string.IsNullOrWhiteSpace(request.RelatedId))
        {
            return ApplicationResult<MarkRelatedOrganizationNotificationsReadResult>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Related type and id are required.");
        }

        var relatedType = request.RelatedType.Trim();
        var relatedId = request.RelatedId.Trim();
        var items = await _notifications
            .ListByOrganizationRelatedAsync(organizationId, relatedType, relatedId, cancellationToken)
            .ConfigureAwait(false);

        var marked = 0;
        var utcNow = _clock.UtcNow;
        foreach (var notification in items)
        {
            if (notification.IsRead)
            {
                continue;
            }

            notification.MarkRead(utcNow);
            await _notifications.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
            marked++;
        }

        if (marked > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ApplicationResult<MarkRelatedOrganizationNotificationsReadResult>.Success(
            new MarkRelatedOrganizationNotificationsReadResult(relatedType, relatedId, marked));
    }
}
