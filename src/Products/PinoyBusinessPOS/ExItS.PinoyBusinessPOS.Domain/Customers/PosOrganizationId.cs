using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Domain.Customers;

/// <summary>
/// Organization reference held by POS data. Stores the Platform organization GUID value only —
/// no cross-database foreign key to Platform.
/// </summary>
public sealed class PosOrganizationId : IEquatable<PosOrganizationId>
{
    public Guid Value { get; }

    private PosOrganizationId(Guid value) => Value = value;

    public static PosOrganizationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationId,
                "OrganizationId cannot be an empty GUID.");
        }

        return new PosOrganizationId(value);
    }

    public bool Equals(PosOrganizationId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is PosOrganizationId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(PosOrganizationId? left, PosOrganizationId? right) => Equals(left, right);

    public static bool operator !=(PosOrganizationId? left, PosOrganizationId? right) => !Equals(left, right);
}
