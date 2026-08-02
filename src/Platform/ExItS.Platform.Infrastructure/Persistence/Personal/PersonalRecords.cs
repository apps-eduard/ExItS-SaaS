namespace ExItS.Platform.Infrastructure.Persistence.Personal;

internal sealed class PersonalAccountSettingsRecord
{
    public Guid UserIdentityId { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public bool InAppNotificationsEnabled { get; set; }
    public bool ReminderNotificationsEnabled { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int Version { get; set; }
    public uint Xmin { get; set; }
}
