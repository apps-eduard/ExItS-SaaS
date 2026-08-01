using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Organizations;

/// <summary>Strongly typed identifier for an organization membership invitation.</summary>
public sealed class OrganizationInvitationId : IEquatable<OrganizationInvitationId>
{
    public Guid Value { get; }

    private OrganizationInvitationId(Guid value) => Value = value;

    public static OrganizationInvitationId New() => new(Guid.NewGuid());

    public static OrganizationInvitationId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationInvitationId,
                "OrganizationInvitationId cannot be an empty GUID.");
        }

        return new OrganizationInvitationId(value);
    }

    public bool Equals(OrganizationInvitationId? other) =>
        other is not null && Value.Equals(other.Value);

    public override bool Equals(object? obj) =>
        obj is OrganizationInvitationId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public override string ToString() => Value.ToString("D");

    public static bool operator ==(OrganizationInvitationId? left, OrganizationInvitationId? right) =>
        Equals(left, right);

    public static bool operator !=(OrganizationInvitationId? left, OrganizationInvitationId? right) =>
        !Equals(left, right);
}
