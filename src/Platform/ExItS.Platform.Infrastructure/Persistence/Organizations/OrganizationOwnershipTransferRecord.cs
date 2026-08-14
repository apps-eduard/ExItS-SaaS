namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class OrganizationOwnershipTransferRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FromOwnerUserId { get; set; }
    public Guid ToUserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
    public DateTimeOffset? AcceptedAtUtc { get; set; }
    public DateTimeOffset? DeclinedAtUtc { get; set; }
    public DateTimeOffset? CancelledAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
