using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Products;

public sealed class ProductAccessAssignmentId : IEquatable<ProductAccessAssignmentId>
{
    public Guid Value { get; }

    private ProductAccessAssignmentId(Guid value) => Value = value;

    public static ProductAccessAssignmentId New() => new(Guid.NewGuid());

    public static ProductAccessAssignmentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidProductAccessAssignmentId,
                "Product access assignment id cannot be empty.");
        }

        return new ProductAccessAssignmentId(value);
    }

    public bool Equals(ProductAccessAssignmentId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is ProductAccessAssignmentId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();
}
