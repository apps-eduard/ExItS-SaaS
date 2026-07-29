using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Catalog;

public sealed class PlanVersionId : IEquatable<PlanVersionId>
{
    public Guid Value { get; }

    private PlanVersionId(Guid value) => Value = value;

    public static PlanVersionId New() => new(Guid.NewGuid());

    public static PlanVersionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidPlanVersionId, "PlanVersionId cannot be an empty GUID.");
        }

        return new PlanVersionId(value);
    }

    public bool Equals(PlanVersionId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is PlanVersionId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(PlanVersionId? left, PlanVersionId? right) => Equals(left, right);
    public static bool operator !=(PlanVersionId? left, PlanVersionId? right) => !Equals(left, right);
}
