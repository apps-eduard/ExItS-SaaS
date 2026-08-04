using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.GlobalCatalog;

public sealed class GlobalProductId : IEquatable<GlobalProductId>
{
    public Guid Value { get; }

    private GlobalProductId(Guid value) => Value = value;

    public static GlobalProductId New() => new(Guid.NewGuid());

    public static GlobalProductId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidGlobalProductId,
                "GlobalProductId cannot be an empty GUID.");
        }

        return new GlobalProductId(value);
    }

    public bool Equals(GlobalProductId? other) => other is not null && Value.Equals(other.Value);
    public override bool Equals(object? obj) => obj is GlobalProductId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(GlobalProductId? left, GlobalProductId? right) => Equals(left, right);
    public static bool operator !=(GlobalProductId? left, GlobalProductId? right) => !Equals(left, right);
}
