using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.LivePreview;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.Identity;

/// <summary>
/// Development/Testing/LivePreview-only deterministic Phase 16 identity seed (idempotent).
/// Never runs in Production.
/// </summary>
public sealed class InitializePhase16AccountSeed
{
    public const string SeedOrgSlug = "phase16-seed-org";
    public const string SeedOrgDisplayName = "Phase16 Seed Organization";

    private readonly IHostEnvironment _environment;
    private readonly LivePreviewOptions _livePreview;
    private readonly CreatePlatformUser _createUser;
    private readonly IPlatformUserRepository _users;
    private readonly SetPlatformUserPassword _setPassword;
    private readonly AssignPlatformRole _assignPlatformRole;
    private readonly CreatePlatformOrganization _createOrg;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly AddOrganizationMembership _addMembership;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly InitializePhase16PersonalUtangSeed _personalUtangSeed;
    private readonly ILogger<InitializePhase16AccountSeed> _logger;

    public InitializePhase16AccountSeed(
        IHostEnvironment environment,
        IOptions<LivePreviewOptions> livePreview,
        CreatePlatformUser createUser,
        IPlatformUserRepository users,
        SetPlatformUserPassword setPassword,
        AssignPlatformRole assignPlatformRole,
        CreatePlatformOrganization createOrg,
        IPlatformOrganizationRepository organizations,
        AddOrganizationMembership addMembership,
        IOrganizationMembershipRepository memberships,
        EnsureAccountProfilesForUser ensureProfiles,
        InitializePhase16PersonalUtangSeed personalUtangSeed,
        ILogger<InitializePhase16AccountSeed> logger)
    {
        _environment = environment;
        _livePreview = livePreview.Value;
        _createUser = createUser;
        _users = users;
        _setPassword = setPassword;
        _assignPlatformRole = assignPlatformRole;
        _createOrg = createOrg;
        _organizations = organizations;
        _addMembership = addMembership;
        _memberships = memberships;
        _ensureProfiles = ensureProfiles;
        _personalUtangSeed = personalUtangSeed;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (_environment.IsProduction())
        {
            throw new InvalidOperationException("Phase 16 account seed must never run in Production.");
        }

        var testing = _environment.IsEnvironment("Testing");
        if (!_livePreview.Enabled && !testing && !_environment.IsDevelopment())
        {
            return;
        }

        if (_livePreview.Enabled && string.IsNullOrWhiteSpace(_livePreview.SharedPassword))
        {
            throw new InvalidOperationException("LivePreview:SharedPassword is required to seed Phase 16 accounts.");
        }

        var password = !string.IsNullOrWhiteSpace(_livePreview.SharedPassword)
            ? _livePreview.SharedPassword
            : "Phase16-Test-Only-Password!";

        _logger.LogInformation("Phase 16 account seed beginning (non-Production).");

        var org = await EnsureOrganizationAsync(cancellationToken).ConfigureAwait(false);

        await EnsurePlatformAdminAsync(
            "platform.admin1",
            "Platform Admin One",
            "platform.admin1@exits.test",
            password,
            cancellationToken).ConfigureAwait(false);
        await EnsurePlatformAdminAsync(
            "platform.admin2",
            "Platform Admin Two",
            "platform.admin2@exits.test",
            password,
            cancellationToken).ConfigureAwait(false);

        await EnsurePersonalUserAsync(
            "personal.user1",
            "Personal User One",
            "personal.user1@exits.test",
            password,
            cancellationToken).ConfigureAwait(false);
        await EnsurePersonalUserAsync(
            "personal.user2",
            "Personal User Two",
            "personal.user2@exits.test",
            password,
            cancellationToken).ConfigureAwait(false);

        await EnsureOrganizationMemberAsync(
            "org.seed.owner",
            "Phase16 Seed Org Owner",
            "org.seed.owner@exits.test",
            password,
            org.Id,
            OrganizationRole.OrganizationOwner,
            cancellationToken).ConfigureAwait(false);

        await _personalUtangSeed.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Phase 16 account seed finished.");
    }

    private async Task<PlatformOrganization> EnsureOrganizationAsync(CancellationToken cancellationToken)
    {
        var existing = await _organizations.GetBySlugAsync(SeedOrgSlug, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var created = await _createOrg.ExecuteAsync(SeedOrgDisplayName, SeedOrgSlug, cancellationToken)
            .ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
        {
            throw new InvalidOperationException(
                created.ErrorMessage ?? "Unable to create Phase 16 seed organization.");
        }

        return created.Value;
    }

    private async Task EnsurePlatformAdminAsync(
        string username,
        string displayName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await EnsureUserAsync(username, displayName, email, password, cancellationToken)
            .ConfigureAwait(false);
        var roleResult = await _assignPlatformRole.ExecuteAsync(
            user.Id.Value,
            PlatformSystemRole.PlatformAdministrator,
            organizationId: null,
            actorIdentifier: "phase16-seed",
            actorType: AuditActorType.System,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (!roleResult.IsSuccess && roleResult.ErrorCode != ApplicationErrorCodes.RoleAssignmentConflict)
        {
            throw new InvalidOperationException(roleResult.ErrorMessage ?? "Unable to assign Platform Administrator.");
        }

        await _ensureProfiles.ExecuteAsync(user.Id, AccountClass.Platform, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task EnsurePersonalUserAsync(
        string username,
        string displayName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var user = await EnsureUserAsync(username, displayName, email, password, cancellationToken)
            .ConfigureAwait(false);
        await _ensureProfiles.ExecuteAsync(user.Id, AccountClass.Personal, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task EnsureOrganizationMemberAsync(
        string username,
        string displayName,
        string email,
        string password,
        PlatformOrganizationId organizationId,
        OrganizationRole role,
        CancellationToken cancellationToken)
    {
        var user = await EnsureUserAsync(username, displayName, email, password, cancellationToken)
            .ConfigureAwait(false);

        var current = await _memberships
            .FindCurrentByUserAndOrganizationAsync(user.Id, organizationId, cancellationToken)
            .ConfigureAwait(false);
        if (current is null)
        {
            var added = await _addMembership.ExecuteAsync(
                organizationId,
                user.Id,
                role,
                cancellationToken).ConfigureAwait(false);
            if (!added.IsSuccess && added.ErrorCode != ApplicationErrorCodes.MembershipConflict)
            {
                throw new InvalidOperationException(
                    added.ErrorMessage ?? "Unable to add Phase 16 seed membership.");
            }
        }

        await _ensureProfiles.ExecuteAsync(user.Id, AccountClass.Organization, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<PlatformUser> EnsureUserAsync(
        string username,
        string displayName,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        var (_, normalized) = PlatformUser.NormalizeUsername(username);
        var existing = await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            await _setPassword.ExecuteAsync(existing.Id.Value, password, cancellationToken)
                .ConfigureAwait(false);
            return existing;
        }

        var created = await _createUser.ExecuteAsync(username, displayName, email, cancellationToken)
            .ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
        {
            // Email/username race: re-read.
            existing = await _users.GetByNormalizedUsernameAsync(normalized, cancellationToken)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                await _setPassword.ExecuteAsync(existing.Id.Value, password, cancellationToken)
                    .ConfigureAwait(false);
                return existing;
            }

            throw new InvalidOperationException(created.ErrorMessage ?? $"Unable to create seed user {username}.");
        }

        var passwordResult = await _setPassword.ExecuteAsync(created.Value.Id.Value, password, cancellationToken)
            .ConfigureAwait(false);
        if (!passwordResult.IsSuccess)
        {
            throw new InvalidOperationException(passwordResult.ErrorMessage ?? $"Unable to set password for {username}.");
        }

        return created.Value;
    }
}
