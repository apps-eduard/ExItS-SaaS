namespace ExItS.Platform.Infrastructure.Persistence.Catalog;

internal sealed class PlanVersionBusinessTypeGrantRecord
{
    public Guid PlanVersionId { get; set; }
    public Guid BusinessTypeId { get; set; }

    public PlanVersionRecord PlanVersion { get; set; } = null!;
}
