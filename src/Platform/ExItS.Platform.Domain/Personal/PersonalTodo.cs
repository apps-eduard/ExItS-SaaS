using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;

namespace ExItS.Platform.Domain.Personal;

public enum PersonalTodoStatus
{
    Open,
    Completed,
    Cancelled
}

public enum PersonalTodoPriority
{
    None,
    Low,
    Normal,
    High
}

/// <summary>
/// Optional related-entity metadata. Stored as string; does not grant authorization.
/// </summary>
public enum PersonalTodoRelatedEntityType
{
    None,
    PersonalUtangRelationship,
    PersonalContact,
    CustomerOrder,
    Organization
}

/// <summary>
/// Personal To-do item owned exclusively by a Personal user identity.
/// Related-entity linkage is metadata only and never opens cross-owner access.
/// </summary>
public sealed class PersonalTodo
{
    public const int MaxTitleLength = 200;
    public const int MaxNotesLength = 2000;

    public PersonalTodoId Id { get; }
    public PlatformUserId OwnerUserIdentityId { get; }
    public string Title { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? DueAtUtc { get; private set; }
    public DateTimeOffset? ReminderAtUtc { get; private set; }
    /// <summary>
    /// When set, the current <see cref="ReminderAtUtc"/> was already delivered as an in-app reminder.
    /// Cleared when the reminder timestamp changes so a reschedule can fire again.
    /// </summary>
    public DateTimeOffset? ReminderNotifiedAtUtc { get; private set; }
    public PersonalTodoPriority Priority { get; private set; }
    public PersonalTodoStatus Status { get; private set; }
    public PersonalTodoRelatedEntityType RelatedEntityType { get; private set; }
    public Guid? RelatedEntityId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public int Version { get; private set; }

    private PersonalTodo(
        PersonalTodoId id,
        PlatformUserId ownerUserIdentityId,
        string title,
        string? notes,
        DateTimeOffset? dueAtUtc,
        DateTimeOffset? reminderAtUtc,
        PersonalTodoPriority priority,
        PersonalTodoStatus status,
        PersonalTodoRelatedEntityType relatedEntityType,
        Guid? relatedEntityId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? completedAtUtc,
        int version,
        DateTimeOffset? reminderNotifiedAtUtc = null)
    {
        Id = id;
        OwnerUserIdentityId = ownerUserIdentityId;
        Title = title;
        Notes = notes;
        DueAtUtc = dueAtUtc;
        ReminderAtUtc = reminderAtUtc;
        ReminderNotifiedAtUtc = reminderNotifiedAtUtc;
        Priority = priority;
        Status = status;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityId;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        CompletedAtUtc = completedAtUtc;
        Version = version;
    }

    public static PersonalTodo Create(
        PlatformUserId ownerUserIdentityId,
        string title,
        DateTimeOffset utcNow,
        string? notes = null,
        DateTimeOffset? dueAtUtc = null,
        DateTimeOffset? reminderAtUtc = null,
        PersonalTodoPriority priority = PersonalTodoPriority.None,
        PersonalTodoRelatedEntityType relatedEntityType = PersonalTodoRelatedEntityType.None,
        Guid? relatedEntityId = null,
        PersonalTodoId? id = null)
    {
        ArgumentNullException.ThrowIfNull(ownerUserIdentityId);
        EnsureUtc(utcNow);
        EnsureOptionalUtc(dueAtUtc);
        EnsureOptionalUtc(reminderAtUtc);
        EnsurePriority(priority);
        EnsureRelated(relatedEntityType, relatedEntityId);

        return new PersonalTodo(
            id ?? PersonalTodoId.New(),
            ownerUserIdentityId,
            NormalizeTitle(title),
            NormalizeNotes(notes),
            dueAtUtc,
            reminderAtUtc,
            priority,
            PersonalTodoStatus.Open,
            relatedEntityType,
            relatedEntityType is PersonalTodoRelatedEntityType.None ? null : relatedEntityId,
            utcNow,
            utcNow,
            completedAtUtc: null,
            version: 1,
            reminderNotifiedAtUtc: null);
    }

    public static PersonalTodo Rehydrate(
        PersonalTodoId id,
        PlatformUserId ownerUserIdentityId,
        string title,
        string? notes,
        DateTimeOffset? dueAtUtc,
        DateTimeOffset? reminderAtUtc,
        PersonalTodoPriority priority,
        PersonalTodoStatus status,
        PersonalTodoRelatedEntityType relatedEntityType,
        Guid? relatedEntityId,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        DateTimeOffset? completedAtUtc,
        int version,
        DateTimeOffset? reminderNotifiedAtUtc = null) =>
        new(
            id,
            ownerUserIdentityId,
            title,
            notes,
            dueAtUtc,
            reminderAtUtc,
            priority,
            status,
            relatedEntityType,
            relatedEntityId,
            createdAtUtc,
            updatedAtUtc,
            completedAtUtc,
            version,
            reminderNotifiedAtUtc);

    public bool IsOwnedBy(PlatformUserId userIdentityId) =>
        OwnerUserIdentityId == userIdentityId;

    public void EnsureOwnedBy(PlatformUserId userIdentityId)
    {
        if (!IsOwnedBy(userIdentityId))
        {
            throw new DomainException(
                DomainErrorCodes.PersonalTodoUnauthorized,
                "Personal to-do is not owned by this account.");
        }
    }

    public void Update(
        string title,
        string? notes,
        DateTimeOffset? dueAtUtc,
        DateTimeOffset? reminderAtUtc,
        PersonalTodoPriority priority,
        PersonalTodoRelatedEntityType relatedEntityType,
        Guid? relatedEntityId,
        DateTimeOffset utcNow,
        int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);
        EnsureMutable();
        EnsureOptionalUtc(dueAtUtc);
        EnsureOptionalUtc(reminderAtUtc);
        EnsurePriority(priority);
        EnsureRelated(relatedEntityType, relatedEntityId);

        Title = NormalizeTitle(title);
        Notes = NormalizeNotes(notes);
        DueAtUtc = dueAtUtc;
        if (ReminderAtUtc != reminderAtUtc)
        {
            ReminderNotifiedAtUtc = null;
        }

        ReminderAtUtc = reminderAtUtc;
        Priority = priority;
        RelatedEntityType = relatedEntityType;
        RelatedEntityId = relatedEntityType is PersonalTodoRelatedEntityType.None ? null : relatedEntityId;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    public void Complete(DateTimeOffset utcNow, int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);

        if (Status is PersonalTodoStatus.Completed)
        {
            return;
        }

        if (Status is not PersonalTodoStatus.Open)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalTodoStatusTransition,
                $"Cannot complete a to-do in status {Status}.");
        }

        Status = PersonalTodoStatus.Completed;
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    public void Reopen(DateTimeOffset utcNow, int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);

        if (Status is not PersonalTodoStatus.Completed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalTodoStatusTransition,
                "Only completed to-dos can be reopened.");
        }

        Status = PersonalTodoStatus.Open;
        CompletedAtUtc = null;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    public void Cancel(DateTimeOffset utcNow, int? expectedVersion = null)
    {
        EnsureUtc(utcNow);
        EnsureVersion(expectedVersion);

        if (Status is PersonalTodoStatus.Cancelled)
        {
            return;
        }

        if (Status is not PersonalTodoStatus.Open)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalTodoStatusTransition,
                $"Cannot cancel a to-do in status {Status}.");
        }

        Status = PersonalTodoStatus.Cancelled;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    public bool IsReminderDue(DateTimeOffset asOfUtc)
    {
        EnsureUtc(asOfUtc);
        return Status is PersonalTodoStatus.Open
            && ReminderAtUtc is DateTimeOffset reminderAt
            && reminderAt <= asOfUtc
            && ReminderNotifiedAtUtc is null;
    }

    public void MarkReminderNotified(DateTimeOffset utcNow)
    {
        EnsureUtc(utcNow);
        if (ReminderAtUtc is null)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalTodo,
                "Cannot notify a to-do without a reminder time.");
        }

        if (Status is not PersonalTodoStatus.Open)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalTodoStatusTransition,
                $"Cannot deliver a reminder for a to-do in status {Status}.");
        }

        if (ReminderNotifiedAtUtc is not null)
        {
            return;
        }

        ReminderNotifiedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
        Version++;
    }

    private void EnsureMutable()
    {
        if (Status is not PersonalTodoStatus.Open)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalTodoStatusTransition,
                $"Cannot update a to-do in status {Status}.");
        }
    }

    private void EnsureVersion(int? expectedVersion)
    {
        if (expectedVersion is null)
        {
            return;
        }

        if (expectedVersion.Value != Version)
        {
            throw new DomainException(
                DomainErrorCodes.PersonalTodoConcurrencyConflict,
                "The personal to-do was modified by another request.");
        }
    }

    private static void EnsurePriority(PersonalTodoPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalTodo, "To-do priority is invalid.");
        }
    }

    private static void EnsureRelated(PersonalTodoRelatedEntityType relatedEntityType, Guid? relatedEntityId)
    {
        if (!Enum.IsDefined(relatedEntityType))
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalTodo, "Related entity type is invalid.");
        }

        if (relatedEntityType is PersonalTodoRelatedEntityType.None)
        {
            if (relatedEntityId is not null)
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPersonalTodo,
                    "Related entity id requires a related entity type.");
            }

            return;
        }

        if (relatedEntityId is Guid id && id == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPersonalTodo,
                "Related entity id is invalid.");
        }
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalTodoTitle, "To-do title is required.");
        }

        var trimmed = title.Trim();
        if (trimmed.Length > MaxTitleLength)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalTodoTitle, "To-do title is too long.");
        }

        return trimmed;
    }

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes))
        {
            return null;
        }

        var trimmed = notes.Trim();
        if (trimmed.Length > MaxNotesLength)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalTodo, "To-do notes are too long.");
        }

        return trimmed;
    }

    private static void EnsureOptionalUtc(DateTimeOffset? value)
    {
        if (value is not null)
        {
            EnsureUtc(value.Value);
        }
    }

    private static void EnsureUtc(DateTimeOffset value)
    {
        if (value.Offset != TimeSpan.Zero)
        {
            throw new DomainException(DomainErrorCodes.InvalidUtcTimestamp, "Timestamps must be UTC.");
        }
    }
}

public sealed class PersonalTodoId : IEquatable<PersonalTodoId>
{
    public Guid Value { get; }

    private PersonalTodoId(Guid value) => Value = value;

    public static PersonalTodoId New() => new(Guid.NewGuid());

    public static PersonalTodoId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPersonalTodoId, "Personal to-do id is required.");
        }

        return new PersonalTodoId(value);
    }

    public bool Equals(PersonalTodoId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PersonalTodoId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PersonalTodoId? left, PersonalTodoId? right) => Equals(left, right);

    public static bool operator !=(PersonalTodoId? left, PersonalTodoId? right) => !Equals(left, right);
}
