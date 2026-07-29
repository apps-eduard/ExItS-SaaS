namespace ExItS.Platform.Infrastructure.Persistence.Access;

internal sealed class ProductAccessAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid MembershipId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset GrantedAtUtc { get; set; }
    public string GrantedByActor { get; set; } = string.Empty;
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokedByActor { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public uint Xmin { get; set; }
}
