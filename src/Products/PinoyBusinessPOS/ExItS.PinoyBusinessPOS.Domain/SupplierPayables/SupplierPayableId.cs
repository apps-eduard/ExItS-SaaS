using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.SupplierPayables;

/// <summary>Strongly typed identifier for a supplier payable. Not a Platform identity.</summary>
public sealed class SupplierPayableId : IEquatable<SupplierPayableId>
{
    public Guid Value { get; }

    private SupplierPayableId(Guid value) => Value = value;

    public static SupplierPayableId New() => new(Guid.NewGuid());

    public static SupplierPayableId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidSupplierPayableId,
                "SupplierPayableId cannot be an empty GUID.");
        }

        return new SupplierPayableId(value);
    }

    public bool Equals(SupplierPayableId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is SupplierPayableId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(SupplierPayableId? left, SupplierPayableId? right) => Equals(left, right);

    public static bool operator !=(SupplierPayableId? left, SupplierPayableId? right) => !Equals(left, right);
}
