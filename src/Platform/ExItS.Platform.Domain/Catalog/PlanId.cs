using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Catalog;

public sealed class PlanId : IEquatable<PlanId>
{
    public Guid Value { get; }

    private PlanId(Guid value) => Value = value;

    public static PlanId New() => new(Guid.NewGuid());

    public static PlanId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlanId, "PlanId cannot be an empty GUID.");
        }

        return new PlanId(value);
    }

    public bool Equals(PlanId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is PlanId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(PlanId? left, PlanId? right) => Equals(left, right);
    public static bool operator !=(PlanId? left, PlanId? right) => !Equals(left, right);
}
