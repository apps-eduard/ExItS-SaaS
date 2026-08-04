using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

public sealed class GlobalCategoryId : IEquatable<GlobalCategoryId>
{
    public Guid Value { get; }

    private GlobalCategoryId(Guid value) => Value = value;

    public static GlobalCategoryId New() => new(Guid.NewGuid());

    public static GlobalCategoryId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalCategoryId,
                "GlobalCategoryId cannot be an empty GUID.");
        }

        return new GlobalCategoryId(value);
    }

    public bool Equals(GlobalCategoryId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is GlobalCategoryId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(GlobalCategoryId? left, GlobalCategoryId? right) => Equals(left, right);
    public static bool operator !=(GlobalCategoryId? left, GlobalCategoryId? right) => !Equals(left, right);
}
