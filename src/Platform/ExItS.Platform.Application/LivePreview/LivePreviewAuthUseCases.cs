using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Identity;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.LivePreview;

public sealed class ListLivePreviewIdentities
{
    private readonly LivePreviewOptions _options;
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformOrganizationRepository _organizations;

    public ListLivePreviewIdentities(
        IOptions<LivePreviewOptions> options,
        IPlatformUserRepository users,
        IPlatformOrganizationRepository organizations)
    {
        _options = options.Value;
        _users = users;
        _organizations = organizations;
    }

    public async Task<ApplicationResult<IReadOnlyList<LivePreviewIdentityDto>>> ExecuteAsync(
        bool isProductionEnvironment,
        CancellationToken cancellationToken = default)
    {
        if (isProductionEnvironment || !_options.Enabled)
        {
            return ApplicationResult<IReadOnlyList<LivePreviewIdentityDto>>.Failure(
                ApplicationErrorCodes.LivePreviewUnavailable,
                "Live preview identities are unavailable.");
        }

        var org = await _organizations.GetBySlugAsync(LivePreviewOptions.OrgSlug, cancellationToken)
            .ConfigureAwait(false);
        Guid? orgId = org?.Id.Value;

        var list = new List<LivePreviewIdentityDto>(LivePreviewIdentityCatalog.All.Count);
        foreach (var identity in LivePreviewIdentityCatalog.All)
        {
            var (_, normalized) = PlatformUser.NormalizeUsername(identity.Username);
            var user = await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken)
                .ConfigureAwait(false);
            if (user is null)
            {
                return ApplicationResult<IReadOnlyList<LivePreviewIdentityDto>>.Failure(
                    ApplicationErrorCodes.LivePreviewNotInitialized,
                    "Live preview identities have not been initialized yet.");
            }

            list.Add(new LivePreviewIdentityDto(
                identity.Key,
                user.Username,
                user.DisplayName,
                user.NormalizedEmail,
                user.Id.Value,
                identity.HasOrganizationMembership ? orgId : null,
                identity.Summary,
                identity.PosLocalRoleCode));
        }

        return ApplicationResult<IReadOnlyList<LivePreviewIdentityDto>>.Success(list);
    }
}

public sealed class LoginLivePreviewIdentity
{
    private readonly LivePreviewOptions _options;
    private readonly LoginPlatformUser _login;

    public LoginLivePreviewIdentity(
        IOptions<LivePreviewOptions> options,
        LoginPlatformUser login)
    {
        _options = options.Value;
        _login = login;
    }

    public async Task<ApplicationResult<PlatformLoginResultDto>> ExecuteAsync(
        string? identityKey,
        bool isProductionEnvironment,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (isProductionEnvironment || !_options.Enabled)
        {
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.LivePreviewUnavailable,
                "Live preview login is unavailable.");
        }

        var identity = LivePreviewIdentityCatalog.FindByKey(identityKey);
        if (identity is null)
        {
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.LivePreviewIdentityUnknown,
                "Unknown live preview identity.");
        }

        if (string.IsNullOrWhiteSpace(_options.SharedPassword))
        {
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.LivePreviewUnavailable,
                "Live preview shared password is not configured.");
        }

        return await _login
            .ExecuteAsync(identity.Username, _options.SharedPassword, ipAddress, userAgent, cancellationToken)
            .ConfigureAwait(false);
    }
}
