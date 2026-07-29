using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Contracts;

/// <summary>
/// Platform organization membership projection.
/// Does not convey Doctor, Nurse, Clinic Admin, Patient, or other clinical roles.
/// </summary>
public sealed class OrganizationMembershipProjection
{
    public PlatformOrganizationId PlatformOrganizationId { get; }
    public PlatformUserId PlatformUserId { get; }
    public MembershipStatus MembershipStatus { get; }
    public OrganizationRole OrganizationRole { get; }
    public DateTimeOffset UpdatedAtUtc { get; }
    public int SourceVersion { get; }

    public OrganizationMembershipProjection(
        PlatformOrganizationId platformOrganizationId,
        PlatformUserId platformUserId,
        MembershipStatus membershipStatus,
        OrganizationRole organizationRole,
        DateTimeOffset updatedAtUtc,
        int sourceVersion)
    {
        ArgumentNullException.ThrowIfNull(platformOrganizationId);
        ArgumentNullException.ThrowIfNull(platformUserId);

        if (updatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new ContractException(DomainErrorCodes.InvalidUtcTimestamp, "UpdatedAt must be UTC.");
        }

        if (sourceVersion < 1)
        {
            throw new ContractException(ContractErrorCodes.InvalidSourceVersion, "Source version must be positive.");
        }

        if (!Enum.IsDefined(membershipStatus) || !Enum.IsDefined(organizationRole))
        {
            throw new ContractException(ContractErrorCodes.InvalidContractEnvelope, "Membership status or role is invalid.");
        }

        PlatformOrganizationId = platformOrganizationId;
        PlatformUserId = platformUserId;
        MembershipStatus = membershipStatus;
        OrganizationRole = organizationRole;
        UpdatedAtUtc = updatedAtUtc;
        SourceVersion = sourceVersion;
    }
}
