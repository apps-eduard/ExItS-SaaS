namespace ExItS.Platform.Infrastructure.Persistence.Organizations;

internal sealed class BranchOperatingHoursRecord
{
    public Guid BranchId { get; set; }
    public Guid OrganizationId { get; set; }
    public int DayOfWeek { get; set; }
    public bool IsClosed { get; set; }
    public bool IsOpen24Hours { get; set; }
    public TimeOnly? OpenTime { get; set; }
    public TimeOnly? CloseTime { get; set; }
}
