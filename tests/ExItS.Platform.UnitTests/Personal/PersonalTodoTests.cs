using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Personal;

namespace ExItS.Platform.UnitTests.Personal;

public sealed class PersonalTodoTests
{
    private static readonly DateTimeOffset T0 = DateTimeOffset.Parse("2026-08-21T12:00:00Z");
    private static readonly PlatformUserId OwnerId =
        PlatformUserId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

    [Fact]
    public void Create_sets_open_status_and_version_one()
    {
        var todo = PersonalTodo.Create(OwnerId, "Buy milk", T0, priority: PersonalTodoPriority.Normal);

        Assert.Equal("Buy milk", todo.Title);
        Assert.Equal(PersonalTodoStatus.Open, todo.Status);
        Assert.Equal(PersonalTodoPriority.Normal, todo.Priority);
        Assert.Equal(1, todo.Version);
        Assert.True(todo.IsOwnedBy(OwnerId));
        Assert.Null(todo.CompletedAtUtc);
    }

    [Fact]
    public void Complete_marks_completed_and_increments_version()
    {
        var todo = PersonalTodo.Create(OwnerId, "Pay rent", T0);

        todo.Complete(T0.AddHours(1), expectedVersion: 1);

        Assert.Equal(PersonalTodoStatus.Completed, todo.Status);
        Assert.Equal(T0.AddHours(1), todo.CompletedAtUtc);
        Assert.Equal(2, todo.Version);
    }

    [Fact]
    public void Complete_rejects_stale_expected_version()
    {
        var todo = PersonalTodo.Create(OwnerId, "Call Ana", T0);

        var ex = Assert.Throws<DomainException>(() => todo.Complete(T0.AddMinutes(1), expectedVersion: 0));

        Assert.Equal(DomainErrorCodes.PersonalTodoConcurrencyConflict, ex.ErrorCode);
        Assert.Equal(PersonalTodoStatus.Open, todo.Status);
        Assert.Equal(1, todo.Version);
    }

    [Fact]
    public void Reopen_from_completed_restores_open()
    {
        var todo = PersonalTodo.Create(OwnerId, "Follow up", T0);
        todo.Complete(T0.AddMinutes(1), expectedVersion: 1);

        todo.Reopen(T0.AddMinutes(2), expectedVersion: 2);

        Assert.Equal(PersonalTodoStatus.Open, todo.Status);
        Assert.Null(todo.CompletedAtUtc);
        Assert.Equal(3, todo.Version);
    }

    [Fact]
    public void EnsureOwnedBy_rejects_other_user()
    {
        var todo = PersonalTodo.Create(OwnerId, "Private item", T0);
        var other = PlatformUserId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        var ex = Assert.Throws<DomainException>(() => todo.EnsureOwnedBy(other));

        Assert.Equal(DomainErrorCodes.PersonalTodoUnauthorized, ex.ErrorCode);
    }

    [Fact]
    public void Cancel_from_open_rejects_complete_but_allows_reopen()
    {
        var todo = PersonalTodo.Create(OwnerId, "Skip this", T0);
        todo.Cancel(T0.AddMinutes(1), expectedVersion: 1);

        Assert.Equal(PersonalTodoStatus.Cancelled, todo.Status);
        Assert.Equal(2, todo.Version);

        var ex = Assert.Throws<DomainException>(() => todo.Complete(T0.AddMinutes(2), expectedVersion: 2));
        Assert.Equal(DomainErrorCodes.InvalidPersonalTodoStatusTransition, ex.ErrorCode);

        todo.Reopen(T0.AddMinutes(3), expectedVersion: 2);
        Assert.Equal(PersonalTodoStatus.Open, todo.Status);
        Assert.Equal(3, todo.Version);
    }

    [Fact]
    public void Reminder_due_until_notified_and_reschedule_clears_notified()
    {
        var reminderAt = T0.AddHours(2);
        var todo = PersonalTodo.Create(OwnerId, "Pay bill", T0, reminderAtUtc: reminderAt);

        Assert.False(todo.IsReminderDue(T0.AddHours(1)));
        Assert.True(todo.IsReminderDue(reminderAt));

        todo.MarkReminderNotified(reminderAt.AddMinutes(1));
        Assert.False(todo.IsReminderDue(reminderAt.AddHours(1)));
        Assert.NotNull(todo.ReminderNotifiedAtUtc);

        todo.Update(
            "Pay bill",
            notes: null,
            dueAtUtc: null,
            reminderAtUtc: reminderAt.AddDays(1),
            priority: PersonalTodoPriority.None,
            relatedEntityType: PersonalTodoRelatedEntityType.None,
            relatedEntityId: null,
            utcNow: reminderAt.AddHours(2),
            expectedVersion: todo.Version);

        Assert.Null(todo.ReminderNotifiedAtUtc);
        Assert.True(todo.IsReminderDue(reminderAt.AddDays(1)));
    }
}
