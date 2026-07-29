using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Catalog;

public sealed class ProductId : IEquatable<ProductId>
{
    public Guid Value { get; }

    private ProductId(Guid value) => Value = value;

    public static ProductId New() => new(Guid.NewGuid());

    public static ProductId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(DomainErrorCodes.InvalidProductId, "ProductId cannot be an empty GUID.");
        }

        return new ProductId(value);
    }

    public bool Equals(ProductId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is ProductId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(ProductId? left, ProductId? right) => Equals(left, right);
    public static bool operator !=(ProductId? left, ProductId? right) => !Equals(left, right);
}
