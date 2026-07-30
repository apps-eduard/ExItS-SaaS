using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>Strongly typed identifier for a POS catalog product. Not a Platform or HealthCare identity.</summary>
public sealed class CatalogProductId : IEquatable<CatalogProductId>
{
    public Guid Value { get; }

    private CatalogProductId(Guid value) => Value = value;

    public static CatalogProductId New() => new(Guid.NewGuid());

    public static CatalogProductId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogProductId,
                "CatalogProductId cannot be an empty GUID.");
        }

        return new CatalogProductId(value);
    }

    public bool Equals(CatalogProductId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is CatalogProductId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(CatalogProductId? left, CatalogProductId? right) => Equals(left, right);

    public static bool operator !=(CatalogProductId? left, CatalogProductId? right) => !Equals(left, right);
}
