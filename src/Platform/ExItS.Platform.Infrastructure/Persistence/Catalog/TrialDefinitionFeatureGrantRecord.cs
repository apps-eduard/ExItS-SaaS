namespace ExItS.Platform.Infrastructure.Persistence.Catalog;

internal enum TrialGrantKind
{
    DuringTrial,
    PostExpiry
}

internal sealed class TrialDefinitionFeatureGrantRecord
{
    public Guid TrialDefinitionId { get; set; }
    public string FeatureCode { get; set; } = string.Empty;
    public string GrantKind { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public int? NumericLimit { get; set; }

    public TrialDefinitionRecord TrialDefinition { get; set; } = null!;
}
