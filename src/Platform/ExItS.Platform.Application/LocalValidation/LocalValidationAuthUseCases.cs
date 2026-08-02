using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.LocalValidation;

/// <summary>
/// Lists seeded local-validation identities for POS bootstrap coordination.
/// Operators authenticate through normal Platform login — there is no normal login path.
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

        var org = await _organizations.GetBySlugAsync(LocalValidationOptions.OrgSlug, cancellationToken)
            .ConfigureAwait(false);
        Guid? orgId = org?.Id.Value;

        var list = new List<LocalValidationIdentityDto>(LocalValidationIdentityCatalog.All.Count);
        foreach (var identity in LocalValidationIdentityCatalog.All)
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

            list.Add(new LocalValidationIdentityDto(
                identity.Key,
                user.Username,
                user.DisplayName,
                user.NormalizedEmail,
                user.Id.Value,
                identity.HasOrganizationMembership ? orgId : null,
                identity.Summary,
                identity.PosLocalRoleCode));
        }

        return ApplicationResult<IReadOnlyList<LocalValidationIdentityDto>>.Success(list);
    }
}
