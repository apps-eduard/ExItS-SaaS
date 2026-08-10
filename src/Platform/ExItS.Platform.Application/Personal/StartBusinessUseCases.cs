using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Application.Personal;

public sealed record StartBusinessRequest(
    string DisplayName,
    string Slug,
    Guid PrimaryBusinessTypeId,
    string? ProductCode = null,
    Guid? PlanId = null,
    Guid? PlanVersionId = null,
    Guid? TrialDefinitionId = null,
    string? PlanKey = null,
    BillingCycle? BillingCycle = null,
    bool StartAsTrial = true,
    bool PayNow = false,
    bool ActivatePosEntitlement = true,
    bool ActivateProductAccess = true,
    bool AssignPosOwnerRole = true);

public sealed record StartBusinessResultDto(
    Guid OrganizationId,
    Guid MembershipId,
    Guid OrganizationAccountProfileId,
    string SessionToken,
    Guid SessionId,
    string AccountClass,
    string AllowedScope,
    Guid? SelectedOrganizationId,
    Guid? SubscriptionId,
    int? EntitlementSnapshotVersion,
    Guid? ProductAccessAssignmentId,
    Guid? ProductLocalRoleGrantId,
    string? ProductLocalRoleCode,
    bool OrganizationOwnerGranted,
    bool PosEntitlementActivated,
    bool PosOwnerRoleGranted,
    string ProductCode,
    Guid? PrimaryBusinessTypeId = null,
    Guid? PrimaryBranchId = null);

public sealed class StartBusinessForPersonalUser
{
    private static readonly string[] ProvisionalFeatureCodes =
    [
        FeatureCode.CustomerCreditView,
        FeatureCode.CustomerCreditRepay,
        FeatureCode.CustomerCreditCreate,
        FeatureCode.StoreCatalogView,
        FeatureCode.StoreCatalogManage,
        FeatureCode.StoreSalesView,
        FeatureCode.StoreSalesCreate,
        FeatureCode.StoreSalesVoid,
        FeatureCode.StoreDashboardView,
        FeatureCode.StoreReportsView,
        FeatureCode.StorePermissionsView,
        FeatureCode.StorePermissionsManage,
        FeatureCode.StoreInventoryView,
        FeatureCode.StoreInventoryManage,
        FeatureCode.StoreExpensesView,
        FeatureCode.StoreExpensesManage,
        FeatureCode.StoreSuppliersView,
        FeatureCode.StoreSuppliersManage,
        FeatureCode.StoreShiftsView,
        FeatureCode.StoreShiftsManage,
        FeatureCode.StoreReturnsView,
        FeatureCode.StoreReturnsManage,
        FeatureCode.StoreRegistersView,
        FeatureCode.StoreRegistersManage
    ];

    private const string ProvisionalPlanCode = "start-business-pos";
    private const string ProvisionalPlanDisplayName = "Start a Business POS Plan";
    private const string ProvisionalTrialDisplayName = "Start a Business Trial";

    private readonly CreatePlatformOrganization _createOrganization;
    private readonly AddOrganizationMembership _addMembership;
    private readonly EnsureAccountProfilesForUser _ensureProfiles;
    private readonly SelectAccountProfileSession _selectProfile;
    private readonly SetSessionOrganizationContext _setOrganizationContext;
    private readonly CreateProduct _createProduct;
    private readonly ActivateProduct _activateProduct;
    private readonly CreateFeatureDefinition _createFeature;
    private readonly CreatePlan _createPlan;
    private readonly ActivatePlan _activatePlan;
    private readonly CreateDraftPlanVersion _createDraftVersion;
    private readonly PublishExistingPlanVersion _publishVersion;
    private readonly CreateTrialDefinition _createTrial;
    private readonly StartTrialSubscription _startTrial;
    private readonly ActivatePaidSubscription _activatePaid;
    private readonly EnsureMvpPosPlans _ensureMvpPosPlans;
    private readonly IPaymentProvider _paymentProvider;
    private readonly RecordLinkedSuccessfulProviderPayment _recordLinkedPayment;
    private readonly GenerateEntitlementSnapshot _generateSnapshot;
    private readonly GrantProductAccess _grantProductAccess;
    private readonly IProductRepository _products;
    private readonly IPlanRepository _plans;
    private readonly ITrialDefinitionRepository _trials;
    private readonly IPlatformOrganizationRepository _organizations;
    private readonly IOrganizationMembershipRepository _memberships;
    private readonly ISubscriptionRepository _subscriptions;
    private readonly IEntitlementSnapshotRepository _entitlementSnapshots;
    private readonly IProductAccessAssignmentRepository _accessAssignments;
    private readonly IProductLocalRoleGrantRepository _roleGrants;
    private readonly IBusinessTypeRepository _businessTypes;
    private readonly IOrganizationBranchRepository _branches;
    private readonly IAuditWriter _auditWriter;
    private readonly IPlatformUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public StartBusinessForPersonalUser(
        CreatePlatformOrganization createOrganization,
        AddOrganizationMembership addMembership,
        EnsureAccountProfilesForUser ensureProfiles,
        SelectAccountProfileSession selectProfile,
        SetSessionOrganizationContext setOrganizationContext,
        CreateProduct createProduct,
        ActivateProduct activateProduct,
        CreateFeatureDefinition createFeature,
        CreatePlan createPlan,
        ActivatePlan activatePlan,
        CreateDraftPlanVersion createDraftVersion,
        PublishExistingPlanVersion publishVersion,
        CreateTrialDefinition createTrial,
        StartTrialSubscription startTrial,
        ActivatePaidSubscription activatePaid,
        EnsureMvpPosPlans ensureMvpPosPlans,
        IPaymentProvider paymentProvider,
        RecordLinkedSuccessfulProviderPayment recordLinkedPayment,
        GenerateEntitlementSnapshot generateSnapshot,
        GrantProductAccess grantProductAccess,
        IProductRepository products,
        IPlanRepository plans,
        ITrialDefinitionRepository trials,
        IPlatformOrganizationRepository organizations,
        IOrganizationMembershipRepository memberships,
        ISubscriptionRepository subscriptions,
        IEntitlementSnapshotRepository entitlementSnapshots,
        IProductAccessAssignmentRepository accessAssignments,
        IProductLocalRoleGrantRepository roleGrants,
        IBusinessTypeRepository businessTypes,
        IOrganizationBranchRepository branches,
        IAuditWriter auditWriter,
        IPlatformUnitOfWork unitOfWork,
        IClock clock)
    {
        _createOrganization = createOrganization;
        _addMembership = addMembership;
        _ensureProfiles = ensureProfiles;
        _selectProfile = selectProfile;
        _setOrganizationContext = setOrganizationContext;
        _createProduct = createProduct;
        _activateProduct = activateProduct;
        _createFeature = createFeature;
        _createPlan = createPlan;
        _activatePlan = activatePlan;
        _createDraftVersion = createDraftVersion;
        _publishVersion = publishVersion;
        _createTrial = createTrial;
        _startTrial = startTrial;
        _activatePaid = activatePaid;
        _ensureMvpPosPlans = ensureMvpPosPlans;
        _paymentProvider = paymentProvider;
        _recordLinkedPayment = recordLinkedPayment;
        _generateSnapshot = generateSnapshot;
        _grantProductAccess = grantProductAccess;
        _products = products;
        _plans = plans;
        _trials = trials;
        _organizations = organizations;
        _memberships = memberships;
        _subscriptions = subscriptions;
        _entitlementSnapshots = entitlementSnapshots;
        _accessAssignments = accessAssignments;
        _roleGrants = roleGrants;
        _businessTypes = businessTypes;
        _branches = branches;
        _auditWriter = auditWriter;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<ApplicationResult<StartBusinessResultDto>> ExecuteAsync(
        PlatformUserId userId,
        PlatformAuthSessionId currentSessionId,
        StartBusinessRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(request);

        var productCode = string.IsNullOrWhiteSpace(request.ProductCode)
            ? ProductCode.PinoyBusinessPos
            : request.ProductCode.Trim().ToLowerInvariant();

        if (request.PrimaryBusinessTypeId == Guid.Empty)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(
                ApplicationErrorCodes.BusinessTypeNotFound,
                "Select an active business type before starting a business.");
        }

        var businessType = await _businessTypes
            .GetByIdAsync(BusinessTypeId.From(request.PrimaryBusinessTypeId), cancellationToken)
            .ConfigureAwait(false);
        if (businessType is null || businessType.Status != BusinessTypeStatus.Active)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(
                ApplicationErrorCodes.BusinessTypeNotFound,
                "The selected business type was not found or is no longer active.");
        }

        await _auditWriter.WriteAsync(
            $"platform-user:{userId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.BusinessUpgradeStarted,
            nameof(PlatformOrganization),
            request.Slug,
            AuditOutcome.Succeeded,
            summary: "Start a Business flow initiated from Personal session.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        PlatformOrganization? existingOrganization = null;
        try
        {
            var normalizedSlug = PlatformOrganization.NormalizeSlug(request.Slug);
            existingOrganization = await _organizations
                .GetBySlugAsync(normalizedSlug, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException)
        {
            // Invalid slug — CreatePlatformOrganization will return the domain error.
        }

        if (existingOrganization is not null)
        {
            var existingMembership = await _memberships
                .FindActiveByUserAndOrganizationAsync(userId, existingOrganization.Id, cancellationToken)
                .ConfigureAwait(false);
            if (existingMembership is not null
                && existingMembership.Role == OrganizationRole.OrganizationOwner)
            {
                return await ResumeExistingStartBusinessAsync(
                        userId,
                        currentSessionId,
                        existingOrganization,
                        existingMembership,
                        request,
                        productCode,
                        ipAddress,
                        userAgent,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            return ApplicationResult<StartBusinessResultDto>.Failure(
                ApplicationErrorCodes.SlugConflict,
                "A Platform Organization with this slug already exists.");
        }

        var orgResult = await _createOrganization
            .ExecuteAsync(request.DisplayName, request.Slug, cancellationToken)
            .ConfigureAwait(false);
        if (!orgResult.IsSuccess || orgResult.Value is null)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(
                orgResult.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                orgResult.ErrorMessage ?? "Organization create failed.");
        }

        var organization = orgResult.Value;
        organization.AssignPrimaryBusinessType(businessType.Id, _clock.UtcNow);
        await _organizations.UpdateAsync(organization, cancellationToken).ConfigureAwait(false);

        var mainBranch = OrganizationBranch.CreateMainBranch(organization.Id, _clock.UtcNow);
        await _branches.AddAsync(mainBranch, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var membershipResult = await _addMembership
            .ExecuteAsync(
                organization.Id,
                userId,
                OrganizationRole.OrganizationOwner,
                exclusiveOrganizationProfile: false,
                cancellationToken)
            .ConfigureAwait(false);
        if (!membershipResult.IsSuccess || membershipResult.Value is null)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(
                membershipResult.ErrorCode ?? ApplicationErrorCodes.DomainViolation,
                membershipResult.ErrorMessage ?? "Owner membership failed.");
        }

        AccountProfile orgProfile;
        try
        {
            orgProfile = await _ensureProfiles
                .ExecuteAsync(userId, AccountClass.Organization, exclusivePreferredClass: false, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(ex.ErrorCode, ex.Message);
        }

        var sessionResult = await _selectProfile
            .ExecuteAsync(userId, currentSessionId, orgProfile.Id.Value, ipAddress, userAgent, cancellationToken)
            .ConfigureAwait(false);
        if (!sessionResult.IsSuccess || sessionResult.Value is null)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(
                sessionResult.ErrorCode ?? ApplicationErrorCodes.SessionInvalid,
                sessionResult.ErrorMessage ?? "Organization session switch failed.");
        }

        // Prefer the newly created organization when the user already has other memberships.
        var contextResult = await _setOrganizationContext
            .ExecuteAsync(sessionResult.Value.SessionToken, organization.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (!contextResult.IsSuccess)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(
                contextResult.ErrorCode ?? ApplicationErrorCodes.OrganizationContextNotEligible,
                contextResult.ErrorMessage ?? "Could not select the new organization context.");
        }

        Guid? subscriptionId = null;
        int? snapshotVersion = null;
        Guid? accessAssignmentId = null;
        Guid? roleGrantId = null;
        string? roleCode = null;
        var entitlementActivated = false;
        var ownerRoleGranted = false;

        if (request.ActivatePosEntitlement)
        {
            if (!string.IsNullOrWhiteSpace(request.PlanKey))
            {
                await _ensureMvpPosPlans.ExecuteAsync(cancellationToken).ConfigureAwait(false);
            }

            var catalog = await ResolveCatalogAsync(
                    productCode,
                    request.PlanKey,
                    request.PlanId,
                    request.PlanVersionId,
                    request.TrialDefinitionId,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!catalog.IsSuccess || catalog.Value is null)
            {
                return ApplicationResult<StartBusinessResultDto>.Failure(
                    catalog.ErrorCode ?? ApplicationErrorCodes.ProductNotFound,
                    catalog.ErrorMessage ?? "POS catalog is not available.");
            }

            var billingCycle = request.BillingCycle ?? BillingCycle.Monthly;
            var startAsTrial = request.StartAsTrial && !request.PayNow;

            if (startAsTrial)
            {
                var selectedPlan = await _plans.GetByIdAsync(catalog.Value.PlanId, cancellationToken).ConfigureAwait(false);
                if (selectedPlan is null)
                {
                    return ApplicationResult<StartBusinessResultDto>.Failure(
                        ApplicationErrorCodes.PlanNotFound,
                        "Plan was not found.");
                }

                if (!selectedPlan.TrialAllowed)
                {
                    return ApplicationResult<StartBusinessResultDto>.Failure(
                        ApplicationErrorCodes.TrialNotAllowed,
                        "This plan does not allow trials. Subscribe now or try Business first.");
                }
            }

            if (request.PayNow)
            {
                var plan = await _plans.GetByIdAsync(catalog.Value.PlanId, cancellationToken).ConfigureAwait(false);
                if (plan is null)
                {
                    return ApplicationResult<StartBusinessResultDto>.Failure(
                        ApplicationErrorCodes.PlanNotFound,
                        "Plan was not found.");
                }

                // Payment rows FK to subscriptions — activate first, then charge with the real id.
                var utcNow = _clock.UtcNow;
                var (periodStart, periodEnd) = SubscriptionBillingPeriods.ComputePaidPeriod(utcNow, billingCycle);
                var paid = await _activatePaid
                    .ExecuteAsync(
                        organization.Id,
                        catalog.Value.PlanId,
                        catalog.Value.PlanVersionId,
                        periodStart,
                        periodEnd,
                        billingCycle,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!paid.IsSuccess || paid.Value is null)
                {
                    return ApplicationResult<StartBusinessResultDto>.Failure(
                        paid.ErrorCode ?? ApplicationErrorCodes.SubscriptionIneligible,
                        paid.ErrorMessage ?? "Paid subscription failed.");
                }

                var activated = paid.Value;
                var idempotencyKey = $"start-business-{organization.Id.Value:N}-{activated.Id.Value:N}";
                Domain.Payments.PaymentProviderResult paymentResult;
                try
                {
                    paymentResult = await _paymentProvider.ChargeAsync(
                        new Domain.Payments.PaymentChargeRequest(
                            organization.Id.Value,
                            activated.Id.Value,
                            plan.PriceForCycle(billingCycle),
                            plan.CurrencyCode,
                            idempotencyKey,
                            Purpose: "start-business"),
                        cancellationToken).ConfigureAwait(false);
                }
                catch (NotSupportedException ex)
                {
                    activated.Cancel(_clock.UtcNow);
                    await _subscriptions.UpdateAsync(activated, cancellationToken).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return ApplicationResult<StartBusinessResultDto>.Failure(
                        ApplicationErrorCodes.PaymentNotConfigured,
                        ex.Message);
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                if (paymentResult.Status != Domain.Payments.PaymentProviderResultStatus.Succeeded)
                {
                    activated.Cancel(_clock.UtcNow);
                    await _subscriptions.UpdateAsync(activated, cancellationToken).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return ApplicationResult<StartBusinessResultDto>.Failure(
                        ApplicationErrorCodes.PaymentNotConfirmed,
                        paymentResult.FailureMessage ?? "Initial payment was not successful.");
                }

                var linked = await _recordLinkedPayment
                    .ExecuteAsync(
                        organization.Id,
                        ProductCode.Create(productCode),
                        activated.Id,
                        paymentResult,
                        "start-business",
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!linked.IsSuccess)
                {
                    activated.Cancel(_clock.UtcNow);
                    await _subscriptions.UpdateAsync(activated, cancellationToken).ConfigureAwait(false);
                    await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                    return ApplicationResult<StartBusinessResultDto>.Failure(
                        linked.ErrorCode ?? ApplicationErrorCodes.PaymentNotConfirmed,
                        linked.ErrorMessage ?? "Successful payment could not be linked for administration.");
                }

                subscriptionId = activated.Id.Value;
            }
            else if (startAsTrial)
            {
                if (catalog.Value.TrialDefinitionId is null)
                {
                    return ApplicationResult<StartBusinessResultDto>.Failure(
                        ApplicationErrorCodes.TrialNotFound,
                        "This plan does not have a trial definition. Subscribe now instead.");
                }

                var trialDefinitionId = catalog.Value.TrialDefinitionId
                    ?? throw new InvalidOperationException("Trial definition id was null after guard.");
                var trial = await _startTrial
                    .ExecuteAsync(
                        organization.Id,
                        catalog.Value.PlanId,
                        catalog.Value.PlanVersionId,
                        trialDefinitionId,
                        cancellationToken)
                    .ConfigureAwait(false);
                if (!trial.IsSuccess || trial.Value is null)
                {
                    return ApplicationResult<StartBusinessResultDto>.Failure(
                        trial.ErrorCode ?? ApplicationErrorCodes.SubscriptionIneligible,
                        trial.ErrorMessage ?? "Trial subscription failed.");
                }

                subscriptionId = trial.Value.Id.Value;
            }
            else
            {
                return ApplicationResult<StartBusinessResultDto>.Failure(
                    ApplicationErrorCodes.PaymentRequiredForPaidActivation,
                    "Paid Start a Business requires PayNow with a successful payment. Start a trial, or subscribe with payment.");
            }

            var snapshot = await _generateSnapshot
                .ExecuteAsync(organization.Id, ProductCode.Create(productCode), cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!snapshot.IsSuccess || snapshot.Value is null)
            {
                return ApplicationResult<StartBusinessResultDto>.Failure(
                    snapshot.ErrorCode ?? ApplicationErrorCodes.EntitlementMissing,
                    snapshot.ErrorMessage ?? "Entitlement snapshot failed.");
            }

            snapshotVersion = snapshot.Value.SnapshotVersion;
            entitlementActivated = true;
        }

        if (request.ActivateProductAccess && entitlementActivated)
        {
            var access = await _grantProductAccess
                .ExecuteAsync(
                    organization.Id,
                    userId,
                    productCode,
                    $"platform-user:{userId.Value:D}",
                    "Start a Business product access grant.",
                    cancellationToken)
                .ConfigureAwait(false);
            if (!access.IsSuccess || access.Value is null)
            {
                return ApplicationResult<StartBusinessResultDto>.Failure(
                    access.ErrorCode ?? ApplicationErrorCodes.ProductAccessConflict,
                    access.ErrorMessage ?? "Product access grant failed.");
            }

            accessAssignmentId = access.Value.Id.Value;
        }

        // Approved MVP provisioning: when POS entitlement activates for the business creator,
        // grant the first POS Owner role (Organization Owner alone is not POS access).
        var assignFirstPosOwner = request.AssignPosOwnerRole
            || (request.ActivatePosEntitlement && entitlementActivated);
        if (assignFirstPosOwner)
        {
            var existing = await _roleGrants
                .FindActiveByUserOrganizationProductAsync(organization.Id, userId, productCode, cancellationToken)
                .ConfigureAwait(false);
            if (existing is null)
            {
                var grant = ProductLocalRoleGrant.Create(
                    organization.Id,
                    userId,
                    productCode,
                    ProductLocalRoleGrant.PosOwnerRoleCode,
                    userId,
                    _clock.UtcNow);
                await _roleGrants.AddAsync(grant, cancellationToken).ConfigureAwait(false);
                await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                roleGrantId = grant.Id.Value;
                roleCode = grant.RoleCode;
                ownerRoleGranted = true;

                await _auditWriter.WriteAsync(
                    $"platform-user:{userId.Value:D}",
                    AuditActorType.PlatformUser,
                    PlatformAuditActions.ProductLocalRoleGranted,
                    nameof(ProductLocalRoleGrant),
                    grant.Id.Value.ToString("D"),
                    AuditOutcome.Succeeded,
                    organizationId: organization.Id,
                    productCode: ProductCode.Create(productCode),
                    summary: "POS Owner product-local role granted (Platform record; POS-mapped Owner).",
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                roleGrantId = existing.Id.Value;
                roleCode = existing.RoleCode;
                ownerRoleGranted = true;
            }
        }

        await _auditWriter.WriteAsync(
            $"platform-user:{userId.Value:D}",
            AuditActorType.PlatformUser,
            PlatformAuditActions.BusinessUpgradeCompleted,
            nameof(PlatformOrganization),
            organization.Id.Value.ToString("D"),
            AuditOutcome.Succeeded,
            organizationId: organization.Id,
            productCode: ProductCode.Create(productCode),
            summary: "Start a Business completed: Organization Owner plus first POS Owner when POS entitlement is active.",
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var login = sessionResult.Value;
        return ApplicationResult<StartBusinessResultDto>.Success(new StartBusinessResultDto(
            organization.Id.Value,
            membershipResult.Value.Id.Value,
            orgProfile.Id.Value,
            login.SessionToken,
            login.SessionId,
            login.AccountClass ?? AccountClass.Organization.ToString(),
            login.AllowedScope ?? "Organization",
            contextResult.Value?.SelectedOrganizationId ?? organization.Id.Value,
            subscriptionId,
            snapshotVersion,
            accessAssignmentId,
            roleGrantId,
            roleCode,
            OrganizationOwnerGranted: true,
            PosEntitlementActivated: entitlementActivated,
            PosOwnerRoleGranted: ownerRoleGranted,
            ProductCode: productCode,
            PrimaryBusinessTypeId: organization.PrimaryBusinessTypeId?.Value,
            PrimaryBranchId: mainBranch.Id.Value));
    }

    private async Task<ApplicationResult<StartBusinessResultDto>> ResumeExistingStartBusinessAsync(
        PlatformUserId userId,
        PlatformAuthSessionId currentSessionId,
        PlatformOrganization organization,
        OrganizationMembership membership,
        StartBusinessRequest request,
        string productCode,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        AccountProfile orgProfile;
        try
        {
            orgProfile = await _ensureProfiles
                .ExecuteAsync(userId, AccountClass.Organization, exclusivePreferredClass: false, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(ex.ErrorCode, ex.Message);
        }

        var sessionResult = await _selectProfile
            .ExecuteAsync(userId, currentSessionId, orgProfile.Id.Value, ipAddress, userAgent, cancellationToken)
            .ConfigureAwait(false);
        if (!sessionResult.IsSuccess || sessionResult.Value is null)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(
                sessionResult.ErrorCode ?? ApplicationErrorCodes.SessionInvalid,
                sessionResult.ErrorMessage ?? "Organization session switch failed.");
        }

        var contextResult = await _setOrganizationContext
            .ExecuteAsync(sessionResult.Value.SessionToken, organization.Id.Value, cancellationToken)
            .ConfigureAwait(false);
        if (!contextResult.IsSuccess)
        {
            return ApplicationResult<StartBusinessResultDto>.Failure(
                contextResult.ErrorCode ?? ApplicationErrorCodes.OrganizationContextNotEligible,
                contextResult.ErrorMessage ?? "Could not select the organization context.");
        }

        Guid? subscriptionId = null;
        int? snapshotVersion = null;
        Guid? accessAssignmentId = null;
        Guid? roleGrantId = null;
        string? roleCode = null;
        var entitlementActivated = false;
        var ownerRoleGranted = false;
        var pc = ProductCode.Create(productCode);

        var subscription = await _subscriptions
            .GetCurrentForOrganizationProductAsync(organization.Id, pc, cancellationToken)
            .ConfigureAwait(false);
        if (subscription is not null)
        {
            subscriptionId = subscription.Id.Value;
            entitlementActivated = request.ActivatePosEntitlement;
        }

        if (entitlementActivated)
        {
            snapshotVersion = await _entitlementSnapshots
                .GetLatestSnapshotVersionAsync(organization.Id, pc, cancellationToken)
                .ConfigureAwait(false);
        }

        if (request.ActivateProductAccess && entitlementActivated)
        {
            var access = await _accessAssignments
                .FindActiveByUserOrganizationProductAsync(userId, organization.Id, pc, cancellationToken)
                .ConfigureAwait(false);
            accessAssignmentId = access?.Id.Value;
        }

        if (request.AssignPosOwnerRole
            || (request.ActivatePosEntitlement && entitlementActivated))
        {
            var existingRole = await _roleGrants
                .FindActiveByUserOrganizationProductAsync(organization.Id, userId, productCode, cancellationToken)
                .ConfigureAwait(false);
            if (existingRole is not null)
            {
                roleGrantId = existingRole.Id.Value;
                roleCode = existingRole.RoleCode;
                ownerRoleGranted = true;
            }
        }

        var login = sessionResult.Value;
        return ApplicationResult<StartBusinessResultDto>.Success(new StartBusinessResultDto(
            organization.Id.Value,
            membership.Id.Value,
            orgProfile.Id.Value,
            login.SessionToken,
            login.SessionId,
            login.AccountClass ?? AccountClass.Organization.ToString(),
            login.AllowedScope ?? "Organization",
            contextResult.Value?.SelectedOrganizationId ?? organization.Id.Value,
            subscriptionId,
            snapshotVersion,
            accessAssignmentId,
            roleGrantId,
            roleCode,
            OrganizationOwnerGranted: true,
            PosEntitlementActivated: entitlementActivated,
            PosOwnerRoleGranted: ownerRoleGranted,
            ProductCode: productCode,
            PrimaryBusinessTypeId: organization.PrimaryBusinessTypeId?.Value,
            PrimaryBranchId: (await _branches.GetPrimaryAsync(organization.Id, cancellationToken).ConfigureAwait(false))?.Id.Value));
    }

    private sealed record CatalogSelection(PlanId PlanId, PlanVersionId PlanVersionId, TrialDefinitionId? TrialDefinitionId);

    private async Task<ApplicationResult<CatalogSelection>> ResolveCatalogAsync(
        string productCode,
        string? planKey,
        Guid? planId,
        Guid? planVersionId,
        Guid? trialDefinitionId,
        CancellationToken cancellationToken)
    {
        if (planId is Guid p && planVersionId is Guid v)
        {
            TrialDefinitionId? trialId = trialDefinitionId is Guid t ? TrialDefinitionId.From(t) : null;
            return ApplicationResult<CatalogSelection>.Success(
                new CatalogSelection(PlanId.From(p), PlanVersionId.From(v), trialId));
        }

        if (!string.IsNullOrWhiteSpace(planKey))
        {
            return await ResolveMvpPlanCatalogAsync(productCode, planKey.Trim(), cancellationToken).ConfigureAwait(false);
        }

        return await EnsureProvisionalCatalogAsync(productCode, null, null, null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ApplicationResult<CatalogSelection>> ResolveMvpPlanCatalogAsync(
        string productCode,
        string planKey,
        CancellationToken cancellationToken)
    {
        var pc = ProductCode.Create(productCode);
        var plan = await _plans
            .GetByProductAndCodeAsync(pc, PlanCode.Create(planKey), cancellationToken)
            .ConfigureAwait(false);
        if (plan is null || plan.Status != PlanStatus.Active)
        {
            return ApplicationResult<CatalogSelection>.Failure(
                ApplicationErrorCodes.PlanNotFound,
                $"MVP plan '{planKey}' was not found or is not active.");
        }

        var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var version = versions.FirstOrDefault(v => v.Status == PlanVersionStatus.Published);
        if (version is null)
        {
            return ApplicationResult<CatalogSelection>.Failure(
                ApplicationErrorCodes.PlanVersionNotFound,
                $"Published plan version for '{planKey}' was not found.");
        }

        // Paid-only plans (e.g. Pro) have TrialAllowed=false / DefaultTrialDays=0 — do not mint a
        // TrialDefinition; Subscribe/PayNow only needs plan + published version.
        if (!plan.TrialAllowed || plan.DefaultTrialDays <= 0)
        {
            return ApplicationResult<CatalogSelection>.Success(
                new CatalogSelection(plan.Id, version.Id, TrialDefinitionId: null));
        }

        var trials = await _trials.ListByProductAsync(pc, cancellationToken).ConfigureAwait(false);
        var activeTrials = trials.Where(t => t.Status == TrialDefinitionStatus.Active).ToList();
        var expectedDuration = TimeSpan.FromDays(plan.DefaultTrialDays);
        var trial = activeTrials.FirstOrDefault(t => t.PlanId == plan.Id)
            ?? activeTrials.FirstOrDefault(t => t.Duration == expectedDuration);

        if (trial is null)
        {
            var spec = MvpPosPlanCatalog.Plans.FirstOrDefault(p =>
                string.Equals(p.PlanKey, planKey, StringComparison.Ordinal));
            FeatureGrantSpec[] grants;
            if (spec is not null)
            {
                grants = EnsureMvpPosPlans.BuildGrants(spec);
            }
            else
            {
                grants = ProvisionalFeatureCodes
                    .Select(c => FeatureGrantSpec.Boolean(FeatureCode.Create(c), true))
                    .ToArray();
            }

            var createdTrial = await _createTrial
                .ExecuteAsync(
                    productCode,
                    $"{plan.DisplayName} Trial",
                    expectedDuration,
                    grants,
                    Array.Empty<FeatureGrantSpec>(),
                    planId: plan.Id.Value,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!createdTrial.IsSuccess || createdTrial.Value is null)
            {
                return ApplicationResult<CatalogSelection>.Failure(
                    createdTrial.ErrorCode ?? ApplicationErrorCodes.TrialNotFound,
                    createdTrial.ErrorMessage ?? "Trial create failed.");
            }

            trial = createdTrial.Value;
        }

        return ApplicationResult<CatalogSelection>.Success(
            new CatalogSelection(plan.Id, version.Id, trial.Id));
    }

    private async Task<ApplicationResult<CatalogSelection>> EnsureProvisionalCatalogAsync(
        string productCode,
        Guid? planId,
        Guid? planVersionId,
        Guid? trialDefinitionId,
        CancellationToken cancellationToken)
    {
        if (planId is Guid p && planVersionId is Guid v)
        {
            TrialDefinitionId? trialId = trialDefinitionId is Guid t ? TrialDefinitionId.From(t) : null;
            return ApplicationResult<CatalogSelection>.Success(
                new CatalogSelection(PlanId.From(p), PlanVersionId.From(v), trialId));
        }

        var code = ProductCode.Create(productCode);
        var product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
        if (product is null)
        {
            var created = await _createProduct
                .ExecuteAsync(productCode, "Pinoy Business POS", cancellationToken)
                .ConfigureAwait(false);
            if (!created.IsSuccess && created.ErrorCode != ApplicationErrorCodes.DuplicateProductCode)
            {
                return ApplicationResult<CatalogSelection>.Failure(
                    created.ErrorCode ?? ApplicationErrorCodes.ProductNotFound,
                    created.ErrorMessage ?? "Product create failed.");
            }

            product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false);
            if (product is null)
            {
                return ApplicationResult<CatalogSelection>.Failure(
                    ApplicationErrorCodes.ProductNotFound,
                    "Product was not found after provisional create.");
            }
        }

        if (product.Status != ProductStatus.Active)
        {
            var activated = await _activateProduct.ExecuteAsync(product.Id, cancellationToken).ConfigureAwait(false);
            if (!activated.IsSuccess && product.Status != ProductStatus.Active)
            {
                // Reload — may already be active from race
                product = await _products.GetByCodeAsync(code, cancellationToken).ConfigureAwait(false)
                    ?? product;
            }
        }

        foreach (var featureCode in ProvisionalFeatureCodes)
        {
            var featureResult = await _createFeature
                .ExecuteAsync(productCode, featureCode, featureCode, FeatureValueType.Boolean, cancellationToken)
                .ConfigureAwait(false);
            if (!featureResult.IsSuccess
                && featureResult.ErrorCode != ApplicationErrorCodes.DuplicateFeatureCode)
            {
                return ApplicationResult<CatalogSelection>.Failure(
                    featureResult.ErrorCode ?? ApplicationErrorCodes.FeatureNotFound,
                    featureResult.ErrorMessage ?? $"Feature '{featureCode}' failed.");
            }
        }

        var planCode = PlanCode.Create(ProvisionalPlanCode);
        var plan = await _plans.GetByProductAndCodeAsync(code, planCode, cancellationToken).ConfigureAwait(false);
        if (plan is null)
        {
            var createdPlan = await _createPlan
                .ExecuteAsync(productCode, ProvisionalPlanCode, ProvisionalPlanDisplayName, cancellationToken)
                .ConfigureAwait(false);
            if (!createdPlan.IsSuccess || createdPlan.Value is null)
            {
                plan = await _plans.GetByProductAndCodeAsync(code, planCode, cancellationToken).ConfigureAwait(false);
                if (plan is null)
                {
                    return ApplicationResult<CatalogSelection>.Failure(
                        createdPlan.ErrorCode ?? ApplicationErrorCodes.PlanNotFound,
                        createdPlan.ErrorMessage ?? "Plan create failed.");
                }
            }
            else
            {
                plan = createdPlan.Value;
            }
        }

        if (plan.Status != PlanStatus.Active)
        {
            var activate = await _activatePlan.ExecuteAsync(plan.Id, cancellationToken).ConfigureAwait(false);
            if (activate.IsSuccess && activate.Value is not null)
            {
                plan = activate.Value;
            }
        }

        var versions = await _plans.ListVersionsAsync(plan.Id, cancellationToken).ConfigureAwait(false);
        var version = versions.FirstOrDefault(x => x.VersionNumber == 1);
        if (version is null)
        {
            var grants = ProvisionalFeatureCodes
                .Select(c => FeatureGrantSpec.Boolean(FeatureCode.Create(c), true))
                .ToArray();
            var draft = await _createDraftVersion
                .ExecuteAsync(plan.Id, 1, BillingPeriod.Monthly, trialEligible: true, grants, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!draft.IsSuccess)
            {
                return ApplicationResult<CatalogSelection>.Failure(
                    draft.ErrorCode ?? ApplicationErrorCodes.PlanVersionNotFound,
                    draft.ErrorMessage ?? "Plan version draft failed.");
            }

            var published = await _publishVersion.ExecuteAsync(plan.Id, 1, cancellationToken).ConfigureAwait(false);
            if (!published.IsSuccess || published.Value is null)
            {
                return ApplicationResult<CatalogSelection>.Failure(
                    published.ErrorCode ?? ApplicationErrorCodes.PlanVersionNotFound,
                    published.ErrorMessage ?? "Plan version publish failed.");
            }

            version = published.Value;
        }
        else if (version.Status != PlanVersionStatus.Published)
        {
            var published = await _publishVersion.ExecuteAsync(plan.Id, 1, cancellationToken).ConfigureAwait(false);
            if (!published.IsSuccess || published.Value is null)
            {
                return ApplicationResult<CatalogSelection>.Failure(
                    published.ErrorCode ?? ApplicationErrorCodes.PlanVersionNotFound,
                    published.ErrorMessage ?? "Plan version publish failed.");
            }

            version = published.Value;
        }

        var trials = await _trials.ListByProductAsync(code, cancellationToken).ConfigureAwait(false);
        var trial = trials.FirstOrDefault(t =>
            string.Equals(t.DisplayName, ProvisionalTrialDisplayName, StringComparison.Ordinal));
        if (trial is null)
        {
            var grants = ProvisionalFeatureCodes
                .Select(c => FeatureGrantSpec.Boolean(FeatureCode.Create(c), true))
                .ToArray();
            var createdTrial = await _createTrial
                .ExecuteAsync(
                    productCode,
                    ProvisionalTrialDisplayName,
                    TimeSpan.FromDays(30),
                    grants,
                    Array.Empty<FeatureGrantSpec>(),
                    planId: null,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            if (!createdTrial.IsSuccess || createdTrial.Value is null)
            {
                return ApplicationResult<CatalogSelection>.Failure(
                    createdTrial.ErrorCode ?? ApplicationErrorCodes.TrialNotFound,
                    createdTrial.ErrorMessage ?? "Trial create failed.");
            }

            trial = createdTrial.Value;
        }

        return ApplicationResult<CatalogSelection>.Success(
            new CatalogSelection(plan.Id, version.Id, trial.Id));
    }
}
