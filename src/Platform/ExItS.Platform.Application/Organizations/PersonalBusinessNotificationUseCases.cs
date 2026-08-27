using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Organizations;

public sealed record PublishPersonalBusinessNotificationRequest(
    Guid RecipientPlatformUserId,
    string RelatedType,
    string RelatedId,
    string Title,
    string Preview);

public sealed record PublishPersonalBusinessNotificationResult(
    Guid RecipientPlatformUserId,
    string RelatedType,
    string RelatedId,
    bool Created,
    bool SkippedExisting);

/// <summary>
/// Product publish into a Personal in-app notification inbox (customer-order lifecycle).
/// Authenticated Organization members may publish only allowlisted CustomerOrder* types.
/// Dedupes by (recipient, relatedType, relatedId).
/// </summary>
public sealed class PublishPersonalBusinessNotification
{
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishPersonalBusinessNotification(
        IPersonalInAppNotificationRepository notifications,
        IPersonalAccountSettingsRepository settings,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _notifications = notifications;
        _settings = settings;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PublishPersonalBusinessNotificationResult>> ExecuteAsync(
        PlatformOrganizationId sourceOrganizationId,
        PublishPersonalBusinessNotificationRequest request,
        CancellationToken cancellationToken = default)
    {
        _ = sourceOrganizationId;

        if (!CustomerOrderNotificationTypes.IsKnown(request.RelatedType))
        {
            return ApplicationResult<PublishPersonalBusinessNotificationResult>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "This notification type cannot be published to Personal inboxes.");
        }

        if (string.IsNullOrWhiteSpace(request.RelatedId)
            || string.IsNullOrWhiteSpace(request.Title)
            || string.IsNullOrWhiteSpace(request.Preview))
        {
            return ApplicationResult<PublishPersonalBusinessNotificationResult>.Failure(
                ApplicationErrorCodes.DomainViolation,
                "Notification title, preview, and related id are required.");
        }

        PlatformUserId recipientId;
        try
        {
            recipientId = PlatformUserId.From(request.RecipientPlatformUserId);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PublishPersonalBusinessNotificationResult>.Failure(ex.ErrorCode, ex.Message);
        }

        var settings = await _settings.GetByUserAsync(recipientId, cancellationToken).ConfigureAwait(false);
        var inAppEnabled = settings?.InAppNotificationsEnabled ?? true;
        if (!inAppEnabled)
        {
            return ApplicationResult<PublishPersonalBusinessNotificationResult>.Success(
                new PublishPersonalBusinessNotificationResult(
                    recipientId.Value,
                    request.RelatedType.Trim(),
                    request.RelatedId.Trim(),
                    Created: false,
                    SkippedExisting: false));
        }

        var relatedType = request.RelatedType.Trim();
        var relatedId = request.RelatedId.Trim();
        var existing = await _notifications
            .FindByRecipientRelatedAsync(recipientId, relatedType, relatedId, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return ApplicationResult<PublishPersonalBusinessNotificationResult>.Success(
                new PublishPersonalBusinessNotificationResult(
                    recipientId.Value,
                    relatedType,
                    relatedId,
                    Created: false,
                    SkippedExisting: true));
        }

        var notification = PersonalInAppNotification.Create(
            recipientId,
            request.Title,
            request.Preview,
            relatedType,
            _clock.UtcNow,
            relatedId);
        await _notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PublishPersonalBusinessNotificationResult>.Success(
            new PublishPersonalBusinessNotificationResult(
                recipientId.Value,
                relatedType,
                relatedId,
                Created: true,
                SkippedExisting: false));
    }
}
