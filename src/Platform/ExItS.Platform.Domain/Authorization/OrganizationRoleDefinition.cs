using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Custom organization-scoped role definition. Built-in Owner/Admin/Member remain on membership enums.
/// Permissions are organization.* codes only — never platform or product-local permissions.
/// </summary>
public sealed class OrganizationRoleDefinition
{
    private static readonly Regex CodePattern = new(@"^[A-Za-z][A-Za-z0-9_]{1,63}$", RegexOptions.Compiled);

    private readonly HashSet<string> _permissions;

    public OrganizationRoleDefinitionId Id { get; }
    public PlatformOrganizationId OrganizationId { get; }
    public string Code { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public PlatformRoleLifecycleStatus Status { get; private set; }
    public IReadOnlySet<string> Permissions => _permissions;
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }

    private OrganizationRoleDefinition(
        OrganizationRoleDefinitionId id,
        PlatformOrganizationId organizationId,
        string code,
        string name,
        string? description,
        PlatformRoleLifecycleStatus status,
        HashSet<string> permissions,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        Id = id;
        OrganizationId = organizationId;
        Code = code;
        Name = name;
        Description = description;
        Status = status;
        _permissions = permissions;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Version = version;
    }

    public static OrganizationRoleDefinition Create(
        PlatformOrganizationId organizationId,
        string code,
        string name,
        string? description,
        IEnumerable<string> permissions,
        DateTimeOffset utcNow,
        OrganizationRoleDefinitionId? id = null)
    {
        ArgumentNullException.ThrowIfNull(organizationId);
        DomainTime.EnsureUtc(utcNow);
        return new OrganizationRoleDefinition(
            id ?? OrganizationRoleDefinitionId.New(),
            organizationId,
            NormalizeCode(code),
            DomainTime.NormalizeDisplayName(name),
            NormalizeDescription(description),
            PlatformRoleLifecycleStatus.Active,
            NormalizeOrganizationPermissions(permissions),
            utcNow,
            utcNow,
            1);
    }

    public static OrganizationRoleDefinition Rehydrate(
        OrganizationRoleDefinitionId id,
        PlatformOrganizationId organizationId,
        string code,
        string name,
        string? description,
        PlatformRoleLifecycleStatus status,
        IEnumerable<string> permissions,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version) =>
        new(
            id,
            organizationId,
            code,
            name,
            description,
            status,
            new HashSet<string>(permissions, StringComparer.Ordinal),
            createdAtUtc,
            updatedAtUtc,
            version);

    public void UpdateDetails(
        string name,
        string? description,
        IEnumerable<string>? permissions,
        DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        EnsureNotRetired();
        Name = DomainTime.NormalizeDisplayName(name);
        Description = NormalizeDescription(description);
        if (permissions is not null)
        {
            _permissions.Clear();
            foreach (var permission in NormalizeOrganizationPermissions(permissions))
            {
                _permissions.Add(permission);
            }
        }

        Touch(utcNow);
    }

    public void Activate(DateTimeOffset utcNow) => TransitionTo(PlatformRoleLifecycleStatus.Active, utcNow);

    public void Deactivate(DateTimeOffset utcNow) => TransitionTo(PlatformRoleLifecycleStatus.Inactive, utcNow);

    public void Retire(DateTimeOffset utcNow) => TransitionTo(PlatformRoleLifecycleStatus.Retired, utcNow);

    public bool IsAssignable => Status == PlatformRoleLifecycleStatus.Active;

    private void TransitionTo(PlatformRoleLifecycleStatus target, DateTimeOffset utcNow)
    {
        DomainTime.EnsureUtc(utcNow);
        if (Status == target)
        {
            return;
        }

        var allowed = Status switch
        {
            PlatformRoleLifecycleStatus.Active => target is PlatformRoleLifecycleStatus.Inactive or PlatformRoleLifecycleStatus.Retired,
            PlatformRoleLifecycleStatus.Inactive => target is PlatformRoleLifecycleStatus.Active or PlatformRoleLifecycleStatus.Retired,
            PlatformRoleLifecycleStatus.Retired => false,
            _ => false
        };

        if (!allowed)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationRoleStatusTransition,
                $"Cannot transition organization role from {Status} to {target}.");
        }

        Status = target;
        Touch(utcNow);
    }

    private void EnsureNotRetired()
    {
        if (Status == PlatformRoleLifecycleStatus.Retired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationRoleStatusTransition,
                "A retired organization role cannot be updated.");
        }
    }

    private void Touch(DateTimeOffset utcNow)
    {
        UpdatedAtUtc = utcNow;
        Version++;
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code) || !CodePattern.IsMatch(code.Trim()))
        {
            throw new DomainException(
                DomainErrorCodes.InvalidOrganizationRoleCode,
                "Role code must be 2–64 characters, start with a letter, and contain only letters, digits, or underscores.");
        }

        return code.Trim();
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var trimmed = description.Trim();
        if (trimmed.Length > 512)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidDisplayName,
                "Role description must be at most 512 characters.");
        }

        return trimmed;
    }

    private static HashSet<string> NormalizeOrganizationPermissions(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in permissions)
        {
            if (string.IsNullOrWhiteSpace(raw)
                || !OrganizationPermission.All.Contains(raw.Trim(), StringComparer.Ordinal))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidOrganizationPermissionCode,
                    $"Permission code '{raw}' is not a recognized organization permission.");
            }

            set.Add(raw.Trim());
        }

        return set;
    }
}
