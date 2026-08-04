using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

public sealed class CatalogImportJobId : IEquatable<CatalogImportJobId>
{
    public Guid Value { get; }

    private CatalogImportJobId(Guid value) => Value = value;

    public static CatalogImportJobId New() => new(Guid.NewGuid());

    public static CatalogImportJobId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogImportJobId,
                "CatalogImportJobId cannot be an empty GUID.");
        }

        return new CatalogImportJobId(value);
    }

    public bool Equals(CatalogImportJobId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is CatalogImportJobId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(CatalogImportJobId? left, CatalogImportJobId? right) => Equals(left, right);
    public static bool operator !=(CatalogImportJobId? left, CatalogImportJobId? right) => !Equals(left, right);
}
