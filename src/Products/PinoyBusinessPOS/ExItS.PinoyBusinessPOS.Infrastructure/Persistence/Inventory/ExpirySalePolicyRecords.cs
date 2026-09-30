namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

internal sealed class OrganizationExpirySalePolicySettingRecord
{
    public Guid OrganizationId { get; set; }
    public int StopSellingDaysBeforeExpiry { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid UpdatedBy { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class OrganizationCategoryExpirySalePolicyRecord
{
    public Guid OrganizationId { get; set; }
    public Guid CategoryId { get; set; }
    public int StopSellingDaysBeforeExpiry { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid UpdatedBy { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class BranchExpirySalePolicySettingRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public int StopSellingDaysBeforeExpiry { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid UpdatedBy { get; set; }
    public uint Xmin { get; set; }
}

internal sealed class BranchCategoryExpirySalePolicyRecord
{
    public Guid OrganizationId { get; set; }
    public Guid BranchId { get; set; }
    public Guid CategoryId { get; set; }
    public int StopSellingDaysBeforeExpiry { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public Guid UpdatedBy { get; set; }
    public uint Xmin { get; set; }
}
