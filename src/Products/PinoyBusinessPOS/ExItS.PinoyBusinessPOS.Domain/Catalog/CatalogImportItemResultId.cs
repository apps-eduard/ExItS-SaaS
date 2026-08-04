using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

public sealed class CatalogImportItemResultId : IEquatable<CatalogImportItemResultId>
{
    public Guid Value { get; }

    private CatalogImportItemResultId(Guid value) => Value = value;

    public static CatalogImportItemResultId New() => new(Guid.NewGuid());

    public static CatalogImportItemResultId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportItemId,
                "CatalogImportItemResultId cannot be an empty GUID.");
        }

        return new CatalogImportItemResultId(value);
    }

    public bool Equals(CatalogImportItemResultId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is CatalogImportItemResultId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(CatalogImportItemResultId? left, CatalogImportItemResultId? right) =>
        Equals(left, right);
    public static bool operator !=(CatalogImportItemResultId? left, CatalogImportItemResultId? right) =>
        !Equals(left, right);
}
