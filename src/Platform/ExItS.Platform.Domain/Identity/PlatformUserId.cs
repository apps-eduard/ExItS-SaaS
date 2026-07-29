using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Identity;

/// <summary>
/// Strongly typed identifier for a Platform User (global authentication identity).
/// Not a Patient, Doctor profile, POS Customer, Store employee, or Organization.
/// </summary>
public sealed class PlatformUserId : IEquatable<PlatformUserId>
{
    public Guid Value { get; }

    private PlatformUserId(Guid value) => Value = value;

    public static PlatformUserId New() => new(Guid.NewGuid());

    public static PlatformUserId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformUserId,
                "PlatformUserId cannot be an empty GUID.");
        }

        return new PlatformUserId(value);
    }

    public bool Equals(PlatformUserId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PlatformUserId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PlatformUserId? left, PlatformUserId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformUserId? left, PlatformUserId? right) =>
        !Equals(left, right);
}
