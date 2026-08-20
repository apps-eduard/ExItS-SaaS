using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Infrastructure.Persistence.Access;
using ExItS.Platform.Infrastructure.Persistence.Identity;
using ExItS.Platform.Infrastructure.Persistence.Organizations;

namespace ExItS.Platform.Infrastructure.Persistence;

internal static class IdentityAccessEntityMapper
{
    public static PlatformUser ToDomain(PlatformUserRecord record) =>
        PlatformUser.Rehydrate(
            PlatformUserId.From(record.Id),
            record.Username,
            record.NormalizedUsername,
            record.DisplayName,
            record.NormalizedEmail,
            record.NormalizedContactEmail,
            record.HomeOrganizationId is null
                ? null
                : PlatformOrganizationId.From(record.HomeOrganizationId.Value),
            record.FirstName,
            record.LastName,
            record.Phone,
            record.EmployeeCode,
            record.StaffNumber,
            record.PublicUserId,
            record.CreatedByUserId is null ? null : PlatformUserId.From(record.CreatedByUserId.Value),
            Enum.Parse<AccountStatus>(record.Status),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.SuspendedAtUtc,
            record.SuspensionReason,
            record.LinkedPersonalUserId is null ? null : PlatformUserId.From(record.LinkedPersonalUserId.Value));

    public static PlatformUserRecord ToRecord(PlatformUser user) =>
        new()
        {
            Id = user.Id.Value,
            Username = user.Username,
            NormalizedUsername = user.NormalizedUsername,
            DisplayName = user.DisplayName,
            NormalizedEmail = user.NormalizedEmail,
            NormalizedContactEmail = user.NormalizedContactEmail,
            HomeOrganizationId = user.HomeOrganizationId?.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Phone = user.Phone,
            EmployeeCode = user.EmployeeCode,
            StaffNumber = user.StaffNumber,
            PublicUserId = user.PublicUserId,
            CreatedByUserId = user.CreatedByUserId?.Value,
            LinkedPersonalUserId = user.LinkedPersonalUserId?.Value,
            Status = user.Status.ToString(),
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc,
            SuspendedAtUtc = user.SuspendedAtUtc,
            SuspensionReason = user.SuspensionReason
        };

    public static void ApplyToRecord(PlatformUser user, PlatformUserRecord record)
    {
        record.Username = user.Username;
        record.NormalizedUsername = user.NormalizedUsername;
        record.DisplayName = user.DisplayName;
        record.NormalizedEmail = user.NormalizedEmail;
        record.NormalizedContactEmail = user.NormalizedContactEmail;
        record.HomeOrganizationId = user.HomeOrganizationId?.Value;
        record.FirstName = user.FirstName;
        record.LastName = user.LastName;
        record.Phone = user.Phone;
        record.EmployeeCode = user.EmployeeCode;
        record.StaffNumber = user.StaffNumber;
        record.PublicUserId = user.PublicUserId;
        record.CreatedByUserId = user.CreatedByUserId?.Value;
        record.LinkedPersonalUserId = user.LinkedPersonalUserId?.Value;
        record.Status = user.Status.ToString();
        record.UpdatedAtUtc = user.UpdatedAtUtc;
        record.SuspendedAtUtc = user.SuspendedAtUtc;
        record.SuspensionReason = user.SuspensionReason;
    }

    public static OrganizationMembership ToMembershipDomain(OrganizationMembershipRecord record) =>
        OrganizationMembership.Rehydrate(
            OrganizationMembershipId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            PlatformUserId.From(record.UserId),
            Enum.Parse<MembershipStatus>(record.Status),
            Enum.Parse<OrganizationRole>(record.Role),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.SuspendedAtUtc,
            record.RemovedAtUtc,
            record.Reason,
            record.ActorReference);

    public static OrganizationMembershipRecord ToMembershipRecord(OrganizationMembership membership) =>
        new()
        {
            Id = membership.Id.Value,
            OrganizationId = membership.OrganizationId.Value,
            UserId = membership.UserId.Value,
            Role = membership.Role.ToString(),
            Status = membership.Status.ToString(),
            CreatedAtUtc = membership.CreatedAtUtc,
            UpdatedAtUtc = membership.UpdatedAtUtc,
            SuspendedAtUtc = membership.SuspendedAtUtc,
            RemovedAtUtc = membership.RemovedAtUtc,
            Reason = membership.Reason,
            ActorReference = membership.ActorReference
        };

    public static void ApplyToMembershipRecord(OrganizationMembership membership, OrganizationMembershipRecord record)
    {
        record.Role = membership.Role.ToString();
        record.Status = membership.Status.ToString();
        record.UpdatedAtUtc = membership.UpdatedAtUtc;
        record.SuspendedAtUtc = membership.SuspendedAtUtc;
        record.RemovedAtUtc = membership.RemovedAtUtc;
        record.Reason = membership.Reason;
        record.ActorReference = membership.ActorReference;
    }

    public static ProductAccessAssignment ToAssignmentDomain(ProductAccessAssignmentRecord record) =>
        ProductAccessAssignment.Rehydrate(
            ProductAccessAssignmentId.From(record.Id),
            PlatformUserId.From(record.UserId),
            PlatformOrganizationId.From(record.OrganizationId),
            OrganizationMembershipId.From(record.MembershipId),
            ProductCode.Create(record.ProductCode),
            Enum.Parse<ProductAccessStatus>(record.Status),
            record.GrantedAtUtc,
            record.GrantedByActor,
            record.RevokedAtUtc,
            record.RevokedByActor,
            record.Reason,
            record.CreatedAtUtc,
            record.UpdatedAtUtc);

    public static ProductAccessAssignmentRecord ToAssignmentRecord(ProductAccessAssignment assignment) =>
        new()
        {
            Id = assignment.Id.Value,
            UserId = assignment.UserId.Value,
            OrganizationId = assignment.OrganizationId.Value,
            MembershipId = assignment.MembershipId.Value,
            ProductCode = assignment.ProductCode.Value,
            Status = assignment.Status.ToString(),
            GrantedAtUtc = assignment.GrantedAtUtc,
            GrantedByActor = assignment.GrantedByActor,
            RevokedAtUtc = assignment.RevokedAtUtc,
            RevokedByActor = assignment.RevokedByActor,
            Reason = assignment.Reason,
            CreatedAtUtc = assignment.CreatedAtUtc,
            UpdatedAtUtc = assignment.UpdatedAtUtc
        };

    public static void ApplyToAssignmentRecord(ProductAccessAssignment assignment, ProductAccessAssignmentRecord record)
    {
        record.Status = assignment.Status.ToString();
        record.RevokedAtUtc = assignment.RevokedAtUtc;
        record.RevokedByActor = assignment.RevokedByActor;
        record.Reason = assignment.Reason;
        record.UpdatedAtUtc = assignment.UpdatedAtUtc;
    }
}
