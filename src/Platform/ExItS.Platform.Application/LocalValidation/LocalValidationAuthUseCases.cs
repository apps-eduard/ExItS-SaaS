using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
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
