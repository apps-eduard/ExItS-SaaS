namespace ExItS.Platform.Infrastructure.Persistence.Authorization;

internal sealed class PlatformRoleDefinitionRecord
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PermissionsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int Version { get; set; }
}

internal sealed class PlatformCustomRoleAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid PlatformUserId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string GrantedByActor { get; set; } = string.Empty;
    public DateTimeOffset GrantedAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? RevokedByActor { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokeReason { get; set; }
}

internal sealed class OrganizationRoleDefinitionRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PermissionsJson { get; set; } = "[]";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
    public int Version { get; set; }
}

internal sealed class OrganizationCustomRoleAssignmentRecord
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PlatformUserId { get; set; }
    public Guid RoleDefinitionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string GrantedByActor { get; set; } = string.Empty;
    public DateTimeOffset GrantedAtUtc { get; set; }
    public string? Reason { get; set; }
    public string? RevokedByActor { get; set; }
    public DateTimeOffset? RevokedAtUtc { get; set; }
    public string? RevokeReason { get; set; }
}
