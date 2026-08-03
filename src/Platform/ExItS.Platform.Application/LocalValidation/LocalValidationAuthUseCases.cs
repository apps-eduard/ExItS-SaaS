using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.LocalValidation;

/// <summary>
/// Lists seeded local-validation identities for POS bootstrap coordination.
/// Operators authenticate through normal Platform login — there is no session bypass.
/// </summary>
public sealed class ListLocalValidationIdentities
{
    private readonly LocalValidationOptions _options;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;

    public ListLocalValidationIdentities(
        IOptions<LocalValidationOptions> options,
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations)
    {
        _options = options.Value;
        _users = users;
        _organizations = organizations;
    }

    public async Task<ApplicationResult<IReadOnlyList<LocalValidationIdentityDto>>> ExecuteAsync(
        bool isProductionEnvironment,
        CancellationToken cancellationToken = default)
    {
        if (isProductionEnvironment || !_options.Enabled)
        {
            return ApplicationResult<IReadOnlyList<LocalValidationIdentityDto>>.Failure(
                ApplicationErrorCodes.LocalValidationUnavailable,
                "Local validation seed identities are unavailable.");
        }

        var orgBySlug = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var orgDisplayBySlug = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var orgDef in LocalValidationOrganizationCatalog.All)
        {
            var org = await _organizations.GetBySlugAsync(orgDef.Slug, cancellationToken).ConfigureAwait(false);
            if (org is not null)
            {
                orgBySlug[orgDef.Slug] = org.Id.Value;
                orgDisplayBySlug[orgDef.Slug] = org.DisplayName;
            }
        }

        var list = new List<LocalValidationIdentityDto>();
        foreach (var identity in LocalValidationOptions.IdentitiesForSeedScope(_options.SeedScope))
        {
            var (_, normalized) = PlatformUser.NormalizeUsername(identity.Username);
            var user = await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken)
                .ConfigureAwait(false);
            if (user is null)
            {
                return ApplicationResult<IReadOnlyList<LocalValidationIdentityDto>>.Failure(
                    ApplicationErrorCodes.LocalValidationNotInitialized,
                    "Local validation seed identities have not been initialized yet.");
            }

            Guid? orgId = null;
            string? orgDisplay = null;
            if (!string.IsNullOrWhiteSpace(identity.OrganizationSlug))
            {
                if (!orgBySlug.TryGetValue(identity.OrganizationSlug, out var resolvedOrgId))
                {
                    return ApplicationResult<IReadOnlyList<LocalValidationIdentityDto>>.Failure(
                        ApplicationErrorCodes.LocalValidationNotInitialized,
                        $"Local validation organization '{identity.OrganizationSlug}' has not been initialized yet.");
                }

                orgId = resolvedOrgId;
                orgDisplay = orgDisplayBySlug.GetValueOrDefault(identity.OrganizationSlug)
                             ?? LocalValidationOrganizationCatalog.FindBySlug(identity.OrganizationSlug)?.DisplayName;
            }

            var listLabel = identity.PreferredAccountClass switch
            {
                AccountClass.Platform => $"Platform - {user.DisplayName}",
                AccountClass.Personal => $"Personal - {user.DisplayName}",
                AccountClass.Organization => $"{orgDisplay ?? "Organization"} - {user.DisplayName}",
                _ => user.DisplayName
            };

            list.Add(new LocalValidationIdentityDto(
                identity.Key,
                user.Username,
                user.DisplayName,
                user.NormalizedEmail,
                user.Id.Value,
                orgId,
                identity.Summary,
                identity.PosLocalRoleCode,
                listLabel));
        }

        return ApplicationResult<IReadOnlyList<LocalValidationIdentityDto>>.Success(list);
    }
}

/// <summary>
/// Dynamic Quick Login directory: eligible Active account profiles from the database.
/// Reuses SharedPassword + normal /auth/login — no session bypass.
/// </summary>
public sealed class ListLocalValidationQuickLoginIdentities
{
    private const int MaxUsers = 500;

    private readonly LocalValidationOptions _options;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IAccountProfileRepository _profiles;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IPlatformRoleAssignmentRepository _roleAssignments;

    public ListLocalValidationQuickLoginIdentities(
        IOptions<LocalValidationOptions> options,
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IAccountProfileRepository profiles,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IPlatformRoleAssignmentRepository roleAssignments)
    {
        _options = options.Value;
        _users = users;
        _credentials = credentials;
        _profiles = profiles;
        _memberships = memberships;
        _organizations = organizations;
        _roleAssignments = roleAssignments;
    }

    public async Task<ApplicationResult<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>> ExecuteAsync(
        bool isProductionEnvironment,
        CancellationToken cancellationToken = default)
    {
        if (isProductionEnvironment || !_options.Enabled)
        {
            return ApplicationResult<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>.Failure(
                ApplicationErrorCodes.LocalValidationUnavailable,
                "Local validation Quick Login identities are unavailable.");
        }

        var (users, _) = await _users
            .ListAsync(
                AccountStatus.Active,
                search: null,
                directoryFilter: null,
                sortBy: "displayName",
                sortDesc: false,
                skip: 0,
                take: MaxUsers,
                cancellationToken)
            .ConfigureAwait(false);

        var list = new List<LocalValidationQuickLoginIdentityDto>();
        foreach (var user in users)
        {
            if (user.Status is not AccountStatus.Active)
            {
                continue;
            }

            var credential = await _credentials.GetByUserIdAsync(user.Id, cancellationToken).ConfigureAwait(false);
            if (credential is null
                || !credential.SupportsPasswordLogin
                || credential.EmailVerifiedAtUtc is null
                || credential.IsLockedOut(DateTimeOffset.UtcNow))
            {
                continue;
            }

            var profiles = await _profiles.ListByUserAsync(user.Id, cancellationToken).ConfigureAwait(false);
            var activeRoles = await _roleAssignments.ListActiveByUserAsync(user.Id, cancellationToken)
                .ConfigureAwait(false);
            var hasPlatformRole = activeRoles.Any(r =>
                r.OrganizationId is null
                && r.Status == PlatformRoleAssignmentStatus.Active);

            foreach (var profile in profiles.Where(p => p.IsActive))
            {
                if (profile.AccountClass is AccountClass.Platform)
                {
                    if (!hasPlatformRole)
                    {
                        continue;
                    }

                    list.Add(CreateEntry(
                        user,
                        profile,
                        organizationId: null,
                        organizationName: null,
                        organizationRole: null,
                        scopeLabel: "Platform Administration",
                        listLabel: $"{user.DisplayName} — Platform Administration"));
                    continue;
                }

                if (profile.AccountClass is AccountClass.Personal)
                {
                    list.Add(CreateEntry(
                        user,
                        profile,
                        organizationId: null,
                        organizationName: null,
                        organizationRole: null,
                        scopeLabel: "Personal",
                        listLabel: $"{user.DisplayName} — Personal"));
                    continue;
                }

                if (profile.AccountClass is not AccountClass.Organization)
                {
                    continue;
                }

                var (memberships, _) = await _memberships
                    .ListByUserAsync(user.Id, MembershipStatus.Active, skip: 0, take: 50, cancellationToken)
                    .ConfigureAwait(false);

                foreach (var membership in memberships)
                {
                    var org = await _organizations.GetByIdAsync(membership.OrganizationId, cancellationToken)
                        .ConfigureAwait(false);
                    if (org is null)
                    {
                        continue;
                    }

                    var roleLabel = OrganizationRoleDisplay.ToDisplayLabel(membership.Role);
                    list.Add(CreateEntry(
                        user,
                        profile,
                        organizationId: org.Id.Value,
                        organizationName: org.DisplayName,
                        organizationRole: membership.Role.ToString(),
                        scopeLabel: "Organization Administration",
                        listLabel: $"{user.DisplayName} — Organization Administration · {org.DisplayName} · {roleLabel}"));
                }
            }
        }

        list.Sort((a, b) =>
        {
            var scope = string.Compare(a.ScopeLabel, b.ScopeLabel, StringComparison.OrdinalIgnoreCase);
            if (scope != 0)
            {
                return scope;
            }

            return string.Compare(a.ListLabel, b.ListLabel, StringComparison.OrdinalIgnoreCase);
        });

        return ApplicationResult<IReadOnlyList<LocalValidationQuickLoginIdentityDto>>.Success(list);
    }

    private static LocalValidationQuickLoginIdentityDto CreateEntry(
        PlatformUser user,
        AccountProfile profile,
        Guid? organizationId,
        string? organizationName,
        string? organizationRole,
        string scopeLabel,
        string listLabel)
    {
        var key = organizationId is Guid orgId
            ? $"ql:{user.Id.Value:N}:{profile.Id.Value:N}:{orgId:N}"
            : $"ql:{user.Id.Value:N}:{profile.Id.Value:N}";

        return new LocalValidationQuickLoginIdentityDto(
            key,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            user.Id.Value,
            profile.Id.Value,
            profile.AccountClass.ToString(),
            organizationId,
            organizationName,
            organizationRole,
            listLabel,
            scopeLabel);
    }
}
