namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Payments;

internal sealed class OrganizationPaymentMethodSettingRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string MethodCode { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string? DisplayName { get; set; }
    public bool RequireReference { get; set; }
    public string BranchScope { get; set; } = "AllBranches";
    public Guid[] SelectedBranchIds { get; set; } = [];
    public string? Instructions { get; set; }
    public string? AccountHint { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
