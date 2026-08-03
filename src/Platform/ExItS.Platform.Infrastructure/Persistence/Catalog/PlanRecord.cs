namespace ExItS.Platform.Infrastructure.Persistence.Catalog;

internal sealed class PlanRecord
{
    public Guid Id { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int MaxBranches { get; set; } = 1;
    public int MaxActiveStaff { get; set; } = 3;
    public bool CustomerCreditEnabled { get; set; }
    public bool AdvancedReportsEnabled { get; set; }
    public bool ExportEnabled { get; set; }
    public bool TrialAllowed { get; set; } = true;
    public int DefaultTrialDays { get; set; } = 14;
    public int SortOrder { get; set; } = 100;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
