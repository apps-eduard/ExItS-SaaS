using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class PosDeviceId : IEquatable<PosDeviceId>
{
    public Guid Value { get; }

    private PosDeviceId(Guid value) => Value = value;

    public static PosDeviceId New() => new(Guid.NewGuid());

    public static PosDeviceId From(Guid value) =>
        value == Guid.Empty
            ? throw new DomainException(DomainErrorCodes.InvalidPosDeviceId, "PosDeviceId cannot be an empty GUID.")
            : new PosDeviceId(value);

    public bool Equals(PosDeviceId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is PosDeviceId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString("D");
    public static bool operator ==(PosDeviceId? left, PosDeviceId? right) => Equals(left, right);
    public static bool operator !=(PosDeviceId? left, PosDeviceId? right) => !Equals(left, right);
}
