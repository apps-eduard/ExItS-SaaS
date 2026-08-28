using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

/// <summary>Strongly typed identifier for an organization-scoped product brand.</summary>
public sealed class ProductBrandId : IEquatable<ProductBrandId>
{
    public Guid Value { get; }

    private ProductBrandId(Guid value) => Value = value;

    public static ProductBrandId New() => new(Guid.NewGuid());

    public static ProductBrandId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductBrandId,
                "ProductBrandId cannot be an empty GUID.");
        }

        return new ProductBrandId(value);
    }

    public bool Equals(ProductBrandId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is ProductBrandId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(ProductBrandId? left, ProductBrandId? right) => Equals(left, right);

    public static bool operator !=(ProductBrandId? left, ProductBrandId? right) => !Equals(left, right);
}
