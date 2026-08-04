using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

public sealed class CatalogImportItemId : IEquatable<CatalogImportItemId>
{
    public Guid Value { get; }

    private CatalogImportItemId(Guid value) => Value = value;

    public static CatalogImportItemId New() => new(Guid.NewGuid());

    public static CatalogImportItemId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportItemId,
                "CatalogImportItemId cannot be an empty GUID.");
        }

        return new CatalogImportItemId(value);
    }

    public bool Equals(CatalogImportItemId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is CatalogImportItemId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(CatalogImportItemId? left, CatalogImportItemId? right) => Equals(left, right);
    public static bool operator !=(CatalogImportItemId? left, CatalogImportItemId? right) => !Equals(left, right);
}
