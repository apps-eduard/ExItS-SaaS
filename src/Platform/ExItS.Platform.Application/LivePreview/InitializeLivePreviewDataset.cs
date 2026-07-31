using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Application.LivePreview;

public sealed class InitializeLivePreviewDataset
{
    private static readonly string[] PreviewFeatureCodes =
    [
        FeatureCode.CustomerCreditView,
        FeatureCode.CustomerCreditRepay,
        FeatureCode.CustomerCreditCreate,
        FeatureCode.StoreCatalogView,
        FeatureCode.StoreCatalogManage,
        FeatureCode.StoreSalesView,
        FeatureCode.StoreSalesCreate,
        FeatureCode.StoreDashboardView,
        FeatureCode.StoreReportsView,
        FeatureCode.StorePermissionsView,
        FeatureCode.StorePermissionsManage
    ];

    private readonly LivePreviewOptions _options;
    private readonly CreatePlatformOrganization _createOrg;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly CreateProduct _createProduct;
    private readonly IProductRepository _products;
    private readonly CreateFeatureDefinition _createFeature;
    private readonly CreatePlan _createPlan;
    private readonly ActivatePlan _activatePlan;
    private readonly CreateDraftPlanVersion _createDraftVersion;
    private readonly PublishExistingPlanVersion _publishVersion;
    private readonly CreateTrialDefinition _createTrial;
    private readonly StartTrialSubscription _startTrial;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly CreatePlatformUser _createUser;
    private readonly IPlatformUserRepository _users;
    private readonly SetPlatformUserPassword _setPassword;
    private readonly AssignPlatformRole _assignPlatformRole;
    private readonly AddOrganizationMembership _addMembership;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly GrantProductAccess _grantProductAccess;
    private readonly IProductAccessAssignmentRepository _accessAssignments;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly ILogger<InitializeLivePreviewDataset> _logger;

    public InitializeLivePreviewDataset(
        IOptions<LivePreviewOptions> options,
        CreatePlatformOrganization createOrg,
        IPlatformOrganizationRepository organizations,
        CreateProduct createProduct,
        IProductRepository products,
        CreateFeatureDefinition createFeature,
        CreatePlan createPlan,
        ActivatePlan activatePlan,
        CreateDraftPlanVersion createDraftVersion,
        PublishExistingPlanVersion publishVersion,
        CreateTrialDefinition createTrial,
        StartTrialSubscription startTrial,
        GenerateEntitlementSnapshot generateSnapshot,
        CreatePlatformUser createUser,
        IPlatformUserRepository users,
        SetPlatformUserPassword setPassword,
        AssignPlatformRole assignPlatformRole,
        AddOrganizationMembership addMembership,
        IOrganizationMembershipRepository memberships,
        GrantProductAccess grantProductAccess,
        IProductAccessAssignmentRepository accessAssignments,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        ISubscriptionRepository subscriptions,
        ILogger<InitializeLivePreviewDataset> logger)
    {
        _options = options.Value;
        _createOrg = createOrg;
        _organizations = organizations;
        _createProduct = createProduct;
        _products = products;
        _createFeature = createFeature;
        _createPlan = createPlan;
        _activatePlan = activatePlan;
        _createDraftVersion = createDraftVersion;
        _publishVersion = publishVersion;
        _createTrial = createTrial;
        _startTrial = startTrial;
        _generateSnapshot = generateSnapshot;
        _createUser = createUser;
        _users = users;
        _setPassword = setPassword;
        _assignPlatformRole = assignPlatformRole;
        _addMembership = addMembership;
        _memberships = memberships;
        _grantProductAccess = grantProductAccess;
        _accessAssignments = accessAssignments;
        _plans = plans;
        _trials = trials;
        _subscriptions = subscriptions;
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
                "LivePreview:SharedPassword must be configured (minimum 12 characters) when LivePreview:Enabled=true.");
        }

        _logger.LogInformation("Live preview dataset initialization starting.");

        var organization = await EnsureOrganizationAsync(cancellationToken).ConfigureAwait(false);
        var productCode = ProductCode.PinoyBusinessPos;
        await EnsureCatalogAndCommercialAsync(organization.Id, productCode, cancellationToken).ConfigureAwait(false);

        foreach (var identity in LivePreviewIdentityCatalog.All)
        {
            var user = await EnsureUserAsync(identity, cancellationToken).ConfigureAwait(false);
            await EnsurePasswordAsync(user.Id.Value, cancellationToken).ConfigureAwait(false);

            if (identity.AssignPlatformAdministrator)
            {
                await EnsurePlatformAdminAsync(user.Id.Value, cancellationToken).ConfigureAwait(false);
            }

            if (identity.HasOrganizationMembership && identity.OrganizationRole is not null)
            {
                var role = identity.OrganizationRole.Value switch
                {
                    OrganizationMembershipPreviewRole.OrganizationAdministrator => OrganizationRole.OrganizationAdministrator,
                    _ => OrganizationRole.OrganizationMember
                };
                await EnsureMembershipAsync(organization.Id, user.Id, role, cancellationToken).ConfigureAwait(false);
            }

            if (identity.GrantPosProductAccess)
            {
                await EnsureProductAccessAsync(organization.Id, user.Id, productCode, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        _logger.LogInformation("Live preview dataset initialization completed.");
    }

    private async Task<PlatformOrganization> EnsureOrganizationAsync(CancellationToken ct)
    {
        var existing = await _organizations.GetBySlugAsync(LivePreviewOptions.OrgSlug, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var created = await _createOrg
            .ExecuteAsync(LivePreviewOptions.OrgDisplayName, LivePreviewOptions.OrgSlug, ct)
            .ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
        {
            // Race: reload by slug
            existing = await _organizations.GetBySlugAsync(LivePreviewOptions.OrgSlug, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                return existing;
            }

            throw new InvalidOperationException(
                $"Live preview organization create failed: {created.ErrorCode} {created.ErrorMessage}");
        }

        return created.Value;
    }

    private async Task EnsureCatalogAndCommercialAsync(
        PlatformOrganizationId organizationId,
        string productCode,
        CancellationToken ct)
    {
        var product = await _products.GetByCodeAsync(ProductCode.Create(productCode), ct).ConfigureAwait(false);
        if (product is null)
        {
            var created = await _createProduct.ExecuteAsync(productCode, "PinoyBusinessPOS", ct).ConfigureAwait(false);
            if (!created.IsSuccess)
            {
                product = await _products.GetByCodeAsync(ProductCode.Create(productCode), ct).ConfigureAwait(false);
                if (product is null)
                {
                    throw new InvalidOperationException(
                        $"Live preview product create failed: {created.ErrorCode} {created.ErrorMessage}");
                }
            }
        }

        foreach (var featureCode in PreviewFeatureCodes)
        {
            var featureResult = await _createFeature
                .ExecuteAsync(productCode, featureCode, featureCode, FeatureValueType.Boolean, ct)
                .ConfigureAwait(false);
            if (!featureResult.IsSuccess
                && featureResult.ErrorCode != ApplicationErrorCodes.DuplicateFeatureCode)
            {
                throw new InvalidOperationException(
                    $"Live preview feature '{featureCode}' failed: {featureResult.ErrorCode} {featureResult.ErrorMessage}");
            }
        }

        var planCode = PlanCode.Create(LivePreviewOptions.ProductPlanCode);
        var plan = await _plans
            .GetByProductAndCodeAsync(ProductCode.Create(productCode), planCode, ct)
            .ConfigureAwait(false);
        if (plan is null)
        {
            var createdPlan = await _createPlan
                .ExecuteAsync(productCode, LivePreviewOptions.ProductPlanCode, LivePreviewOptions.ProductPlanDisplayName, ct)
                .ConfigureAwait(false);
            if (!createdPlan.IsSuccess || createdPlan.Value is null)
            {
                plan = await _plans
                    .GetByProductAndCodeAsync(ProductCode.Create(productCode), planCode, ct)
                    .ConfigureAwait(false);
                if (plan is null)
                {
                    throw new InvalidOperationException(
                        $"Live preview plan create failed: {createdPlan.ErrorCode} {createdPlan.ErrorMessage}");
                }
            }
            else
            {
                plan = createdPlan.Value;
            }
        }

        if (plan.Status != PlanStatus.Active)
        {
            var activate = await _activatePlan.ExecuteAsync(plan.Id, ct).ConfigureAwait(false);
            if (!activate.IsSuccess)
            {
                var reloaded = await _plans.GetByIdAsync(plan.Id, ct).ConfigureAwait(false);
                if (reloaded?.Status != PlanStatus.Active)
                {
                    throw new InvalidOperationException(
                        $"Live preview plan activate failed: {activate.ErrorCode} {activate.ErrorMessage}");
                }

                plan = reloaded;
            }
            else if (activate.Value is not null)
            {
                plan = activate.Value;
            }
        }

        var versions = await _plans.ListVersionsAsync(plan.Id, ct).ConfigureAwait(false);
        var version = versions.FirstOrDefault(v => v.VersionNumber == 1);
        if (version is null)
        {
            var grants = PreviewFeatureCodes
                .Select(c => FeatureGrantSpec.Boolean(FeatureCode.Create(c), true))
                .ToArray();
            var draft = await _createDraftVersion
                .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, trialEligible: true, grants, cancellationToken: ct)
                .ConfigureAwait(false);
            if (!draft.IsSuccess || draft.Value is null)
            {
                throw new InvalidOperationException(
                    $"Live preview plan version draft failed: {draft.ErrorCode} {draft.ErrorMessage}");
            }

            var published = await _publishVersion.ExecuteAsync(plan.Id, 1, ct).ConfigureAwait(false);
            if (!published.IsSuccess || published.Value is null)
            {
                throw new InvalidOperationException(
                    $"Live preview plan version publish failed: {published.ErrorCode} {published.ErrorMessage}");
            }

            version = published.Value;
        }
        else if (version.Status != PlanVersionStatus.Published)
        {
            var published = await _publishVersion.ExecuteAsync(plan.Id, 1, ct).ConfigureAwait(false);
            if (!published.IsSuccess || published.Value is null)
            {
                throw new InvalidOperationException(
                    $"Live preview plan version publish failed: {published.ErrorCode} {published.ErrorMessage}");
            }

            version = published.Value;
        }

        var trials = await _trials.ListByProductAsync(ProductCode.Create(productCode), ct).ConfigureAwait(false);
        var trial = trials.FirstOrDefault(t => string.Equals(t.DisplayName, LivePreviewOptions.TrialDisplayName, StringComparison.Ordinal));
        if (trial is null)
        {
            var grants = PreviewFeatureCodes
                .Select(c => FeatureGrantSpec.Boolean(FeatureCode.Create(c), true))
                .ToArray();
            var createdTrial = await _createTrial
                .ExecuteAsync(
                    productCode,
                    LivePreviewOptions.TrialDisplayName,
                    TimeSpan.FromDays(30),
                    grants,
                    Array.Empty<FeatureGrantSpec>(),
                    planId: null,
                    cancellationToken: ct)
                .ConfigureAwait(false);
            if (!createdTrial.IsSuccess || createdTrial.Value is null)
            {
                throw new InvalidOperationException(
                    $"Live preview trial create failed: {createdTrial.ErrorCode} {createdTrial.ErrorMessage}");
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
                    $"Live preview trial subscription failed: {started.ErrorCode} {started.ErrorMessage}");
            }

            subscription = started.Value;
        }

        var snapshot = await _generateSnapshot
            .ExecuteAsync(organizationId, ProductCode.Create(productCode), cancellationToken: ct)
            .ConfigureAwait(false);
        if (!snapshot.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Live preview entitlement snapshot failed: {snapshot.ErrorCode} {snapshot.ErrorMessage}");
        }
    }

    private async Task<PlatformUser> EnsureUserAsync(LivePreviewIdentityDefinition identity, CancellationToken ct)
    {
        var (_, normalized) = PlatformUser.NormalizeUsername(identity.Username);
        var existing = await _users.GetByNormalizedUsernameAsync(normalized, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        var created = await _createUser
            .ExecuteAsync(identity.Username, identity.DisplayName, identity.Email, ct)
            .ConfigureAwait(false);
        if (!created.IsSuccess || created.Value is null)
        {
            existing = await _users.GetByNormalizedUsernameAsync(normalized, ct).ConfigureAwait(false);
            if (existing is not null)
            {
                return existing;
            }

            throw new InvalidOperationException(
                $"Live preview user '{identity.Username}' create failed: {created.ErrorCode} {created.ErrorMessage}");
        }

        return created.Value;
    }

    private async Task EnsurePasswordAsync(Guid userId, CancellationToken ct)
    {
        var result = await _setPassword.ExecuteAsync(userId, _options.SharedPassword, ct).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(
                $"Live preview password set failed for {userId:D}: {result.ErrorCode} {result.ErrorMessage}");
        }
    }

    private async Task EnsurePlatformAdminAsync(Guid userId, CancellationToken ct)
    {
        var result = await _assignPlatformRole.ExecuteAsync(
            userId,
            PlatformSystemRole.PlatformAdministrator,
            organizationId: null,
            actorIdentifier: LivePreviewOptions.Actor,
            actorType: AuditActorType.System,
            reason: "live-preview dataset",
            cancellationToken: ct).ConfigureAwait(false);

        if (!result.IsSuccess && result.ErrorCode != ApplicationErrorCodes.RoleAssignmentConflict)
        {
            throw new InvalidOperationException(
                $"Live preview PlatformAdministrator assign failed: {result.ErrorCode} {result.ErrorMessage}");
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
                $"Live preview membership failed: {result.ErrorCode} {result.ErrorMessage}");
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
            .ExecuteAsync(organizationId, userId, productCode, LivePreviewOptions.Actor, "live-preview dataset", ct)
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
                $"Live preview product access grant failed: {result.ErrorCode} {result.ErrorMessage}");
        }
    }
}
