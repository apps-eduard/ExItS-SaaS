using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>Strongly typed identifier for a product-specific purchase or sell unit.</summary>
public sealed class ProductUnitId : IEquatable<ProductUnitId>
{
    public Guid Value { get; }

    private ProductUnitId(Guid value) => Value = value;

    public static ProductUnitId New() => new(Guid.NewGuid());

    public static ProductUnitId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductUnitId,
                "ProductUnitId cannot be an empty GUID.");
        }

        return new ProductUnitId(value);
    }

    public bool Equals(ProductUnitId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is ProductUnitId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(ProductUnitId? left, ProductUnitId? right) => Equals(left, right);

    public static bool operator !=(ProductUnitId? left, ProductUnitId? right) => !Equals(left, right);
}
