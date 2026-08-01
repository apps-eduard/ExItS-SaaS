using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Authorization;

public sealed class OrganizationRoleDefinitionId : IEquatable<OrganizationRoleDefinitionId>
{
    public Guid Value { get; }

    private OrganizationRoleDefinitionId(Guid value) => Value = value;

    public static OrganizationRoleDefinitionId New() => new(Guid.NewGuid());

    public static OrganizationRoleDefinitionId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationRoleDefinitionId,
                "Organization role definition id cannot be empty.");
        }

        return new OrganizationRoleDefinitionId(value);
    }

    public bool Equals(OrganizationRoleDefinitionId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is OrganizationRoleDefinitionId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(OrganizationRoleDefinitionId? left, OrganizationRoleDefinitionId? right) =>
        Equals(left, right);

    public static bool operator !=(OrganizationRoleDefinitionId? left, OrganizationRoleDefinitionId? right) =>
        !Equals(left, right);

    public override string ToString() => Value.ToString("D");
}

public sealed class OrganizationCustomRoleAssignmentId : IEquatable<OrganizationCustomRoleAssignmentId>
{
    public Guid Value { get; }

    private OrganizationCustomRoleAssignmentId(Guid value) => Value = value;

    public static OrganizationCustomRoleAssignmentId New() => new(Guid.NewGuid());

    public static OrganizationCustomRoleAssignmentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationCustomRoleAssignmentId,
                "Organization custom role assignment id cannot be empty.");
        }

        return new OrganizationCustomRoleAssignmentId(value);
    }

    public bool Equals(OrganizationCustomRoleAssignmentId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is OrganizationCustomRoleAssignmentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(OrganizationCustomRoleAssignmentId? left, OrganizationCustomRoleAssignmentId? right) =>
        Equals(left, right);

    public static bool operator !=(OrganizationCustomRoleAssignmentId? left, OrganizationCustomRoleAssignmentId? right) =>
        !Equals(left, right);

    public override string ToString() => Value.ToString("D");
}

public sealed class PlatformCustomRoleAssignmentId : IEquatable<PlatformCustomRoleAssignmentId>
{
    public Guid Value { get; }

    private PlatformCustomRoleAssignmentId(Guid value) => Value = value;

    public static PlatformCustomRoleAssignmentId New() => new(Guid.NewGuid());

    public static PlatformCustomRoleAssignmentId From(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformCustomRoleAssignmentId,
                "Platform custom role assignment id cannot be empty.");
        }

        return new PlatformCustomRoleAssignmentId(value);
    }

    public bool Equals(PlatformCustomRoleAssignmentId? other) => other is not null && Value == other.Value;

    public override bool Equals(object? obj) => obj is PlatformCustomRoleAssignmentId other && Equals(other);

    public override int GetHashCode() => Value.GetHashCode();

    public static bool operator ==(PlatformCustomRoleAssignmentId? left, PlatformCustomRoleAssignmentId? right) =>
        Equals(left, right);

    public static bool operator !=(PlatformCustomRoleAssignmentId? left, PlatformCustomRoleAssignmentId? right) =>
        !Equals(left, right);

    public override string ToString() => Value.ToString("D");
}
