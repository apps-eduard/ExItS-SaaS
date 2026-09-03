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
    public int MaxActivePosDevices { get; set; } = 1;
    public int MaxActiveBusinessTypes { get; set; } = 1;
    public int MaxAreas { get; set; } = 1;
    public bool CustomerCreditEnabled { get; set; }
    public bool AdvancedReportsEnabled { get; set; }
    public bool ExportEnabled { get; set; }
    public bool TrialAllowed { get; set; } = true;
    public int DefaultTrialDays { get; set; } = 14;
    public int SortOrder { get; set; } = 100;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public string CurrencyCode { get; set; } = "PHP";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
