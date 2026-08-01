using System.Text.RegularExpressions;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Domain.Authorization;

/// <summary>
/// Platform role definition (built-in or custom). Permissions are Platform Admin codes only —
/// never product-local POS/clinical roles. No hard delete; use Active → Inactive → Retired.
/// </summary>
public sealed class PlatformRoleDefinition
{
    private static readonly Regex CodePattern = new(@"^[A-Za-z][A-Za-z0-9_]{1,63}$", RegexOptions.Compiled);

    private readonly HashSet<string> _permissions;

    public PlatformRoleDefinitionId Id { get; }
    public string Code { get; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public PlatformRoleKind Kind { get; }
    public PlatformRoleLifecycleStatus Status { get; private set; }
    public IReadOnlySet<string> Permissions => _permissions;
    public DateTimeOffset CreatedAtUtc { get; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public int Version { get; private set; }

    private PlatformRoleDefinition(
        PlatformRoleDefinitionId id,
        string code,
        string name,
        string? description,
        PlatformRoleKind kind,
        PlatformRoleLifecycleStatus status,
        HashSet<string> permissions,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version)
    {
        Id = id;
        Code = code;
        Name = name;
        Description = description;
        Kind = kind;
        Status = status;
        _permissions = permissions;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
        Version = version;
    }

    public static PlatformRoleDefinition CreateCustom(
        string code,
        string name,
        string? description,
        IEnumerable<string> permissions,
        DateTimeOffset utcNow,
        PlatformRoleDefinitionId? id = null)
    {
        DomainTime.EnsureUtc(utcNow);
        return new PlatformRoleDefinition(
            id ?? PlatformRoleDefinitionId.New(),
            NormalizeCode(code),
            DomainTime.NormalizeDisplayName(name),
            NormalizeDescription(description),
            PlatformRoleKind.Custom,
            PlatformRoleLifecycleStatus.Active,
            NormalizePlatformPermissions(permissions),
            utcNow,
            utcNow,
            1);
    }

    public static PlatformRoleDefinition CreateBuiltIn(
        PlatformRoleDefinitionId id,
        string code,
        string name,
        string? description,
        IEnumerable<string> permissions,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(id);
        DomainTime.EnsureUtc(utcNow);
        return new PlatformRoleDefinition(
            id,
            NormalizeCode(code),
            DomainTime.NormalizeDisplayName(name),
            NormalizeDescription(description),
            PlatformRoleKind.BuiltIn,
            PlatformRoleLifecycleStatus.Active,
            NormalizePlatformPermissions(permissions),
            utcNow,
            utcNow,
            1);
    }

    public static PlatformRoleDefinition Rehydrate(
        PlatformRoleDefinitionId id,
        string code,
        string name,
        string? description,
        PlatformRoleKind kind,
        PlatformRoleLifecycleStatus status,
        IEnumerable<string> permissions,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc,
        int version) =>
        new(
            id,
            code,
            name,
            description,
            kind,
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
            if (Kind == PlatformRoleKind.BuiltIn)
            {
                throw new DomainException(
                    DomainErrorCodes.BuiltInRoleProtected,
                    "Built-in platform role permissions cannot be changed.");
            }

            _permissions.Clear();
            foreach (var permission in NormalizePlatformPermissions(permissions))
            {
                _permissions.Add(permission);
            }
        }

        Touch(utcNow);
    }

    public void Activate(DateTimeOffset utcNow) => TransitionTo(PlatformRoleLifecycleStatus.Active, utcNow);

    public void Deactivate(DateTimeOffset utcNow)
    {
        if (Kind == PlatformRoleKind.BuiltIn)
        {
            throw new DomainException(
                DomainErrorCodes.BuiltInRoleProtected,
                "Built-in platform roles cannot be deactivated.");
        }

        TransitionTo(PlatformRoleLifecycleStatus.Inactive, utcNow);
    }

    public void Retire(DateTimeOffset utcNow)
    {
        if (Kind == PlatformRoleKind.BuiltIn)
        {
            throw new DomainException(
                DomainErrorCodes.BuiltInRoleProtected,
                "Built-in platform roles cannot be retired.");
        }

        TransitionTo(PlatformRoleLifecycleStatus.Retired, utcNow);
    }

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
                DomainErrorCodes.InvalidPlatformRoleStatusTransition,
                $"Cannot transition platform role from {Status} to {target}.");
        }

        Status = target;
        Touch(utcNow);
    }

    private void EnsureNotRetired()
    {
        if (Status == PlatformRoleLifecycleStatus.Retired)
        {
            throw new DomainException(
                DomainErrorCodes.InvalidPlatformRoleStatusTransition,
                "A retired platform role cannot be updated.");
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
                DomainErrorCodes.InvalidPlatformRoleCode,
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

    private static HashSet<string> NormalizePlatformPermissions(IEnumerable<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (var raw in permissions)
        {
            if (string.IsNullOrWhiteSpace(raw)
                || !PlatformPermission.All.Contains(raw.Trim(), StringComparer.Ordinal))
            {
                throw new DomainException(
                    DomainErrorCodes.InvalidPermissionCode,
                    $"Permission code '{raw}' is not a recognized platform permission.");
            }

            set.Add(raw.Trim());
        }

        return set;
    }
}
