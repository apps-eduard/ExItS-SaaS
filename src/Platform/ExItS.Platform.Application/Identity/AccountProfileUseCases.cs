using System.Security.Cryptography;
using System.Text;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

public sealed record AccountProfileDto(
    Guid Id,
    Guid UserIdentityId,
    string AccountClass,
    string AllowedScope,
    string Status);

public sealed class EnsureAccountProfilesForUser
{
    private readonly IAccountProfileRepository _profiles;
    private readonly IPlatformRoleAssignmentRepository _roles;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public EnsureAccountProfilesForUser(
        IAccountProfileRepository profiles,
        IPlatformRoleAssignmentRepository roles,
        IOrganizationMembershipRepository memberships,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _profiles = profiles;
        _roles = roles;
        _memberships = memberships;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Ensures implied account profiles exist. Returns the preferred profile for a new session.
    /// </summary>
    public async Task<AccountProfile> ExecuteAsync(
        PlatformUserId userId,
        AccountClass? preferredClass = null,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _clock.UtcNow;
        var existing = (await _profiles.ListByUserAsync(userId, cancellationToken).ConfigureAwait(false)).ToList();

        async Task<AccountProfile> Ensure(AccountClass accountClass)
        {
            var found = existing.FirstOrDefault(p => p.AccountClass == accountClass && p.IsActive);
            if (found is not null)
            {
                return found;
            }

            var created = AccountProfile.Create(userId, accountClass, utcNow);
            await _profiles.AddAsync(created, cancellationToken).ConfigureAwait(false);
            existing.Add(created);
            return created;
        }

        await Ensure(AccountClass.Personal).ConfigureAwait(false);

        var roles = await _roles.ListActiveByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        if (roles.Count > 0)
        {
            await Ensure(AccountClass.Platform).ConfigureAwait(false);
        }

        var (memberships, _) = await _memberships
            .ListByUserAsync(userId, MembershipStatus.Active, skip: 0, take: 50, cancellationToken)
            .ConfigureAwait(false);
        if (memberships.Count > 0)
        {
            await Ensure(AccountClass.Organization).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (preferredClass is AccountClass requested)
        {
            var preferred = existing.FirstOrDefault(p => p.AccountClass == requested && p.IsActive)
                ?? throw new DomainException(
                    DomainErrorCodes.InvalidAccountStatusTransition,
                    $"Account profile class '{requested}' is not available for this identity.");
            return preferred;
        }

        return existing.FirstOrDefault(p => p.AccountClass == AccountClass.Platform && p.IsActive)
               ?? existing.FirstOrDefault(p => p.AccountClass == AccountClass.Organization && p.IsActive)
               ?? existing.First(p => p.AccountClass == AccountClass.Personal && p.IsActive);
    }
}

public sealed class ListAccountProfilesForUser
{
    private readonly IAccountProfileRepository _profiles;

    public ListAccountProfilesForUser(IAccountProfileRepository profiles) => _profiles = profiles;

    public async Task<IReadOnlyList<AccountProfileDto>> ExecuteAsync(
        PlatformUserId userId,
        CancellationToken cancellationToken = default)
    {
        var list = await _profiles.ListByUserAsync(userId, cancellationToken).ConfigureAwait(false);
        return list
            .Where(p => p.IsActive)
            .OrderBy(p => p.AccountClass)
            .Select(p => new AccountProfileDto(
                p.Id.Value,
                p.UserIdentityId.Value,
                p.AccountClass.ToString(),
                AccountClassScope.ToScope(p.AccountClass).ToString(),
                p.Status))
            .ToList();
    }
}

public sealed class SelectAccountProfileSession
{
    private readonly IPlatformUserRepository _users;
    private readonly IPlatformUserCredentialRepository _credentials;
    private readonly IAccountProfileRepository _profiles;
    private readonly IPlatformAuthSessionRepository _sessions;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationContextPreferenceRepository _orgPreferences;
    private readonly IPlatformSessionTokenService _tokens;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly PlatformSessionOptions _sessionOptions;

    public SelectAccountProfileSession(
        IPlatformUserRepository users,
        IPlatformUserCredentialRepository credentials,
        IAccountProfileRepository profiles,
        IPlatformAuthSessionRepository sessions,
        IOrganizationMembershipRepository memberships,
        IPlatformOrganizationRepository organizations,
        IOrganizationContextPreferenceRepository orgPreferences,
        IPlatformSessionTokenService tokens,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        IOptions<PlatformSessionOptions> sessionOptions)
    {
        _users = users;
        _credentials = credentials;
        _profiles = profiles;
        _sessions = sessions;
        _memberships = memberships;
        _organizations = organizations;
        _orgPreferences = orgPreferences;
        _tokens = tokens;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _sessionOptions = sessionOptions.Value;
    }

    public async Task<ApplicationResult<PlatformLoginResultDto>> ExecuteAsync(
        PlatformUserId userId,
        PlatformAuthSessionId currentSessionId,
        Guid accountProfileId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var utcNow = _clock.UtcNow;
        var user = await _users.GetByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (user is null || user.Status is not AccountStatus.Active)
        {
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Account is not eligible.");
        }

        var profile = await _profiles.GetByIdAsync(AccountProfileId.From(accountProfileId), cancellationToken)
            .ConfigureAwait(false);
        if (profile is null || !profile.IsActive || profile.UserIdentityId != userId)
        {
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.AccountProfileNotAvailable,
                "Account profile is not available for this identity.");
        }

        var credential = await _credentials.GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
        if (credential is null)
        {
            return ApplicationResult<PlatformLoginResultDto>.Failure(
                ApplicationErrorCodes.AccountNotEligibleForLogin,
                "Credential is not available.");
        }

        var current = await _sessions.GetByIdAsync(currentSessionId, cancellationToken).ConfigureAwait(false);
        if (current is not null && current.IsActive(utcNow))
        {
            current.Revoke(utcNow);
            await _sessions.UpdateAsync(current, cancellationToken).ConfigureAwait(false);
        }

        var opaqueToken = _tokens.CreateOpaqueToken();
        var tokenHash = _tokens.HashToken(opaqueToken);
        var idle = TimeSpan.FromMinutes(Math.Max(1, _sessionOptions.IdleTimeoutMinutes));
        var absolute = TimeSpan.FromHours(Math.Max(1, _sessionOptions.AbsoluteLifetimeHours));
        var session = PlatformAuthSession.Create(
            user.Id,
            profile.Id,
            profile.AccountClass,
            tokenHash,
            credential.SecurityStamp,
            utcNow,
            idle,
            absolute,
            ipAddress,
            HashUserAgent(userAgent));

        await _sessions.AddAsync(session, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Guid? orgId = null;
        string? orgName = null;
        var selectionState = "None";
        var activeCount = 0;
        if (profile.AccountClass is AccountClass.Organization)
        {
            (orgId, orgName, selectionState, activeCount) = await OrganizationContextResolver
                .ResolveAsync(
                    session,
                    _memberships,
                    _organizations,
                    _sessions,
                    _unitOfWork,
                    cancellationToken,
                    _orgPreferences,
                    utcNow)
                .ConfigureAwait(false);
        }

        await _auditWriter.WriteAsync(
            $"platform-user:{user.Id.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.PlatformAccountProfileSelected,
            nameof(AccountProfile),
            profile.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: session.SelectedOrganizationId,
            summary: $"Session bound to {profile.AccountClass} account profile.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return ApplicationResult<PlatformLoginResultDto>.Success(new PlatformLoginResultDto(
            opaqueToken,
            session.Id.Value,
            user.Id.Value,
            user.Username,
            user.DisplayName,
            user.NormalizedEmail,
            session.ExpiresAtUtc,
            session.AbsoluteExpiresAtUtc,
            orgId,
            orgName,
            selectionState,
            activeCount,
            Mfa: null,
            AccountProfileId: profile.Id.Value,
            AccountClass: profile.AccountClass.ToString(),
            AllowedScope: session.AllowedScope.ToString()));
    }

    private static string? HashUserAgent(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(userAgent.Trim())));
    }
}
