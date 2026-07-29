namespace ExItS.Platform.Infrastructure.Persistence.Catalog;

internal sealed class PlanVersionFeatureGrantRecord
{
    public Guid PlanVersionId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int? NumericLimit { get; set; }

    public PlanVersionRecord PlanVersion { get; set; } = null!;
}
