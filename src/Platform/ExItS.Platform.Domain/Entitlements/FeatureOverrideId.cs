using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Entitlements;

public sealed class FeatureOverrideId : IEquatable<FeatureOverrideId>
{
    public Guid Value { get; }

    private FeatureOverrideId(Guid value) => Value = value;

    public static FeatureOverrideId New() => new(Guid.NewGuid());

    public static FeatureOverrideId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidFeatureOverrideId,
                "FeatureOverrideId cannot be an empty GUID.");
        }

        return new FeatureOverrideId(value);
    }

    public bool Equals(FeatureOverrideId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is FeatureOverrideId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(FeatureOverrideId? left, FeatureOverrideId? right) => Equals(left, right);
    public static bool operator !=(FeatureOverrideId? left, FeatureOverrideId? right) => !Equals(left, right);
}
