using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

public sealed class StaffPasswordResetRequestId : IEquatable<StaffPasswordResetRequestId>
{
    public Guid Value { get; }

    private StaffPasswordResetRequestId(Guid value) => Value = value;

    public static StaffPasswordResetRequestId New() => new(Guid.NewGuid());

    public static StaffPasswordResetRequestId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidStaffPasswordResetRequestId,
                "StaffPasswordResetRequestId cannot be an empty GUID.");
        }

        return new StaffPasswordResetRequestId(value);
    }

    public bool Equals(StaffPasswordResetRequestId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) => obj is StaffPasswordResetRequestId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(StaffPasswordResetRequestId? left, StaffPasswordResetRequestId? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(StaffPasswordResetRequestId? left, StaffPasswordResetRequestId? right) =>
        !(left == right);
}
