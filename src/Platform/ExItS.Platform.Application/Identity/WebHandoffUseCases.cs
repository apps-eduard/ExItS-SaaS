using System.Security.Cryptography;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Application.Identity;

public static class WebHandoffApps
{
    public const string Platform = "platform";
    public const string Organization = "organization";
    public const string Personal = "personal";

    public static bool TryNormalize(string? value, out string app)
    {
        app = (value ?? string.Empty).Trim().ToLowerInvariant();
        return app is Platform or Organization or Personal;
    }

    public static string DefaultReturnPath(string app) => app switch
    {
        Organization => "/overview",
        Personal => "/",
        _ => "/admin"
    };
}

public sealed record WebWorkspaceDto(
    string App,
    string Label,
    Guid? AccountProfileId,
    Guid? OrganizationId,
    string? OrganizationName,
    string? RoleLabel);

public sealed record WebWorkspaceListDto(
    IReadOnlyList<WebWorkspaceDto> Workspaces,
    string? CurrentApp,
    Guid? CurrentOrganizationId);

public sealed record WebHandoffCreatedDto(
    string Ticket,
    string TargetApp,
    string ReturnPath,
    DateTimeOffset ExpiresAtUtc);

public sealed record WebHandoffRedeemedDto(
    string SessionToken,
    string TargetApp,
    string ReturnPath,
    string AccountClass,
    Guid? OrganizationId,
    DateTimeOffset SessionExpiresAtUtc);

public sealed record WebHandoffTicketRecord(
    string TargetApp,
    string SessionToken,
    Guid UserId,
    string AccountClass,
    Guid? OrganizationId,
    string ReturnPath,
    DateTimeOffset ExpiresAtUtc);

public interface IWebHandoffTicketStore
{
    Task StoreAsync(string ticketHash, WebHandoffTicketRecord record, TimeSpan ttl, CancellationToken cancellationToken);

    /// <summary>Atomically read and delete. Returns null when missing.</summary>
    Task<WebHandoffTicketRecord?> TakeAsync(string ticketHash, CancellationToken cancellationToken);
}

public static class WebHandoffReturnPath
{
    public static string Sanitize(string? path, string fallback)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return fallback;
        }

        var trimmed = path.Trim();
        if (trimmed.Length > 1024
            || !trimmed.StartsWith('/')
            || trimmed.StartsWith("//", StringComparison.Ordinal)
            || trimmed.StartsWith("/\\", StringComparison.Ordinal)
            || trimmed.Contains("://", StringComparison.Ordinal)
            || trimmed.Contains('\\'))
        {
            return fallback;
        }

        return trimmed;
    }
}

public sealed class ListWebWorkspaces
{
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IAccountProfileRepository _profiles;
    private readonly IPlatformRoleAssignmentRepository _roles;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IClock _clock;

    public ListWebWorkspaces(
        IPlatformAuthSessionRepository sessions,
        IPlatformSessionTokenService tokens,
        IAccountProfileRepository profiles,
        IPlatformRoleAssignmentRepository roles,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IClock clock)
    {
        _sessions = sessions;
        _tokens = tokens;
        _profiles = profiles;
        _roles = roles;
        _memberships = memberships;
        _organizations = organizations;
        _clock = clock;
    }

    public async Task<ApplicationResult<WebWorkspaceListDto>> ExecuteAsync(
        string? opaqueToken,
        CancellationToken cancellationToken = default)
    {
        var session = await ResolveSessionAsync(opaqueToken, cancellationToken).ConfigureAwait(false);
        if (session is null)
        {
            return ApplicationResult<WebWorkspaceListDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var workspaces = await BuildAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        var currentApp = session.AccountClass switch
        {
            AccountClass.Organization => WebHandoffApps.Organization,
            AccountClass.Personal => WebHandoffApps.Personal,
            _ => WebHandoffApps.Platform
        };

        return ApplicationResult<WebWorkspaceListDto>.Success(new WebWorkspaceListDto(
            workspaces,
            currentApp,
            session.SelectedOrganizationId?.Value));
    }

    internal async Task<IReadOnlyList<WebWorkspaceDto>> BuildAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken)
    {
        var list = new List<WebWorkspaceDto>();
        var profiles = (await _profiles.ListByUserAsync(userId, cancellationToken).ConfigureAwait(false))
            .Where(p => p.IsActive)
            .ToList();

        if (profiles.Any(p => p.AccountClass is AccountClass.Platform))
        {
            var roles = await _roles.ListActiveByUserAsync(userId, cancellationToken).ConfigureAwait(false);
            if (roles.Count > 0)
            {
                var platformProfile = profiles.First(p => p.AccountClass is AccountClass.Platform);
                list.Add(new WebWorkspaceDto(
                    WebHandoffApps.Platform,
                    "Platform Administration",
                    platformProfile.Id.Value,
                    null,
                    null,
                    roles[0].Role.ToString()));
            }
        }

        if (profiles.Any(p => p.AccountClass is AccountClass.Personal))
        {
            var personal = profiles.First(p => p.AccountClass is AccountClass.Personal);
            list.Add(new WebWorkspaceDto(
                WebHandoffApps.Personal,
                "Personal",
                personal.Id.Value,
                null,
                null,
                "Personal"));
        }

        if (profiles.Any(p => p.AccountClass is AccountClass.Organization))
        {
            var orgProfile = profiles.First(p => p.AccountClass is AccountClass.Organization);
            var (memberships, _) = await _memberships
                .ListByUserAsync(userId, MembershipStatus.Active, skip: 0, take: 200, cancellationToken)
                .ConfigureAwait(false);
            foreach (var membership in memberships)
            {
                var organization = await _organizations
                    .GetByIdAsync(membership.OrganizationId, cancellationToken)
                    .ConfigureAwait(false);
                if (organization is null || organization.Status is not OrganizationStatus.Active)
                {
                    continue;
                }

                list.Add(new WebWorkspaceDto(
                    WebHandoffApps.Organization,
                    organization.DisplayName,
                    orgProfile.Id.Value,
                    organization.Id.Value,
                    organization.DisplayName,
                    membership.Role.ToString()));
            }
        }

        return list;
    }

    private async Task<PlatformAuthSession?> ResolveSessionAsync(string? opaqueToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return null;
        }

        var session = await _sessions
            .GetByTokenHashAsync(_tokens.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (session is null || !session.IsActive(_clock.UtcNow))
        {
            return null;
        }

        return session;
    }
}

public sealed class CreateWebHandoffTicket
{
    private static readonly TimeSpan TicketLifetime = TimeSpan.FromSeconds(60);

    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IAccountProfileRepository _profiles;
    private readonly IPlatformRoleAssignmentRepository _roles;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly SelectAccountProfileSession _selectProfile;
    private readonly SetSessionOrganizationContext _setOrganization;
    private readonly ListWebWorkspaces _workspaces;
    private readonly IWebHandoffTicketStore _tickets;
    private readonly IClock _clock;

    public CreateWebHandoffTicket(
        IPlatformAuthSessionRepository sessions,
        IPlatformSessionTokenService tokens,
        IAccountProfileRepository profiles,
        IPlatformRoleAssignmentRepository roles,
        IOrganizationMembershipRepository memberships,
        SelectAccountProfileSession selectProfile,
        SetSessionOrganizationContext setOrganization,
        ListWebWorkspaces workspaces,
        IWebHandoffTicketStore tickets,
        IClock clock)
    {
        _sessions = sessions;
        _tokens = tokens;
        _profiles = profiles;
        _roles = roles;
        _memberships = memberships;
        _selectProfile = selectProfile;
        _setOrganization = setOrganization;
        _workspaces = workspaces;
        _tickets = tickets;
        _clock = clock;
    }

    public async Task<ApplicationResult<WebHandoffCreatedDto>> ExecuteAsync(
        string? opaqueToken,
        string? targetApp,
        Guid? organizationId,
        string? returnPath,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (!WebHandoffApps.TryNormalize(targetApp, out var app))
        {
            return ApplicationResult<WebHandoffCreatedDto>.Failure(
                ApplicationErrorCodes.WebHandoffInvalid,
                "Target application is invalid.");
        }

        if (string.IsNullOrWhiteSpace(opaqueToken))
        {
            return ApplicationResult<WebHandoffCreatedDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var session = await _sessions
            .GetByTokenHashAsync(_tokens.HashToken(opaqueToken), cancellationToken)
            .ConfigureAwait(false);
        if (session is null || !session.IsActive(_clock.UtcNow))
        {
            return ApplicationResult<WebHandoffCreatedDto>.Failure(
                ApplicationErrorCodes.SessionInvalid,
                "Session is invalid.");
        }

        var workspaces = await _workspaces.BuildAsync(session.UserId, cancellationToken).ConfigureAwait(false);
        var match = workspaces.FirstOrDefault(w =>
            string.Equals(w.App, app, StringComparison.Ordinal)
            && (app != WebHandoffApps.Organization
                || organizationId is null
                || w.OrganizationId == organizationId));
        if (match is null)
        {
            return ApplicationResult<WebHandoffCreatedDto>.Failure(
                ApplicationErrorCodes.WebHandoffUnauthorized,
                "The signed-in identity is not authorized for that workspace.");
        }

        var sessionToken = opaqueToken;
        var accountClass = session.AccountClass.ToString();
        Guid? selectedOrg = session.SelectedOrganizationId?.Value;

        var requiredClass = app switch
        {
            WebHandoffApps.Organization => AccountClass.Organization,
            WebHandoffApps.Personal => AccountClass.Personal,
            _ => AccountClass.Platform
        };

        if (session.AccountClass != requiredClass)
        {
            var profiles = await _profiles.ListByUserAsync(session.UserId, cancellationToken).ConfigureAwait(false);
            var profile = profiles.FirstOrDefault(p => p.IsActive && p.AccountClass == requiredClass);
            if (profile is null)
            {
                return ApplicationResult<WebHandoffCreatedDto>.Failure(
                    ApplicationErrorCodes.AccountProfileNotAvailable,
                    "Required account profile is not available.");
            }

            if (requiredClass is AccountClass.Platform)
            {
                var roles = await _roles.ListActiveByUserAsync(session.UserId, cancellationToken).ConfigureAwait(false);
                if (roles.Count == 0)
                {
                    return ApplicationResult<WebHandoffCreatedDto>.Failure(
                        ApplicationErrorCodes.WebHandoffUnauthorized,
                        "Platform Administration requires a Platform role.");
                }
            }

            var switched = await _selectProfile.ExecuteAsync(
                session.UserId,
                session.Id,
                profile.Id.Value,
                ipAddress,
                userAgent,
                cancellationToken).ConfigureAwait(false);
            if (!switched.IsSuccess || switched.Value is null)
            {
                return ApplicationResult<WebHandoffCreatedDto>.Failure(
                    switched.ErrorCode ?? ApplicationErrorCodes.WebHandoffInvalid,
                    switched.ErrorMessage ?? "Could not switch account profile.");
            }

            sessionToken = switched.Value.SessionToken;
            accountClass = switched.Value.AccountClass ?? requiredClass.ToString();
            selectedOrg = switched.Value.SelectedOrganizationId;
        }

        if (app == WebHandoffApps.Organization)
        {
            var orgId = organizationId ?? selectedOrg ?? match.OrganizationId;
            if (orgId is null || orgId == Guid.Empty)
            {
                return ApplicationResult<WebHandoffCreatedDto>.Failure(
                    ApplicationErrorCodes.OrganizationContextNotEligible,
                    "An organization must be selected.");
            }

            var (memberships, _) = await _memberships
                .ListByUserAsync(session.UserId, MembershipStatus.Active, skip: 0, take: 200, cancellationToken)
                .ConfigureAwait(false);
            if (!memberships.Any(m => m.OrganizationId.Value == orgId.Value))
            {
                return ApplicationResult<WebHandoffCreatedDto>.Failure(
                    ApplicationErrorCodes.WebHandoffUnauthorized,
                    "Organization membership was not found.");
            }

            if (selectedOrg != orgId)
            {
                var context = await _setOrganization
                    .ExecuteAsync(sessionToken, orgId, cancellationToken)
                    .ConfigureAwait(false);
                if (!context.IsSuccess)
                {
                    return ApplicationResult<WebHandoffCreatedDto>.Failure(
                        context.ErrorCode ?? ApplicationErrorCodes.OrganizationContextNotEligible,
                        context.ErrorMessage ?? "Organization context was rejected.");
                }
            }

            selectedOrg = orgId;
        }

        var path = WebHandoffReturnPath.Sanitize(returnPath, WebHandoffApps.DefaultReturnPath(app));
        var expires = _clock.UtcNow.Add(TicketLifetime);
        var ticketBytes = RandomNumberGenerator.GetBytes(32);
        var ticket = Convert.ToBase64String(ticketBytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(ticket)));

        await _tickets.StoreAsync(
            hash,
            new WebHandoffTicketRecord(
                app,
                sessionToken,
                session.UserId.Value,
                accountClass,
                selectedOrg,
                path,
                expires),
            TicketLifetime,
            cancellationToken).ConfigureAwait(false);

        return ApplicationResult<WebHandoffCreatedDto>.Success(
            new WebHandoffCreatedDto(ticket, app, path, expires));
    }
}

public sealed class RedeemWebHandoffTicket
{
    private readonly IWebHandoffTicketStore _tickets;
    private readonly IClock _clock;

    public RedeemWebHandoffTicket(IWebHandoffTicketStore tickets, IClock clock)
    {
        _tickets = tickets;
        _clock = clock;
    }

    public async Task<ApplicationResult<WebHandoffRedeemedDto>> ExecuteAsync(
        string? ticket,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(ticket) || ticket.Length > 128)
        {
            return ApplicationResult<WebHandoffRedeemedDto>.Failure(
                ApplicationErrorCodes.WebHandoffInvalid,
                "Handoff ticket is invalid.");
        }

        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(ticket.Trim())));
        var record = await _tickets.TakeAsync(hash, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return ApplicationResult<WebHandoffRedeemedDto>.Failure(
                ApplicationErrorCodes.WebHandoffReplay,
                "Handoff ticket was already used or was not found.");
        }

        if (record.ExpiresAtUtc <= _clock.UtcNow)
        {
            return ApplicationResult<WebHandoffRedeemedDto>.Failure(
                ApplicationErrorCodes.WebHandoffExpired,
                "Handoff ticket has expired.");
        }

        return ApplicationResult<WebHandoffRedeemedDto>.Success(new WebHandoffRedeemedDto(
            record.SessionToken,
            record.TargetApp,
            record.ReturnPath,
            record.AccountClass,
            record.OrganizationId,
            record.ExpiresAtUtc));
    }
}
