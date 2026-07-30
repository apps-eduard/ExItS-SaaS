using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>Strongly typed identifier for a flat POS product category.</summary>
public sealed class ProductCategoryId : IEquatable<ProductCategoryId>
{
    public Guid Value { get; }

    private ProductCategoryId(Guid value) => Value = value;

    public static ProductCategoryId New() => new(Guid.NewGuid());

    public static ProductCategoryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductCategoryId,
                "ProductCategoryId cannot be an empty GUID.");
        }

        return new ProductCategoryId(value);
    }

    public bool Equals(ProductCategoryId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is ProductCategoryId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(ProductCategoryId? left, ProductCategoryId? right) => Equals(left, right);

    public static bool operator !=(ProductCategoryId? left, ProductCategoryId? right) => !Equals(left, right);
}
