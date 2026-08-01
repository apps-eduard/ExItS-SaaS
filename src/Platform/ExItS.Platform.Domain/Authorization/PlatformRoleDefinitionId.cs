using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Authorization;

public sealed class PlatformRoleDefinitionId : IEquatable<PlatformRoleDefinitionId>
{
    public Guid Value { get; }

    private PlatformRoleDefinitionId(Guid value) => Value = value;

    public static PlatformRoleDefinitionId New() => new(Guid.NewGuid());

    public static PlatformRoleDefinitionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformRoleDefinitionId,
                "Platform role definition id cannot be empty.");
        }

        return new PlatformRoleDefinitionId(value);
    }

    public bool Equals(PlatformRoleDefinitionId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is PlatformRoleDefinitionId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(PlatformRoleDefinitionId? left, PlatformRoleDefinitionId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformRoleDefinitionId? left, PlatformRoleDefinitionId? right) =>
        !Equals(left, right);

    public override string ToString() => Value.ToString("D");
}
