namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Registers;

internal sealed class RegisterRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string RegisterCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid UpdatedBy { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class RegisterCodeSequenceRecord
{
    public Guid OrganizationId { get; set; }
    public long NextValue { get; set; }
}
