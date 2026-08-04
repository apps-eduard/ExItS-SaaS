using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

public sealed class CatalogTemplateId : IEquatable<CatalogTemplateId>
{
    public Guid Value { get; }

    private CatalogTemplateId(Guid value) => Value = value;

    public static CatalogTemplateId New() => new(Guid.NewGuid());

    public static CatalogTemplateId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidCatalogTemplateId,
                "CatalogTemplateId cannot be an empty GUID.");
        }

        return new CatalogTemplateId(value);
    }

    public bool Equals(CatalogTemplateId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is CatalogTemplateId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(CatalogTemplateId? left, CatalogTemplateId? right) => Equals(left, right);
    public static bool operator !=(CatalogTemplateId? left, CatalogTemplateId? right) => !Equals(left, right);
}
