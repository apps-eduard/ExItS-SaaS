using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Personal;
using ExItS.Platform.Domain.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.LocalValidation;

public sealed class InitializeLocalValidationDataset
{
    private readonly LocalValidationOptions _options;
    private readonly CreatePlatformOrganization _createOrg;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly CreateProduct _createProduct;
    private readonly IProductRepository _products;
    private readonly EnsureMvpPosPlans _ensureMvpPosPlans;
    private readonly CreateTrialDefinition _createTrial;
    private readonly StartTrialSubscription _startTrial;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly CreatePlatformUser _createUser;
    private readonly IPlatformUserRepository _users;
    private readonly SetPlatformUserPassword _setPassword;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly IAccountProfileRepository _profiles;
    private readonly AssignPlatformRole _assignPlatformRole;
    private readonly RevokePlatformRole _revokePlatformRole;
    private readonly IPlatformRoleAssignmentRepository _roleAssignments;
    private readonly AddOrganizationMembership _addMembership;
    private readonly ChangeOrganizationRole _changeMembershipRole;
    private readonly RevokeOrganizationMembership _revokeMembership;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly GrantProductAccess _grantProductAccess;
    private readonly RevokeProductAccess _revokeProductAccess;
    private readonly IProductAccessAssignmentRepository _accessAssignments;
    private readonly DeactivatePlatformUser _deactivateUser;
    private readonly IPersonalContactRepository _contacts;
    private readonly IPersonalDebtRelationshipRepository _relationships;
    private readonly InitializeLocalValidationPersonalUtangSeed _personalUtangSeed;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly ILogger<InitializeLocalValidationDataset> _logger;

    public InitializeLocalValidationDataset(
        IOptions<LocalValidationOptions> options,
        CreatePlatformOrganization createOrg,
        IPlatformOrganizationRepository organizations,
        CreateProduct createProduct,
        IProductRepository products,
        EnsureMvpPosPlans ensureMvpPosPlans,
        CreateTrialDefinition createTrial,
        StartTrialSubscription startTrial,
        GenerateEntitlementSnapshot generateSnapshot,
        CreatePlatformUser createUser,
        IPlatformUserRepository users,
        SetPlatformUserPassword setPassword,
        EnsureAccountProfilesForUser ensureProfiles,
        IAccountProfileRepository profiles,
        AssignPlatformRole assignPlatformRole,
        RevokePlatformRole revokePlatformRole,
        IPlatformRoleAssignmentRepository roleAssignments,
        AddOrganizationMembership addMembership,
        ChangeOrganizationRole changeMembershipRole,
        RevokeOrganizationMembership revokeMembership,
        IOrganizationMembershipRepository memberships,
        GrantProductAccess grantProductAccess,
        RevokeProductAccess revokeProductAccess,
        IProductAccessAssignmentRepository accessAssignments,
        DeactivatePlatformUser deactivateUser,
        IPersonalContactRepository contacts,
        IPersonalDebtRelationshipRepository relationships,
        InitializeLocalValidationPersonalUtangSeed personalUtangSeed,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        ISubscriptionRepository subscriptions,
        IPlatformUnitOfWork unitOfWork,
        IClock clock,
        ILogger<InitializeLocalValidationDataset> logger)
    {
        _options = options.Value;
        _createOrg = createOrg;
        _organizations = organizations;
        _createProduct = createProduct;
        _products = products;
        _ensureMvpPosPlans = ensureMvpPosPlans;
        _createTrial = createTrial;
        _startTrial = startTrial;
        _generateSnapshot = generateSnapshot;
        _createUser = createUser;
        _users = users;
        _setPassword = setPassword;
        _ensureProfiles = ensureProfiles;
        _profiles = profiles;
        _assignPlatformRole = assignPlatformRole;
        _revokePlatformRole = revokePlatformRole;
        _roleAssignments = roleAssignments;
        _addMembership = addMembership;
        _changeMembershipRole = changeMembershipRole;
        _revokeMembership = revokeMembership;
        _memberships = memberships;
        _grantProductAccess = grantProductAccess;
        _revokeProductAccess = revokeProductAccess;
        _accessAssignments = accessAssignments;
        _deactivateUser = deactivateUser;
        _contacts = contacts;
        _relationships = relationships;
        _personalUtangSeed = personalUtangSeed;
        _plans = plans;
        _trials = trials;
        _subscriptions = subscriptions;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.SharedPassword) || _options.SharedPassword.Length < 12)
        {
            throw new InvalidOperationException(
                "LocalValidation:SharedPassword must be configured (minimum 12 characters) when LocalValidation:Enabled=true.");
        }

        _logger.LogInformation(
            "Local validation dataset initialization starting (version {DatasetVersion}).",
            LocalValidationOptions.DatasetVersion);

        await CleanupObsoleteSeedAsync(cancellationToken).ConfigureAwait(false);

        var productCode = ProductCode.PinoyBusinessPos;
        var organizations = new Dictionary<string, PlatformOrganization>(StringComparer.OrdinalIgnoreCase);
        foreach (var orgDef in LocalValidationOrganizationCatalog.All)
        {
            var organization = await EnsureOrganizationAsync(orgDef, cancellationToken).ConfigureAwait(false);
            organizations[orgDef.Slug] = organization;
            await EnsureCatalogAndCommercialAsync(organization.Id, productCode, cancellationToken).ConfigureAwait(false);
        }

        foreach (var identity in LocalValidationIdentityCatalog.All)
        {
            var user = await EnsureUserAsync(identity, cancellationToken).ConfigureAwait(false);
            await EnsurePasswordAsync(user.Id.Value, cancellationToken).ConfigureAwait(false);

            await ReconcilePlatformRolesAsync(user.Id, identity, cancellationToken).ConfigureAwait(false);
            await ReconcileOrganizationAccessAsync(user.Id, identity, organizations, productCode, cancellationToken)
                .ConfigureAwait(false);

            await _ensureProfiles
                .ExecuteAsync(
                    user.Id,
                    identity.PreferredAccountClass,
                    exclusivePreferredClass: true,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        await CloseObsoleteOrganizationsAsync(cancellationToken).ConfigureAwait(false);
        await _personalUtangSeed.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Local validation dataset initialization completed (version {DatasetVersion}).",
            LocalValidationOptions.DatasetVersion);
    }

    private async Task CleanupObsoleteSeedAsync(CancellationToken ct)
    {
        var seen = new HashSet<Guid>();
        foreach (var email in ObsoletePhase16SeedIdentities.NormalizedEmails)
        {
            var user = await _users.GetByNormalizedEmailAsync(email, ct).ConfigureAwait(false);
            if (user is not null && seen.Add(user.Id.Value))
            {
                await DecommissionObsoleteUserAsync(user, ct).ConfigureAwait(false);
            }
        }

        foreach (var username in ObsoletePhase16SeedIdentities.NormalizedUsernames)
        {
            var user = await _users.GetByNormalizedUsernameAsync(username, ct).ConfigureAwait(false);
            if (user is not null && seen.Add(user.Id.Value))
            {
                await DecommissionObsoleteUserAsync(user, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task CloseObsoleteOrganizationsAsync(CancellationToken ct)
    {
        foreach (var slug in ObsoleteLocalValidationOrganizations.Slugs.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (LocalValidationOrganizationCatalog.FindBySlug(slug) is not null)
            {
                continue;
            }

            var org = await _organizations.GetBySlugAsync(slug, ct).ConfigureAwait(false);
            if (org is null)
            {
                continue;
            }

            var (memberships, _) = await _memberships
                .ListByOrganizationAsync(org.Id, MembershipStatus.Active, skip: 0, take: 500, ct)
                .ConfigureAwait(false);
            foreach (var membership in memberships)
            {
                await _revokeMembership
                    .ExecuteAsync(
                        membership.Id,
                        reason: "obsolete local-validation organization cleanup",
                        actorReference: LocalValidationOptions.Actor,
                        cancellationToken: ct)
                    .ConfigureAwait(false);
            }

            if (org.Status == OrganizationStatus.Active || org.Status == OrganizationStatus.Suspended)
            {
                org.Close(_clock.UtcNow);
                await _organizations.UpdateAsync(org, ct).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
                _logger.LogInformation("Closed obsolete Local Validation organization '{Slug}'.", slug);
            }
        }
    }

    private async Task DecommissionObsoleteUserAsync(PlatformUser user, CancellationToken ct)
    {
        _logger.LogInformation(
            "Removing obsolete Phase16 seed identity {Username} ({Email}).",
            user.Username,
            user.NormalizedEmail);

        var utcNow = _clock.UtcNow;
        foreach (var profile in await _profiles.ListByUserAsync(user.Id, ct).ConfigureAwait(false))
        {
            if (!profile.IsActive)
            {
                continue;
            }

            profile.Deactivate(utcNow);
            await _profiles.UpdateAsync(profile, ct).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var roles = await _roleAssignments.ListActiveByUserAsync(user.Id, ct).ConfigureAwait(false);
        foreach (var role in roles)
        {
            var revoke = await _revokePlatformRole
                .ExecuteAsync(
                    role.Id.Value,
                    LocalValidationOptions.Actor,
                    AuditActorType.System,
                    reason: "obsolete phase16 seed cleanup",
                    cancellationToken: ct)
                .ConfigureAwait(false);
            if (!revoke.IsSuccess
                && revoke.ErrorCode != ApplicationErrorCodes.LastPlatformAdministratorProtected)
            {
                _logger.LogWarning(
                    "Could not revoke platform role {Role} for obsolete user {UserId}: {Code}",
                    role.Role,
                    user.Id.Value,
                    revoke.ErrorCode);
            }
        }

        var (memberships, _) = await _memberships
            .ListByUserAsync(user.Id, MembershipStatus.Active, skip: 0, take: 200, ct)
            .ConfigureAwait(false);
        foreach (var membership in memberships)
        {
            await _revokeMembership
                .ExecuteAsync(membership.Id, reason: "obsolete phase16 seed cleanup", actorReference: LocalValidationOptions.Actor, cancellationToken: ct)
                .ConfigureAwait(false);
        }

        var (accessItems, _) = await _accessAssignments
            .ListByUserAsync(user.Id, ProductAccessStatus.Active, skip: 0, take: 200, ct)
            .ConfigureAwait(false);
        foreach (var access in accessItems)
        {
            await _revokeProductAccess
                .ExecuteAsync(access.Id, LocalValidationOptions.Actor, "obsolete phase16 seed cleanup", ct)
                .ConfigureAwait(false);
        }

        var contacts = await _contacts.ListByOwnerAsync(user.Id, ct).ConfigureAwait(false);
        foreach (var contact in contacts.Where(c => c.Status != PersonalContactStatus.Archived))
        {
            contact.Archive(utcNow);
            await _contacts.UpdateAsync(contact, ct).ConfigureAwait(false);
        }

        var relationships = await _relationships.ListForUserAsync(user.Id, ct).ConfigureAwait(false);
        foreach (var relationship in relationships.Where(r => r.Status != PersonalDebtRelationshipStatus.Archived))
        {
            relationship.Archive(utcNow);
            await _relationships.UpdateAsync(relationship, ct).ConfigureAwait(false);
        }

        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        if (user.Status != AccountStatus.Deactivated)
        {
            var deactivated = await _deactivateUser
                .ExecuteAsync(user.Id, "Obsolete Local Validation identity cleanup", cancellationToken: ct)
                .ConfigureAwait(false);
            if (!deactivated.IsSuccess)
            {
                _logger.LogWarning(
                    "Could not deactivate obsolete user {UserId}: {Code}",
                    user.Id.Value,
                    deactivated.ErrorCode);
            }
        }
    }

    private async Task ReconcilePlatformRolesAsync(
        PlatformUserId userId,
        LocalValidationIdentityDefinition identity,
        CancellationToken ct)
    {
        var active = await _roleAssignments.ListActiveByUserAsync(userId, ct).ConfigureAwait(false);
        var desired = identity.AssignPlatformRole;

        foreach (var assignment in active)
        {
            if (desired is PlatformSystemRole wanted && assignment.Role == wanted && assignment.OrganizationId is null)
            {
                continue;
            }

            var revoke = await _revokePlatformRole
                .ExecuteAsync(
                    assignment.Id.Value,
                    LocalValidationOptions.Actor,
                    AuditActorType.System,
                    reason: "local-validation single-scope reconcile",
                    cancellationToken: ct)
                .ConfigureAwait(false);
            if (!revoke.IsSuccess
                && revoke.ErrorCode != ApplicationErrorCodes.LastPlatformAdministratorProtected)
            {
                throw new InvalidOperationException(
                    $"Local validation platform role revoke failed: {revoke.ErrorCode} {revoke.ErrorMessage}");
            }
        }

        if (desired is PlatformSystemRole role)
        {
            await EnsurePlatformRoleAsync(userId.Value, role, ct).ConfigureAwait(false);
        }
    }

    private async Task ReconcileOrganizationAccessAsync(
        PlatformUserId userId,
        LocalValidationIdentityDefinition identity,
        IReadOnlyDictionary<string, PlatformOrganization> organizations,
        string productCode,
        CancellationToken ct)
    {
        var (memberships, _) = await _memberships
            .ListByUserAsync(userId, MembershipStatus.Active, skip: 0, take: 200, ct)
            .ConfigureAwait(false);

        Guid? desiredOrgId = null;
        OrganizationRole? desiredRole = null;
        if (identity.HasOrganizationMembership
            && identity.OrganizationRole is not null
            && !string.IsNullOrWhiteSpace(identity.OrganizationSlug))
        {
            if (!organizations.TryGetValue(identity.OrganizationSlug, out var organization))
            {
                throw new InvalidOperationException(
                    $"Local validation identity '{identity.Key}' references unknown organization '{identity.OrganizationSlug}'.");
            }

            desiredOrgId = organization.Id.Value;
            desiredRole = identity.OrganizationRole.Value switch
            {
                OrganizationMembershipValidationRole.OrganizationOwner => OrganizationRole.OrganizationOwner,
                OrganizationMembershipValidationRole.OrganizationAdministrator => OrganizationRole.OrganizationAdministrator,
                _ => OrganizationRole.OrganizationMember
            };
        }

        foreach (var membership in memberships)
        {
            if (desiredOrgId is Guid orgId && membership.OrganizationId.Value == orgId)
            {
                if (desiredRole is OrganizationRole role && membership.Role != role)
                {
                    var changed = await _changeMembershipRole
                        .ExecuteAsync(membership.Id, role, LocalValidationOptions.Actor, cancellationToken: ct)
                        .ConfigureAwait(false);
                    if (!changed.IsSuccess)
                    {
                        throw new InvalidOperationException(
                            $"Local validation membership role change failed: {changed.ErrorCode} {changed.ErrorMessage}");
                    }
                }

                continue;
            }

            var revoked = await _revokeMembership
                .ExecuteAsync(
                    membership.Id,
                    reason: "local-validation single-scope reconcile",
                    actorReference: LocalValidationOptions.Actor,
                    cancellationToken: ct)
                .ConfigureAwait(false);
            if (!revoked.IsSuccess)
            {
                throw new InvalidOperationException(
                    $"Local validation membership revoke failed: {revoked.ErrorCode} {revoked.ErrorMessage}");
            }
        }

        if (desiredOrgId is Guid ensureOrgId && desiredRole is OrganizationRole ensureRole)
        {
            var org = organizations.Values.First(o => o.Id.Value == ensureOrgId);
            await EnsureMembershipAsync(org.Id, userId, ensureRole, ct).ConfigureAwait(false);

            if (identity.GrantPosProductAccess)
            {
                await EnsureProductAccessAsync(org.Id, userId, productCode, ct).ConfigureAwait(false);
            }
            else
            {
                var access = await _accessAssignments
                    .FindActiveByUserOrganizationProductAsync(userId, org.Id, ProductCode.Create(productCode), ct)
                    .ConfigureAwait(false);
                if (access is not null)
                {
                    await _revokeProductAccess
                        .ExecuteAsync(access.Id, LocalValidationOptions.Actor, "local-validation single-scope reconcile", ct)
                        .ConfigureAwait(false);
                }
            }
        }
        else
        {
            var (accessItems, _) = await _accessAssignments
                .ListByUserAsync(userId, ProductAccessStatus.Active, skip: 0, take: 200, ct)
                .ConfigureAwait(false);
            foreach (var access in accessItems)
            {
                await _revokeProductAccess
                    .ExecuteAsync(access.Id, LocalValidationOptions.Actor, "local-validation single-scope reconcile", ct)
                    .ConfigureAwait(false);
            }
        }
    }

    private async Task<PlatformOrganization> EnsureOrganizationAsync(
        LocalValidationOrganizationDefinition orgDef,
        CancellationToken ct)
    {
        var existing = await _organizations.GetBySlugAsync(orgDef.Slug, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var created = await _createOrg
            .ExecuteAsync(orgDef.DisplayName, orgDef.Slug, ct)
            .ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
        {
            existing = await _organizations.GetBySlugAsync(orgDef.Slug, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                return existing;
            }

            throw new InvalidOperationException(
                $"Local validation organization '{orgDef.Slug}' create failed: {created.ErrorCode} {created.ErrorMessage}");
        }

        return created.Value;
    }

    private async Task EnsureCatalogAndCommercialAsync(
        PlatformOrganizationId organizationId,
        string productCode,
        CancellationToken ct)
    {
        const string productDisplayName = "Pinoy Business POS";
        var product = await _products.GetByCodeAsync(ProductCode.Create(productCode), ct).ConfigureAwait(false);
        if (product is null)
        {
            var created = await _createProduct.ExecuteAsync(productCode, productDisplayName, ct).ConfigureAwait(false);
            if (!created.IsSuccess)
            {
                product = await _products.GetByCodeAsync(ProductCode.Create(productCode), ct).ConfigureAwait(false);
                if (product is null)
                {
                    throw new InvalidOperationException(
                        $"Local validation product create failed: {created.ErrorCode} {created.ErrorMessage}");
                }
            }
            else
            {
                product = created.Value;
            }
        }

        if (product is not null
            && !string.Equals(product.DisplayName, productDisplayName, StringComparison.Ordinal))
        {
            product.Rename(productDisplayName, _clock.UtcNow);
            await _products.UpdateAsync(product, ct).ConfigureAwait(false);
            await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        await _ensureMvpPosPlans.ExecuteAsync(ct).ConfigureAwait(false);

        var businessSpec = MvpPosPlanCatalog.Plans.First(p =>
            string.Equals(p.PlanKey, MvpPosPlanCodes.Business, StringComparison.Ordinal));
        var planCode = PlanCode.Create(MvpPosPlanCodes.Business);
        var plan = await _plans
            .GetByProductAndCodeAsync(ProductCode.Create(productCode), planCode, ct)
            .ConfigureAwait(false);
        if (plan is null || plan.Status != PlanStatus.Active)
        {
            throw new InvalidOperationException(
                $"MVP Business plan '{MvpPosPlanCodes.Business}' was not available after EnsureMvpPosPlans.");
        }

        var versions = await _plans.ListVersionsAsync(plan.Id, ct).ConfigureAwait(false);
        var version = versions.FirstOrDefault(v =>
            v.VersionNumber == 1 && v.Status == PlanVersionStatus.Published);
        if (version is null)
        {
            throw new InvalidOperationException(
                $"Published Business plan version 1 was not available after EnsureMvpPosPlans.");
        }

        var trials = await _trials.ListByProductAsync(ProductCode.Create(productCode), ct).ConfigureAwait(false);
        var trial = trials.FirstOrDefault(t => string.Equals(t.DisplayName, LocalValidationOptions.TrialDisplayName, StringComparison.Ordinal));
        if (trial is null)
        {
            var grants = EnsureMvpPosPlans.BuildGrants(businessSpec);
            var createdTrial = await _createTrial
                .ExecuteAsync(
                    productCode,
                    LocalValidationOptions.TrialDisplayName,
                    TimeSpan.FromDays(30),
                    grants,
                    Array.Empty<FeatureGrantSpec>(),
                    planId: null,
                    cancellationToken: ct)
                .ConfigureAwait(false);
            if (!createdTrial.IsSuccess || createdTrial.Value is null)
            {
                throw new InvalidOperationException(
                    $"Local validation trial create failed: {createdTrial.ErrorCode} {createdTrial.ErrorMessage}");
            }

            trial = createdTrial.Value;
        }

        var subscription = await _subscriptions
            .GetCurrentForOrganizationProductAsync(organizationId, ProductCode.Create(productCode), ct)
            .ConfigureAwait(false);
        if (subscription is null)
        {
            var started = await _startTrial
                .ExecuteAsync(organizationId, plan.Id, version.Id, trial.Id, ct)
                .ConfigureAwait(false);
            if (!started.IsSuccess || started.Value is null)
            {
                throw new InvalidOperationException(
                    $"Local validation trial subscription failed: {started.ErrorCode} {started.ErrorMessage}");
            }

            subscription = started.Value;
        }

        var snapshot = await _generateSnapshot
            .ExecuteAsync(organizationId, ProductCode.Create(productCode), cancellationToken: ct)
            .ConfigureAwait(false);
        if (!snapshot.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Local validation entitlement snapshot failed: {snapshot.ErrorCode} {snapshot.ErrorMessage}");
        }
    }

    private async Task<PlatformUser> EnsureUserAsync(LocalValidationIdentityDefinition identity, CancellationToken ct)
    {
        var (_, normalized) = PlatformUser.NormalizeUsername(identity.Username);
        var existing = await _users.GetByNormalizedUsernameAsync(normalized, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var created = await _createUser
            .ExecuteAsync(identity.Username, identity.DisplayName, identity.Email, cancellationToken: ct)
            .ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
        {
            existing = await _users.GetByNormalizedUsernameAsync(normalized, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                return existing;
            }

            throw new InvalidOperationException(
                $"Local validation user '{identity.Username}' create failed: {created.ErrorCode} {created.ErrorMessage}");
        }

        return created.Value;
    }

    private async Task EnsurePasswordAsync(Guid userId, CancellationToken ct)
    {
        var result = await _setPassword.ExecuteAsync(userId, _options.SharedPassword, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Local validation password set failed for {userId:D}: {result.ErrorCode} {result.ErrorMessage}");
        }
    }

    private async Task EnsurePlatformRoleAsync(Guid userId, PlatformSystemRole role, CancellationToken ct)
    {
        var result = await _assignPlatformRole.ExecuteAsync(
            userId,
            role,
            organizationId: null,
            actorIdentifier: LocalValidationOptions.Actor,
            actorType: AuditActorType.System,
            reason: "local-validation dataset",
            cancellationToken: ct).ConfigureAwait(false);

        if (!result.IsSuccess && result.ErrorCode != ApplicationErrorCodes.RoleAssignmentConflict)
        {
            throw new InvalidOperationException(
                $"Local validation {role} assign failed: {result.ErrorCode} {result.ErrorMessage}");
        }
    }

    private async Task EnsureMembershipAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        OrganizationRole role,
        CancellationToken ct)
    {
        var existing = await _memberships
            .FindActiveByUserAndOrganizationAsync(userId, organizationId, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            if (existing.Role != role)
            {
                var changed = await _changeMembershipRole
                    .ExecuteAsync(existing.Id, role, LocalValidationOptions.Actor, cancellationToken: ct)
                    .ConfigureAwait(false);
                if (!changed.IsSuccess)
                {
                    throw new InvalidOperationException(
                        $"Local validation membership role change failed: {changed.ErrorCode} {changed.ErrorMessage}");
                }
            }

            return;
        }

        var result = await _addMembership.ExecuteAsync(organizationId, userId, role, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            existing = await _memberships
                .FindActiveByUserAndOrganizationAsync(userId, organizationId, ct)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Local validation membership failed: {result.ErrorCode} {result.ErrorMessage}");
        }
    }

    private async Task EnsureProductAccessAsync(
        PlatformOrganizationId organizationId,
        PlatformUserId userId,
        string productCode,
        CancellationToken ct)
    {
        var existing = await _accessAssignments
            .FindActiveByUserOrganizationProductAsync(userId, organizationId, ProductCode.Create(productCode), ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            return;
        }

        var result = await _grantProductAccess
            .ExecuteAsync(organizationId, userId, productCode, LocalValidationOptions.Actor, "local-validation dataset", ct)
            .ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            existing = await _accessAssignments
                .FindActiveByUserOrganizationProductAsync(userId, organizationId, ProductCode.Create(productCode), ct)
                .ConfigureAwait(false);
            if (existing is not null)
            {
                return;
            }

            throw new InvalidOperationException(
                $"Local validation product access grant failed: {result.ErrorCode} {result.ErrorMessage}");
        }
    }
}
