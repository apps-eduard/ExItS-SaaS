using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Authorization;

/// <summary>Strongly typed identifier for a Platform system role assignment.</summary>
public sealed class PlatformRoleAssignmentId : IEquatable<PlatformRoleAssignmentId>
{
    public Guid Value { get; }

    private PlatformRoleAssignmentId(Guid value) => Value = value;

    public static PlatformRoleAssignmentId New() => new(Guid.NewGuid());

    public static PlatformRoleAssignmentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformRoleAssignmentId,
                "PlatformRoleAssignmentId cannot be an empty GUID.");
        }

        return new PlatformRoleAssignmentId(value);
    }

    public bool Equals(PlatformRoleAssignmentId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PlatformRoleAssignmentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PlatformRoleAssignmentId? left, PlatformRoleAssignmentId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformRoleAssignmentId? left, PlatformRoleAssignmentId? right) =>
        !Equals(left, right);
}
