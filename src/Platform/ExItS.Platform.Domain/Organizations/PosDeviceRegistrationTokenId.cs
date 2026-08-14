using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class PosDeviceRegistrationTokenId : IEquatable<PosDeviceRegistrationTokenId>
{
    public Guid Value { get; }

    private PosDeviceRegistrationTokenId(Guid value) => Value = value;

    public static PosDeviceRegistrationTokenId New() => new(Guid.NewGuid());

    public static PosDeviceRegistrationTokenId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException(
                DomainErrorCodes.InvalidPosDeviceRegistrationTokenId,
                "PosDeviceRegistrationTokenId cannot be an empty GUID.")
            : new PosDeviceRegistrationTokenId(value);

    public bool Equals(PosDeviceRegistrationTokenId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is PosDeviceRegistrationTokenId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(PosDeviceRegistrationTokenId? left, PosDeviceRegistrationTokenId? right) =>
        Equals(left, right);
    public static bool operator !=(PosDeviceRegistrationTokenId? left, PosDeviceRegistrationTokenId? right) =>
        !Equals(left, right);
}
