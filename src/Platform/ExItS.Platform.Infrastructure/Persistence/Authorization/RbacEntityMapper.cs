using System.Text.Json;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Infrastructure.Persistence.Authorization;

internal static class RbacEntityMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public static PlatformRoleDefinition ToDomain(PlatformRoleDefinitionRecord record) =>
        PlatformRoleDefinition.Rehydrate(
            PlatformRoleDefinitionId.From(record.Id),
            record.Code,
            record.Name,
            record.Description,
            Enum.Parse<PlatformRoleKind>(record.Kind),
            Enum.Parse<PlatformRoleLifecycleStatus>(record.Status),
            DeserializePermissions(record.PermissionsJson),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.Version);

    public static PlatformRoleDefinitionRecord ToRecord(PlatformRoleDefinition definition) =>
        new()
        {
            Id = definition.Id.Value,
            Code = definition.Code,
            Name = definition.Name,
            Description = definition.Description,
            Kind = definition.Kind.ToString(),
            Status = definition.Status.ToString(),
            PermissionsJson = SerializePermissions(definition.Permissions),
            CreatedAtUtc = definition.CreatedAtUtc,
            UpdatedAtUtc = definition.UpdatedAtUtc,
            Version = definition.Version
        };

    public static void ApplyToRecord(PlatformRoleDefinition definition, PlatformRoleDefinitionRecord record)
    {
        record.Name = definition.Name;
        record.Description = definition.Description;
        record.Status = definition.Status.ToString();
        record.PermissionsJson = SerializePermissions(definition.Permissions);
        record.UpdatedAtUtc = definition.UpdatedAtUtc;
        record.Version = definition.Version;
    }

    public static PlatformCustomRoleAssignment ToDomain(PlatformCustomRoleAssignmentRecord record) =>
        PlatformCustomRoleAssignment.Rehydrate(
            PlatformCustomRoleAssignmentId.From(record.Id),
            PlatformUserId.From(record.PlatformUserId),
            PlatformRoleDefinitionId.From(record.RoleDefinitionId),
            Enum.Parse<PlatformRoleAssignmentStatus>(record.Status),
            record.GrantedByActor,
            record.GrantedAtUtc,
            record.Reason,
            record.RevokedByActor,
            record.RevokedAtUtc,
            record.RevokeReason);

    public static PlatformCustomRoleAssignmentRecord ToRecord(PlatformCustomRoleAssignment assignment) =>
        new()
        {
            Id = assignment.Id.Value,
            PlatformUserId = assignment.PlatformUserId.Value,
            RoleDefinitionId = assignment.RoleDefinitionId.Value,
            Status = assignment.Status.ToString(),
            GrantedByActor = assignment.GrantedByActor,
            GrantedAtUtc = assignment.GrantedAtUtc,
            Reason = assignment.Reason,
            RevokedByActor = assignment.RevokedByActor,
            RevokedAtUtc = assignment.RevokedAtUtc,
            RevokeReason = assignment.RevokeReason
        };

    public static void ApplyToRecord(PlatformCustomRoleAssignment assignment, PlatformCustomRoleAssignmentRecord record)
    {
        record.Status = assignment.Status.ToString();
        record.RevokedByActor = assignment.RevokedByActor;
        record.RevokedAtUtc = assignment.RevokedAtUtc;
        record.RevokeReason = assignment.RevokeReason;
    }

    public static OrganizationRoleDefinition ToDomain(OrganizationRoleDefinitionRecord record) =>
        OrganizationRoleDefinition.Rehydrate(
            OrganizationRoleDefinitionId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            record.Code,
            record.Name,
            record.Description,
            Enum.Parse<PlatformRoleLifecycleStatus>(record.Status),
            DeserializePermissions(record.PermissionsJson),
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            record.Version);

    public static OrganizationRoleDefinitionRecord ToRecord(OrganizationRoleDefinition definition) =>
        new()
        {
            Id = definition.Id.Value,
            OrganizationId = definition.OrganizationId.Value,
            Code = definition.Code,
            Name = definition.Name,
            Description = definition.Description,
            Status = definition.Status.ToString(),
            PermissionsJson = SerializePermissions(definition.Permissions),
            CreatedAtUtc = definition.CreatedAtUtc,
            UpdatedAtUtc = definition.UpdatedAtUtc,
            Version = definition.Version
        };

    public static void ApplyToRecord(OrganizationRoleDefinition definition, OrganizationRoleDefinitionRecord record)
    {
        record.Name = definition.Name;
        record.Description = definition.Description;
        record.Status = definition.Status.ToString();
        record.PermissionsJson = SerializePermissions(definition.Permissions);
        record.UpdatedAtUtc = definition.UpdatedAtUtc;
        record.Version = definition.Version;
    }

    public static OrganizationCustomRoleAssignment ToDomain(OrganizationCustomRoleAssignmentRecord record) =>
        OrganizationCustomRoleAssignment.Rehydrate(
            OrganizationCustomRoleAssignmentId.From(record.Id),
            PlatformOrganizationId.From(record.OrganizationId),
            PlatformUserId.From(record.PlatformUserId),
            OrganizationRoleDefinitionId.From(record.RoleDefinitionId),
            Enum.Parse<PlatformRoleAssignmentStatus>(record.Status),
            record.GrantedByActor,
            record.GrantedAtUtc,
            record.Reason,
            record.RevokedByActor,
            record.RevokedAtUtc,
            record.RevokeReason);

    public static OrganizationCustomRoleAssignmentRecord ToRecord(OrganizationCustomRoleAssignment assignment) =>
        new()
        {
            Id = assignment.Id.Value,
            OrganizationId = assignment.OrganizationId.Value,
            PlatformUserId = assignment.PlatformUserId.Value,
            RoleDefinitionId = assignment.RoleDefinitionId.Value,
            Status = assignment.Status.ToString(),
            GrantedByActor = assignment.GrantedByActor,
            GrantedAtUtc = assignment.GrantedAtUtc,
            Reason = assignment.Reason,
            RevokedByActor = assignment.RevokedByActor,
            RevokedAtUtc = assignment.RevokedAtUtc,
            RevokeReason = assignment.RevokeReason
        };

    public static void ApplyToRecord(OrganizationCustomRoleAssignment assignment, OrganizationCustomRoleAssignmentRecord record)
    {
        record.Status = assignment.Status.ToString();
        record.RevokedByActor = assignment.RevokedByActor;
        record.RevokedAtUtc = assignment.RevokedAtUtc;
        record.RevokeReason = assignment.RevokeReason;
    }

    private static string SerializePermissions(IEnumerable<string> permissions) =>
        JsonSerializer.Serialize(permissions.OrderBy(p => p, StringComparer.Ordinal).ToArray(), JsonOptions);

    private static IReadOnlyList<string> DeserializePermissions(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<string>();
        }

        return JsonSerializer.Deserialize<string[]>(json, JsonOptions) ?? Array.Empty<string>();
    }
}
