using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.Application.Personal;

public sealed record PersonalReminderDto(
    Guid Id,
    Guid DebtRelationshipId,
    Guid CreatedByUserIdentityId,
    string ScheduleType,
    string? Message,
    DateTimeOffset ScheduledForUtc,
    DateTimeOffset? NextDeliveryAtUtc,
    string Status,
    int DeliveryAttemptCount,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? DeliveredAtUtc);

public sealed record CreatePersonalReminderRequest(
    string ScheduleType,
    DateTimeOffset ScheduledForUtc,
    string? Message);

public sealed record PersonalInAppNotificationDto(
    Guid Id,
    string Title,
    string Preview,
    string RelatedType,
    string? RelatedId,
    bool IsRead,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record PersonalNotificationDeliveryDto(
    Guid Id,
    Guid? ReminderId,
    Guid? NotificationId,
    Guid RecipientUserIdentityId,
    string Channel,
    string Status,
    string PreviewText,
    DateTimeOffset AttemptedAtUtc,
    DateTimeOffset? DeliveredAtUtc,
    string? FailureReason);

public sealed class CreatePersonalReminder
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalReminderRepository _reminders;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CreatePersonalReminder(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalContactRepository contacts,
        IPersonalReminderRepository reminders,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _relationships = relationships;
        _contacts = contacts;
        _reminders = reminders;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalReminderDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid relationshipId,
        CreatePersonalReminderRequest request,
        CancellationToken cancellationToken = default)
    {
        var relationship = await _relationships.GetByIdAsync(
            PersonalDebtRelationshipId.From(relationshipId),
            cancellationToken).ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        if (!await PersonalUtangAccess.CanViewAsync(relationship, actingUserIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<PersonalReminderDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Not authorized for this personal debt relationship.");
        }

        if (!Enum.TryParse<PersonalReminderScheduleType>(request.ScheduleType, ignoreCase: true, out var scheduleType))
        {
            return ApplicationResult<PersonalReminderDto>.Failure(
                DomainErrorCodes.InvalidPersonalReminder,
                "Reminder schedule type is invalid.");
        }

        try
        {
            var reminder = PersonalReminder.Create(
                relationship.Id,
                actingUserIdentityId,
                scheduleType,
                request.ScheduledForUtc,
                _clock.UtcNow,
                request.Message);
            await _reminders.AddAsync(reminder, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalReminderCreated,
                nameof(PersonalReminder),
                reminder.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang reminder scheduled.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalReminderDto>.Success(ToDto(reminder));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    internal static PersonalReminderDto ToDto(PersonalReminder reminder) =>
        new(
            reminder.Id.Value,
            reminder.DebtRelationshipId.Value,
            reminder.CreatedByUserIdentityId.Value,
            reminder.ScheduleType.ToString(),
            reminder.Message,
            reminder.ScheduledForUtc,
            reminder.NextDeliveryAtUtc,
            reminder.Status.ToString(),
            reminder.DeliveryAttemptCount,
            reminder.CreatedAtUtc,
            reminder.DeliveredAtUtc);
}

public sealed class ListPersonalReminders
{
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalReminderRepository _reminders;

    public ListPersonalReminders(
        IPersonalDebtRelationshipRepository relationships,
        IPersonalContactRepository contacts,
        IPersonalReminderRepository reminders)
    {
        _relationships = relationships;
        _contacts = contacts;
        _reminders = reminders;
    }

    public async Task<ApplicationResult<IReadOnlyList<PersonalReminderDto>>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid relationshipId,
        CancellationToken cancellationToken = default)
    {
        var relationship = await _relationships.GetByIdAsync(
            PersonalDebtRelationshipId.From(relationshipId),
            cancellationToken).ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<IReadOnlyList<PersonalReminderDto>>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        if (!await PersonalUtangAccess.CanViewAsync(relationship, actingUserIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<IReadOnlyList<PersonalReminderDto>>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Not authorized for this personal debt relationship.");
        }

        var list = await _reminders.ListByRelationshipAsync(relationship.Id, cancellationToken)
            .ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<PersonalReminderDto>>.Success(
            list.Select(CreatePersonalReminder.ToDto).ToList());
    }
}

public sealed class ListDuePersonalReminders
{
    private readonly IPersonalReminderRepository _reminders;
    private readonly IClock _clock;

    public ListDuePersonalReminders(IPersonalReminderRepository reminders, IClock clock)
    {
        _reminders = reminders;
        _clock = clock;
    }

    public async Task<IReadOnlyList<PersonalReminderDto>> ExecuteAsync(
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var list = await _reminders.ListDueAsync(_clock.UtcNow, Math.Clamp(take, 1, 200), cancellationToken)
            .ConfigureAwait(false);
        return list.Select(CreatePersonalReminder.ToDto).ToList();
    }
}

public sealed class DeliverPersonalReminder
{
    private readonly IPersonalReminderRepository _reminders;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalAccountSettingsRepository _settings;
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IPersonalNotificationDeliveryRepository _deliveries;
    private readonly IPersonalPushNotificationSink _pushSink;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public DeliverPersonalReminder(
        IPersonalReminderRepository reminders,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalContactRepository contacts,
        IPersonalAccountSettingsRepository settings,
        IPersonalInAppNotificationRepository notifications,
        IPersonalNotificationDeliveryRepository deliveries,
        IPersonalPushNotificationSink pushSink,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _reminders = reminders;
        _relationships = relationships;
        _contacts = contacts;
        _settings = settings;
        _notifications = notifications;
        _deliveries = deliveries;
        _pushSink = pushSink;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalReminderDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid reminderId,
        CancellationToken cancellationToken = default) =>
        await ExecuteCoreAsync(actingUserIdentityId, reminderId, requireCreator: true, cancellationToken)
            .ConfigureAwait(false);

    /// <summary>
    /// System/worker delivery path: uses the reminder creator as the acting principal for
    /// recipient resolution, without requiring an end-user JWT match.
    /// </summary>
    public async Task<ApplicationResult<PersonalReminderDto>> ExecuteSystemAsync(
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        var reminder = await _reminders.GetByIdAsync(PersonalReminderId.From(reminderId), cancellationToken)
            .ConfigureAwait(false);
        if (reminder is null)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(
                ApplicationErrorCodes.PersonalReminderNotFound,
                "Reminder was not found.");
        }

        return await ExecuteCoreAsync(
                reminder.CreatedByUserIdentityId,
                reminderId,
                requireCreator: false,
                cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<ApplicationResult<PersonalReminderDto>> ExecuteCoreAsync(
        PlatformUserId actingUserIdentityId,
        Guid reminderId,
        bool requireCreator,
        CancellationToken cancellationToken)
    {
        var reminder = await _reminders.GetByIdAsync(PersonalReminderId.From(reminderId), cancellationToken)
            .ConfigureAwait(false);
        if (reminder is null)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(
                ApplicationErrorCodes.PersonalReminderNotFound,
                "Reminder was not found.");
        }

        if (requireCreator && reminder.CreatedByUserIdentityId != actingUserIdentityId)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Not authorized to deliver this reminder.");
        }

        var relationship = await _relationships.GetByIdAsync(reminder.DebtRelationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(
                ApplicationErrorCodes.PersonalUtangRelationshipNotFound,
                "Personal debt relationship was not found.");
        }

        try
        {
            var since = _clock.UtcNow.AddHours(-24);
            var deliveriesLastDay = await _reminders.CountDeliveriesSinceAsync(
                relationship.Id,
                since,
                cancellationToken).ConfigureAwait(false);
            var lastDelivery = await _reminders.GetLastDeliveryAtAsync(relationship.Id, cancellationToken)
                .ConfigureAwait(false);
            PersonalReminder.EnsureDeliveryAllowed(deliveriesLastDay, lastDelivery, _clock.UtcNow);

            var recipients = await ResolveRecipientsAsync(
                relationship,
                actingUserIdentityId,
                cancellationToken).ConfigureAwait(false);
            var preview = PersonalReminder.BuildMinimizedPreview(reminder.Message);
            const string title = "Personal Utang reminder";

            foreach (var recipient in recipients)
            {
                var prefs = await _settings.GetByUserAsync(recipient, cancellationToken).ConfigureAwait(false)
                    ?? PersonalAccountSettings.CreateDefaults(recipient, _clock.UtcNow);

                if (prefs.ReminderNotificationsEnabled && prefs.InAppNotificationsEnabled)
                {
                    var notification = PersonalInAppNotification.Create(
                        recipient,
                        title,
                        preview,
                        relatedType: "PersonalDebtRelationship",
                        _clock.UtcNow,
                        relatedId: relationship.Id.Value.ToString("D"));
                    await _notifications.AddAsync(notification, cancellationToken).ConfigureAwait(false);

                    var inAppDelivery = PersonalNotificationDelivery.Create(
                        recipient,
                        PersonalNotificationChannel.InApp,
                        preview,
                        _clock.UtcNow,
                        reminderId: reminder.Id,
                        notificationId: notification.Id);
                    inAppDelivery.MarkDelivered(_clock.UtcNow);
                    await _deliveries.AddAsync(inAppDelivery, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    var skipped = PersonalNotificationDelivery.Create(
                        recipient,
                        PersonalNotificationChannel.InApp,
                        preview,
                        _clock.UtcNow,
                        reminderId: reminder.Id);
                    skipped.MarkSkipped("In-app or reminder notifications disabled by recipient preferences.");
                    await _deliveries.AddAsync(skipped, cancellationToken).ConfigureAwait(false);
                }

                var pushDelivery = PersonalNotificationDelivery.Create(
                    recipient,
                    PersonalNotificationChannel.Push,
                    preview,
                    _clock.UtcNow,
                    reminderId: reminder.Id);

                if (prefs.ReminderNotificationsEnabled && prefs.PushNotificationsEnabled)
                {
                    var pushed = await _pushSink.TryDeliverAsync(recipient, title, preview, cancellationToken)
                        .ConfigureAwait(false);
                    if (pushed)
                    {
                        pushDelivery.MarkDelivered(_clock.UtcNow);
                    }
                    else
                    {
                        pushDelivery.MarkSkipped("Push sink unavailable or no vendor configured.");
                    }
                }
                else
                {
                    pushDelivery.MarkSkipped("Push or reminder notifications disabled by recipient preferences.");
                }

                await _deliveries.AddAsync(pushDelivery, cancellationToken).ConfigureAwait(false);
            }

            reminder.MarkDelivered(_clock.UtcNow);
            await _reminders.UpdateAsync(reminder, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            var actor = requireCreator
                ? $"platform-user:{actingUserIdentityId.Value:D}"
                : "system:personal-reminder-worker";
            await _auditWriter.WriteAsync(
                actor,
                requireCreator ? AuditActorType.PlatformUser : AuditActorType.System,
                PlatformAuditActions.PersonalReminderDelivered,
                nameof(PersonalReminder),
                reminder.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang reminder delivered with minimized preview.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalReminderDto>.Success(CreatePersonalReminder.ToDto(reminder));
        }
        catch (DomainException ex) when (ex.ErrorCode == DomainErrorCodes.PersonalReminderRateLimited)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(
                ApplicationErrorCodes.PersonalReminderRateLimited,
                ex.Message);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    private async Task<IReadOnlyList<PlatformUserId>> ResolveRecipientsAsync(
        PersonalDebtRelationship relationship,
        PlatformUserId actingUserIdentityId,
        CancellationToken cancellationToken)
    {
        var recipients = new HashSet<Guid>();
        if (relationship.CreditorUserIdentityId is not null
            && relationship.CreditorUserIdentityId != actingUserIdentityId)
        {
            recipients.Add(relationship.CreditorUserIdentityId.Value);
        }

        if (relationship.DebtorUserIdentityId is not null
            && relationship.DebtorUserIdentityId != actingUserIdentityId)
        {
            recipients.Add(relationship.DebtorUserIdentityId.Value);
        }

        // Unlinked contacts: notify the acting user only that delivery targeted the relationship owner side.
        if (recipients.Count == 0)
        {
            recipients.Add(actingUserIdentityId.Value);
        }

        _ = _contacts;
        _ = cancellationToken;
        return recipients.Select(PlatformUserId.From).ToList();
    }
}

public sealed class CancelPersonalReminder
{
    private readonly IPersonalReminderRepository _reminders;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public CancelPersonalReminder(
        IPersonalReminderRepository reminders,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _reminders = reminders;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalReminderDto>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid reminderId,
        CancellationToken cancellationToken = default)
    {
        var reminder = await _reminders.GetByIdAsync(PersonalReminderId.From(reminderId), cancellationToken)
            .ConfigureAwait(false);
        if (reminder is null || reminder.CreatedByUserIdentityId != actingUserIdentityId)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(
                ApplicationErrorCodes.PersonalReminderNotFound,
                "Reminder was not found.");
        }

        try
        {
            reminder.Cancel(_clock.UtcNow);
            await _reminders.UpdateAsync(reminder, cancellationToken).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            await _auditWriter.WriteAsync(
                $"platform-user:{actingUserIdentityId.Value:D}",
                AuditActorType.PlatformUser,
                PlatformAuditActions.PersonalReminderCancelled,
                nameof(PersonalReminder),
                reminder.Id.Value.ToString("D"),
                AuditOutcome.Succeeded,
                summary: "Personal Utang reminder cancelled.",
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return ApplicationResult<PersonalReminderDto>.Success(CreatePersonalReminder.ToDto(reminder));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<PersonalReminderDto>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}

public sealed class ListPersonalInAppNotifications
{
    private readonly IPersonalInAppNotificationRepository _notifications;

    public ListPersonalInAppNotifications(IPersonalInAppNotificationRepository notifications) =>
        _notifications = notifications;

    public async Task<IReadOnlyList<PersonalInAppNotificationDto>> ExecuteAsync(
        PlatformUserId recipientUserIdentityId,
        CancellationToken cancellationToken = default)
    {
        var list = await _notifications.ListForUserAsync(recipientUserIdentityId, take: 50, cancellationToken)
            .ConfigureAwait(false);
        return list.Select(ToDto).ToList();
    }

    internal static PersonalInAppNotificationDto ToDto(PersonalInAppNotification notification) =>
        new(
            notification.Id.Value,
            notification.Title,
            notification.Preview,
            notification.RelatedType,
            notification.RelatedId,
            notification.IsRead,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
}

public sealed class MarkPersonalInAppNotificationRead
{
    private readonly IPersonalInAppNotificationRepository _notifications;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public MarkPersonalInAppNotificationRead(
        IPersonalInAppNotificationRepository notifications,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _notifications = notifications;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<PersonalInAppNotificationDto>> ExecuteAsync(
        PlatformUserId recipientUserIdentityId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        var notification = await _notifications.GetByIdAsync(
            PersonalInAppNotificationId.From(notificationId),
            cancellationToken).ConfigureAwait(false);
        if (notification is null || notification.RecipientUserIdentityId != recipientUserIdentityId)
        {
            return ApplicationResult<PersonalInAppNotificationDto>.Failure(
                ApplicationErrorCodes.PersonalNotificationNotFound,
                "Notification was not found.");
        }

        notification.MarkRead(_clock.UtcNow);
        await _notifications.UpdateAsync(notification, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ApplicationResult<PersonalInAppNotificationDto>.Success(
            ListPersonalInAppNotifications.ToDto(notification));
    }
}

public sealed class ListPersonalNotificationDeliveries
{
    private readonly IPersonalNotificationDeliveryRepository _deliveries;
    private readonly IPersonalReminderRepository _reminders;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly IPersonalContactRepository _contacts;

    public ListPersonalNotificationDeliveries(
        IPersonalNotificationDeliveryRepository deliveries,
        IPersonalReminderRepository reminders,
        IPersonalDebtRelationshipRepository relationships,
        IPersonalContactRepository contacts)
    {
        _deliveries = deliveries;
        _reminders = reminders;
        _relationships = relationships;
        _contacts = contacts;
    }

    public async Task<ApplicationResult<IReadOnlyList<PersonalNotificationDeliveryDto>>> ExecuteAsync(
        PlatformUserId actingUserIdentityId,
        Guid? reminderId,
        CancellationToken cancellationToken = default)
    {
        if (reminderId is null)
        {
            var forUser = await _deliveries.ListForRecipientAsync(actingUserIdentityId, take: 50, cancellationToken)
                .ConfigureAwait(false);
            return ApplicationResult<IReadOnlyList<PersonalNotificationDeliveryDto>>.Success(
                forUser.Select(ToDto).ToList());
        }

        var reminder = await _reminders.GetByIdAsync(PersonalReminderId.From(reminderId.Value), cancellationToken)
            .ConfigureAwait(false);
        if (reminder is null)
        {
            return ApplicationResult<IReadOnlyList<PersonalNotificationDeliveryDto>>.Failure(
                ApplicationErrorCodes.PersonalReminderNotFound,
                "Reminder was not found.");
        }

        var relationship = await _relationships.GetByIdAsync(reminder.DebtRelationshipId, cancellationToken)
            .ConfigureAwait(false);
        if (relationship is null
            || !await PersonalUtangAccess.CanViewAsync(relationship, actingUserIdentityId, _contacts, cancellationToken)
                .ConfigureAwait(false))
        {
            return ApplicationResult<IReadOnlyList<PersonalNotificationDeliveryDto>>.Failure(
                ApplicationErrorCodes.PersonalUtangUnauthorized,
                "Not authorized for this delivery audit.");
        }

        var list = await _deliveries.ListByReminderAsync(reminder.Id, cancellationToken).ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<PersonalNotificationDeliveryDto>>.Success(list.Select(ToDto).ToList());
    }

    private static PersonalNotificationDeliveryDto ToDto(PersonalNotificationDelivery delivery) =>
        new(
            delivery.Id.Value,
            delivery.ReminderId?.Value,
            delivery.NotificationId?.Value,
            delivery.RecipientUserIdentityId.Value,
            delivery.Channel.ToString(),
            delivery.Status.ToString(),
            delivery.PreviewText,
            delivery.AttemptedAtUtc,
            delivery.DeliveredAtUtc,
            delivery.FailureReason);
}

/// <summary>
/// One worker tick: deliver due Utang relationship reminders and due Personal To-do reminders.
/// </summary>
public sealed class ProcessDuePersonalReminders
{
    private readonly IPersonalReminderRepository _reminders;
    private readonly IPersonalTodoRepository _todos;
    private readonly DeliverPersonalReminder _deliverUtang;
    private readonly DeliverPersonalTodoReminder _deliverTodo;
    private readonly IClock _clock;

    public ProcessDuePersonalReminders(
        IPersonalReminderRepository reminders,
        IPersonalTodoRepository todos,
        DeliverPersonalReminder deliverUtang,
        DeliverPersonalTodoReminder deliverTodo,
        IClock clock)
    {
        _reminders = reminders;
        _todos = todos;
        _deliverUtang = deliverUtang;
        _deliverTodo = deliverTodo;
        _clock = clock;
    }

    public async Task<int> ExecuteOnceAsync(int take = 50, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(take, 1, 200);
        var delivered = 0;

        var dueUtang = await _reminders.ListDueAsync(_clock.UtcNow, limit, cancellationToken)
            .ConfigureAwait(false);
        foreach (var reminder in dueUtang)
        {
            var result = await _deliverUtang.ExecuteSystemAsync(reminder.Id.Value, cancellationToken)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                delivered++;
            }
        }

        var dueTodos = await _todos.ListDueRemindersAsync(_clock.UtcNow, limit, cancellationToken)
            .ConfigureAwait(false);
        foreach (var todo in dueTodos)
        {
            var result = await _deliverTodo.ExecuteAsync(todo.Id.Value, cancellationToken)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                delivered++;
            }
        }

        return delivered;
    }
}
