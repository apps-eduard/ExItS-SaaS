using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Catalog;

public sealed class TrialDefinitionId : IEquatable<TrialDefinitionId>
{
    public Guid Value { get; }

    private TrialDefinitionId(Guid value) => Value = value;

    public static TrialDefinitionId New() => new(Guid.NewGuid());

    public static TrialDefinitionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidTrialDefinitionId, "TrialDefinitionId cannot be an empty GUID.");
        }

        return new TrialDefinitionId(value);
    }

    public bool Equals(TrialDefinitionId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is TrialDefinitionId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(TrialDefinitionId? left, TrialDefinitionId? right) => Equals(left, right);
    public static bool operator !=(TrialDefinitionId? left, TrialDefinitionId? right) => !Equals(left, right);
}
