namespace ExItS.Platform.Infrastructure.Persistence.Identity;

internal sealed class AccountProfileRecord
{
    public Guid Id { get; set; }
    public Guid UserIdentityId { get; set; }
    public string AccountClass { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
