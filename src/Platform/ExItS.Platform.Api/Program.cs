using ExItS.Platform.Api.LocalValidation;
using ExItS.Platform.Api.Access;
using ExItS.Platform.Api.Admin;
using ExItS.Platform.Api.Audit;
using ExItS.Platform.Api.Authentication;
using ExItS.Platform.Api.Authorization;
using ExItS.Platform.Api.Catalog;
using ExItS.Platform.Api.Commercial;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Api.Entitlements;
using ExItS.Platform.Api.GlobalCatalog;
using ExItS.Platform.Api.Identity;
using ExItS.Platform.Api.Organizations;
using ExItS.Platform.Api.Payments;
using ExItS.Platform.Api.Personal;
using ExItS.Platform.Api.PrivacyCompliance;
using ExItS.Platform.Api.Subscriptions;
using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Commercial;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Integration.Pos;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Personal;
using ExItS.Platform.Application.PrivacyCompliance;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Infrastructure;
using ExItS.Platform.Infrastructure.GlobalCatalog;
using ExItS.Platform.Infrastructure.Integration.Pos;
using ExItS.Platform.Infrastructure.Payments;
using ExItS.Platform.Infrastructure.LocalValidation;
using ExItS.Platform.Infrastructure.Health;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;

var builder = WebApplication.CreateBuilder(args);

PlatformSecurityPipeline.ValidateProductionConfigurationOrThrow(builder);
if (builder.Configuration.GetValue<bool>("LocalValidation:Enabled") && builder.Environment.IsProduction())
{
    throw new InvalidOperationException("LocalValidation:Enabled=true is forbidden in Production.");
}

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter(
            namingPolicy: null,
            allowIntegerValues: true));
});
builder.Services.AddPlatformHealthChecks();
builder.AddPlatformSecurity();
builder.AddPlatformForwardedHeaders();
builder.Services.AddPlatformPersistence(builder.Configuration);
builder.Services.AddPlatformPaymentProvider(builder.Configuration, builder.Environment);

var externalAuthOptions = builder.Configuration
    .GetSection(PlatformExternalAuthOptions.SectionName)
    .Get<PlatformExternalAuthOptions>() ?? new PlatformExternalAuthOptions();

var authenticationBuilder = builder.Services.AddAuthentication(PlatformSessionDefaults.AuthenticationScheme)
    .AddScheme<PlatformSessionAuthenticationOptions, PlatformSessionAuthenticationHandler>(
        PlatformSessionDefaults.AuthenticationScheme,
        _ => { })
    .AddCookie(PlatformExternalAuthDefaults.CorrelationScheme, options =>
    {
        options.Cookie.Name = ".ExItS.Platform.External";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing")
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.SlidingExpiration = false;
    });

if (externalAuthOptions.Google.Enabled
    && !string.IsNullOrWhiteSpace(externalAuthOptions.Google.ClientId)
    && !string.IsNullOrWhiteSpace(externalAuthOptions.Google.ClientSecret))
{
    authenticationBuilder.AddGoogle(options =>
    {
        options.ClientId = externalAuthOptions.Google.ClientId;
        options.ClientSecret = externalAuthOptions.Google.ClientSecret;
        options.SignInScheme = PlatformExternalAuthDefaults.CorrelationScheme;
        options.CallbackPath = "/api/v1/platform/auth/external/google/callback";
        options.SaveTokens = false;
        options.Scope.Add("email");
        options.Scope.Add("profile");
    });
}

if (externalAuthOptions.Facebook.Enabled
    && !string.IsNullOrWhiteSpace(externalAuthOptions.Facebook.ClientId)
    && !string.IsNullOrWhiteSpace(externalAuthOptions.Facebook.ClientSecret))
{
    authenticationBuilder.AddFacebook(options =>
    {
        options.AppId = externalAuthOptions.Facebook.ClientId;
        options.AppSecret = externalAuthOptions.Facebook.ClientSecret;
        options.SignInScheme = PlatformExternalAuthDefaults.CorrelationScheme;
        options.CallbackPath = "/api/v1/platform/auth/external/facebook/callback";
        options.SaveTokens = false;
        options.Fields.Add("email");
        options.Fields.Add("name");
        options.Scope.Add("email");
    });
}

builder.Services.AddAuthorization();

// Development-stage only: DevelopmentOperator actors receive full Platform permissions so existing
// unauthenticated development/testing workflows continue while permission enforcement is exercised.
// Must never be enabled outside Development/Testing (never a production authentication substitute).
builder.Services.Configure<DevelopmentAuthorizationOptions>(options =>
{
    options.GrantDevelopmentOperatorFullAccess =
        builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing");
});

builder.Services.AddScoped<CatalogQueryService>();
builder.Services.AddScoped<CreateProduct>();
builder.Services.AddScoped<RenameProduct>();
builder.Services.AddScoped<ActivateProduct>();
builder.Services.AddScoped<DeactivateProduct>();
builder.Services.AddScoped<RetireProduct>();
builder.Services.AddScoped<CreateFeatureDefinition>();
builder.Services.AddScoped<RetireFeatureDefinition>();
builder.Services.AddScoped<CreatePlan>();
builder.Services.AddScoped<RenamePlan>();
builder.Services.AddScoped<ActivatePlan>();
builder.Services.AddScoped<DeactivatePlan>();
builder.Services.AddScoped<UpdatePlanCommercialPackage>();
builder.Services.AddScoped<RetirePlan>();
builder.Services.AddScoped<CreateDraftPlanVersion>();
builder.Services.AddScoped<ReplaceDraftPlanVersionGrants>();
builder.Services.AddScoped<UpsertDraftFeatureGrant>();
builder.Services.AddScoped<PublishExistingPlanVersion>();
builder.Services.AddScoped<PublishPlanVersion>();
builder.Services.AddScoped<CreateTrialDefinition>();
builder.Services.AddScoped<RetireTrialDefinition>();
builder.Services.AddScoped<EnsureMvpPosPlans>();
builder.Services.AddScoped<EnsurePhilippinePosStarterCatalog>();
builder.Services.AddScoped<CommercialCatalogQueryService>();
builder.Services.AddScoped<OrganizationCurrentPlanQueryService>();
builder.Services.AddScoped<StartOrganizationCommercialSubscription>();

builder.Services.AddScoped<BusinessTypeQueryService>();
builder.Services.AddScoped<GlobalCategoryQueryService>();
builder.Services.AddScoped<GlobalProductQueryService>();
builder.Services.AddScoped<CatalogTemplateQueryService>();
builder.Services.AddScoped<CreateBusinessType>();
builder.Services.AddScoped<UpdateBusinessType>();
builder.Services.AddScoped<SetBusinessTypeStatus>();
builder.Services.AddScoped<BulkAssignCategoryBusinessTypes>();
builder.Services.AddScoped<CreateGlobalCategory>();
builder.Services.AddScoped<UpdateGlobalCategory>();
builder.Services.AddScoped<SetGlobalCategoryStatus>();
builder.Services.AddScoped<CreateGlobalProduct>();
builder.Services.AddScoped<UpdateGlobalProduct>();
builder.Services.AddScoped<SetGlobalProductStatus>();
builder.Services.AddScoped<CreateCatalogTemplate>();
builder.Services.AddScoped<UpdateCatalogTemplate>();
builder.Services.AddScoped<PublishCatalogTemplate>();
builder.Services.AddScoped<UnpublishCatalogTemplate>();
builder.Services.AddScoped<ArchiveCatalogTemplate>();
builder.Services.AddScoped<AssignCatalogTemplateProduct>();
builder.Services.AddScoped<BulkAssignCatalogTemplateProducts>();
builder.Services.AddScoped<BulkRemoveCatalogTemplateProducts>();
builder.Services.AddScoped<RemoveCatalogTemplateProduct>();
builder.Services.AddScoped<ReorderCatalogTemplateProducts>();
builder.Services.AddScoped<UpdateCatalogTemplateProductFlags>();
builder.Services.AddScoped<CatalogImportQueryService>();
builder.Services.AddScoped<CreateCatalogImport>();
builder.Services.AddScoped<ConfirmCatalogImport>();
builder.Services.AddScoped<ProcessCatalogImportChunk>();
builder.Services.AddHostedService<CatalogImportBackgroundService>();

builder.Services.AddScoped<OrganizationQueryService>();
builder.Services.AddScoped<CreatePlatformOrganization>();
builder.Services.AddScoped<IOrganizationBusinessTypeEntitlementResolver, OrganizationBusinessTypeEntitlementResolver>();
builder.Services.AddScoped<MerchantCatalogEntitlementGate>();
builder.Services.AddScoped<GetOrganizationBusinessTypeEntitlement>();
builder.Services.AddScoped<ActivateOrganizationBusinessType>();
builder.Services.AddScoped<DeactivateOrganizationBusinessType>();
builder.Services.AddScoped<SuspendPlatformOrganization>();
builder.Services.AddScoped<ReactivatePlatformOrganization>();
builder.Services.AddScoped<ClosePlatformOrganization>();
builder.Services.AddScoped<UpdateOrganizationProfile>();
builder.Services.AddScoped<UpdateOrganizationPlatformFields>();
builder.Services.AddScoped<UpdateOrganizationBranding>();
builder.Services.AddScoped<ListBranches>();
builder.Services.AddScoped<CreateBranch>();
builder.Services.AddScoped<UpdateBranch>();
builder.Services.AddScoped<ArchiveBranch>();
builder.Services.AddScoped<GetBranchCapacity>();
builder.Services.AddScoped<EnsureMainBranchExists>();
builder.Services.AddScoped<ListDevices>();
builder.Services.AddScoped<RegisterCurrentDevice>();
builder.Services.AddScoped<RenameDevice>();
builder.Services.AddScoped<RevokeDevice>();
builder.Services.AddScoped<GetDeviceCapacity>();
builder.Services.AddScoped<AuthorizeForTransactions>();
builder.Services.Configure<PosProductApiOptions>(builder.Configuration.GetSection(PosProductApiOptions.SectionName));
builder.Services.AddHttpClient<IPosOrganizationCatalogReadClient, PosOrganizationCatalogReadClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<PosProductApiOptions>>().Value;
    if (!string.IsNullOrWhiteSpace(opts.BaseUrl))
    {
        client.BaseAddress = new Uri(opts.BaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    client.Timeout = TimeSpan.FromSeconds(15);
});
builder.Services.AddScoped<GetOrganizationCatalogVisibility>();

builder.Services.AddScoped<PlatformUserQueryService>();
builder.Services.AddScoped<CreatePlatformUser>();
builder.Services.AddScoped<CreatePlatformStaffUser>();
builder.Services.AddScoped<IssueEmailVerificationForUser>();
builder.Services.AddScoped<EnsureOrganizationStaffIdentity>();
builder.Services.AddScoped<UpdatePlatformUserProfile>();
builder.Services.AddScoped<SuspendPlatformUser>();
builder.Services.AddScoped<ReactivatePlatformUser>();
builder.Services.AddScoped<DeactivatePlatformUser>();
builder.Services.AddScoped<MovePlatformUserToSuspended>();
builder.Services.AddScoped<PlatformLifecycleStepUp>();

builder.Services.AddScoped<GetPlatformCredentialStatus>();
builder.Services.AddScoped<SetPlatformUserPassword>();
builder.Services.AddScoped<UnlockPlatformUserCredential>();
builder.Services.AddScoped<MarkPlatformUserEmailVerified>();
builder.Services.AddScoped<VerifyPlatformUserPassword>();
builder.Services.AddScoped<BootstrapFirstPlatformAdministrator>();
builder.Services.AddScoped<LoginPlatformUser>();
builder.Services.AddScoped<LogoutPlatformSession>();
builder.Services.AddScoped<ValidateAndRenewPlatformSession>();
builder.Services.AddScoped<CompleteExternalLogin>();
builder.Services.AddScoped<EnsureAccountProfilesForUser>();
builder.Services.AddScoped<ListAccountProfilesForUser>();
builder.Services.AddScoped<SelectAccountProfileSession>();
builder.Services.AddScoped<ListEligibleOrganizationsForSession>();
builder.Services.AddScoped<SetSessionOrganizationContext>();
builder.Services.AddScoped<InitializePhase16AccountSeed>();
builder.Services.AddScoped<InitializePhase16PersonalUtangSeed>();
builder.Services.AddScoped<GetPersonalDashboard>();
builder.Services.AddScoped<GetPersonalProfile>();
builder.Services.AddScoped<GetPersonalAccountSettings>();
builder.Services.AddScoped<UpdatePersonalAccountSettings>();
builder.Services.AddScoped<CreatePersonalContact>();
builder.Services.AddScoped<ListPersonalContacts>();
builder.Services.AddScoped<CreatePersonalDebtRelationship>();
builder.Services.AddScoped<ListPersonalUtangRelationships>();
builder.Services.AddScoped<GetPersonalUtangRelationship>();
builder.Services.AddScoped<GetPersonalUtangBalance>();
builder.Services.AddScoped<ListPersonalUtangHistory>();
builder.Services.AddScoped<RecordPersonalUtangEntry>();
builder.Services.AddScoped<CreatePersonalUtangInvitation>();
builder.Services.AddScoped<ListPersonalUtangInvitations>();
builder.Services.AddScoped<ResendPersonalUtangInvitation>();
builder.Services.AddScoped<RevokePersonalUtangInvitation>();
builder.Services.AddScoped<DeclinePersonalUtangInvitation>();
builder.Services.AddScoped<AcceptPersonalUtangInvitation>();
builder.Services.AddScoped<CreatePersonalReminder>();
builder.Services.AddScoped<ListPersonalReminders>();
builder.Services.AddScoped<ListDuePersonalReminders>();
builder.Services.AddScoped<DeliverPersonalReminder>();
builder.Services.AddScoped<CancelPersonalReminder>();
builder.Services.AddScoped<ListPersonalInAppNotifications>();
builder.Services.AddScoped<MarkPersonalInAppNotificationRead>();
builder.Services.AddScoped<ListPersonalNotificationDeliveries>();
builder.Services.AddScoped<IssuePlatformAccessToken>();
builder.Services.AddScoped<BindPlatformAccessTokenProductContext>();
builder.Services.AddScoped<IntrospectPlatformAccessToken>();
builder.Services.AddScoped<RevokePlatformAccessToken>();
builder.Services.AddScoped<ChangePlatformUserPassword>();
builder.Services.AddScoped<RequestPasswordReset>();
builder.Services.AddScoped<ResetPasswordWithToken>();
builder.Services.AddScoped<RequestEmailVerification>();
builder.Services.AddScoped<ConfirmEmailVerification>();
builder.Services.AddScoped<GetOrAssignPublicIdentity>();
builder.Services.AddScoped<ResolvePublicUserId>();
builder.Services.AddScoped<RegisterPersonalAccount>();
builder.Services.AddScoped<ActivatePersonalAccountRegistration>();
builder.Services.AddScoped<RequestRecoveryEmailChange>();
builder.Services.AddScoped<ConfirmRecoveryEmailChange>();
builder.Services.AddScoped<SkipRecoveryEmailPrompt>();
builder.Services.AddScoped<ClearRecoveryEmail>();

builder.Services.AddScoped<MembershipQueryService>();
builder.Services.AddScoped<AddOrganizationMembership>();
builder.Services.AddScoped<ChangeOrganizationRole>();
builder.Services.AddScoped<SuspendOrganizationMembership>();
builder.Services.AddScoped<ReactivateOrganizationMembership>();
builder.Services.AddScoped<RevokeOrganizationMembership>();
builder.Services.AddScoped<OrganizationInvitationQueryService>();
builder.Services.AddScoped<CreateOrganizationInvitation>();
builder.Services.AddScoped<ResendOrganizationInvitation>();
builder.Services.AddScoped<RevokeOrganizationInvitation>();
builder.Services.AddScoped<AcceptOrganizationInvitation>();
builder.Services.AddScoped<AcceptOrganizationInvitationByIdForInvitee>();
builder.Services.AddScoped<ListPendingOrganizationInvitationsForUser>();
builder.Services.AddScoped<BusinessCustomerQueryService>();
builder.Services.AddScoped<CreateBusinessCustomer>();
builder.Services.AddScoped<UpdateBusinessCustomer>();
builder.Services.AddScoped<ArchiveBusinessCustomer>();
builder.Services.AddScoped<RejectPromoteBusinessCustomerToStaff>();
builder.Services.AddScoped<CreditCustomerQueryService>();
builder.Services.AddScoped<EnableCreditCustomer>();
builder.Services.AddScoped<CloseCreditCustomer>();
builder.Services.AddScoped<CustomerLinkRequestQueryService>();
builder.Services.AddScoped<LinkedCustomerAppUserQueryService>();
builder.Services.AddScoped<CreateCustomerLinkRequest>();
builder.Services.AddScoped<ResendCustomerLinkRequest>();
builder.Services.AddScoped<RevokeCustomerLinkRequest>();
builder.Services.AddScoped<DeclineCustomerLinkRequest>();
builder.Services.AddScoped<AcceptCustomerLinkRequest>();
builder.Services.AddScoped<UnlinkAcceptedCustomerLink>();
builder.Services.AddScoped<ListLinkedMerchantsForPersonalUser>();
builder.Services.AddScoped<AuthorizeLinkedCustomerAccess>();
builder.Services.AddScoped<DenyStaffAccessToUnrelatedPersonalRecords>();
builder.Services.AddScoped<GrantPersonalFeature>();
builder.Services.AddScoped<RevokePersonalFeature>();
builder.Services.AddScoped<GetPersonalFeatureActiveStatus>();
builder.Services.AddScoped<EnsureKnownPersonalFeatureDefinitions>();
builder.Services.AddScoped<ListPersonalFeatureDefinitions>();
builder.Services.AddScoped<GetPersonalFeatureDefinition>();
builder.Services.AddScoped<UpdatePersonalFeatureDefinition>();
builder.Services.AddScoped<AwardPersonalRewardPoints>();
builder.Services.AddScoped<GetPersonalRewardPointsBalance>();
builder.Services.AddScoped<ListPersonalRewardPointsActivity>();
builder.Services.AddScoped<RedeemPersonalFeatureWithRewardPoints>();
builder.Services.AddScoped<ClaimPersonalAdReward>();
builder.Services.AddScoped<GetPersonalAdEligibility>();
builder.Services.AddScoped<StartBusinessForPersonalUser>();
builder.Services.AddScoped<PreviewPersonalUtangMigration>();
builder.Services.AddScoped<ExecutePersonalUtangMigration>();

builder.Services.AddScoped<ProductAccessQueryService>();
builder.Services.AddScoped<GrantProductAccess>();
builder.Services.AddScoped<RevokeProductAccess>();
builder.Services.AddScoped<EvaluateEffectiveProductAccess>();
builder.Services.AddScoped<EvaluateProductAuthorization>();
builder.Services.AddScoped<DiscoverEnabledProducts>();
builder.Services.AddScoped<AssignProductLocalRole>();
builder.Services.AddScoped<RevokeProductLocalRole>();
builder.Services.AddScoped<ProductLocalRoleGrantQueryService>();

builder.Services.AddScoped<SubscriptionQueryService>();
builder.Services.AddScoped<StartTrialSubscription>();
builder.Services.AddScoped<ActivatePaidSubscription>();
builder.Services.AddScoped<ActivateSubscription>();
builder.Services.AddScoped<EnterSubscriptionGracePeriod>();
builder.Services.AddScoped<MarkSubscriptionPastDue>();
builder.Services.AddScoped<SuspendSubscription>();
builder.Services.AddScoped<ReactivateSubscription>();
builder.Services.AddScoped<CancelSubscription>();
builder.Services.AddScoped<ExpireSubscription>();

builder.Services.AddScoped<SaaSPaymentQueryService>();
builder.Services.AddScoped<CreateManualSaaSPayment>();
builder.Services.AddScoped<ConfirmSaaSPayment>();
builder.Services.AddScoped<RejectSaaSPayment>();
builder.Services.AddScoped<VoidSaaSPayment>();
builder.Services.AddScoped<ConfirmPaymentAndActivateSubscription>();
builder.Services.AddScoped<ActivatePaidSubscriptionFromConfirmedPayment>();
builder.Services.AddScoped<RecordLinkedSuccessfulProviderPayment>();

builder.Services.AddScoped<ProcessSubscriptionInitialPayment>();
builder.Services.AddScoped<ProcessSubscriptionRenewal>();
builder.Services.AddScoped<SimulateLocalValidationPayment>();
builder.Services.AddScoped<ConvertTrialSubscription>();
builder.Services.AddScoped<UpgradeOrganizationSubscription>();
builder.Services.AddScoped<ScheduleOrganizationSubscriptionDowngrade>();
builder.Services.AddScoped<PreviewOrganizationPlanChange>();
builder.Services.AddScoped<ApplyDuePendingPlanChanges>();

builder.Services.AddScoped<EntitlementQueryService>();
builder.Services.AddScoped<FeatureOverrideQueryService>();
builder.Services.AddScoped<CreateFeatureOverride>();
builder.Services.AddScoped<RevokeFeatureOverride>();
builder.Services.AddScoped<GenerateEntitlementSnapshot>();
builder.Services.AddScoped<ReconcileEntitlementSnapshot>();

builder.Services.AddScoped<AdminPortfolioQueryService>();

builder.Services.AddScoped<ListPlatformRoles>();
builder.Services.AddScoped<AssignPlatformRole>();
builder.Services.AddScoped<RevokePlatformRole>();
builder.Services.AddScoped<ResolveCurrentPermissions>();
builder.Services.AddScoped<PlatformRoleDefinitionQueryService>();
builder.Services.AddScoped<CreatePlatformRoleDefinition>();
builder.Services.AddScoped<UpdatePlatformRoleDefinition>();
builder.Services.AddScoped<ChangePlatformRoleDefinitionStatus>();
builder.Services.AddScoped<EnsureBuiltInPlatformRoleDefinitions>();
builder.Services.AddScoped<AssignPlatformCustomRole>();
builder.Services.AddScoped<RevokePlatformCustomRole>();
builder.Services.AddScoped<ListPlatformCustomRoleAssignments>();
builder.Services.AddScoped<ResolveEffectivePlatformPermissions>();
builder.Services.AddScoped<OrganizationRoleDefinitionQueryService>();
builder.Services.AddScoped<CreateOrganizationRoleDefinition>();
builder.Services.AddScoped<UpdateOrganizationRoleDefinition>();
builder.Services.AddScoped<ChangeOrganizationRoleDefinitionStatus>();
builder.Services.AddScoped<AssignOrganizationCustomRole>();
builder.Services.AddScoped<RevokeOrganizationCustomRole>();
builder.Services.AddScoped<ListOrganizationCustomRoleAssignments>();
builder.Services.AddScoped<ResolveEffectiveOrganizationPermissions>();
builder.Services.AddScoped<QueryAuditRecords>();
builder.Services.AddScoped<GetAuditRecord>();
builder.Services.AddScoped<EnsurePrivacyComplianceCatalog>();
builder.Services.AddScoped<GetPrivacyComplianceOverview>();
builder.Services.AddScoped<ListComplianceRequirements>();
builder.Services.AddScoped<GetComplianceRequirement>();
builder.Services.AddScoped<UpdateComplianceRequirementStatus>();
builder.Services.AddScoped<UpdateComplianceRequirementDetails>();
builder.Services.AddScoped<ListComplianceEvidence>();
builder.Services.AddScoped<AddComplianceEvidence>();
builder.Services.AddScoped<ListProcessingSystems>();
builder.Services.AddScoped<ExportComplianceRequirementPdf>();
builder.Services.AddScoped<PlatformAuthz>();
builder.Services.AddScoped<PlatformMembershipAuthz>();
builder.Services.AddScoped<PlatformOrganizationAuthz>();

builder.Services.Configure<LocalValidationOptions>(builder.Configuration.GetSection(LocalValidationOptions.SectionName));
builder.Services.AddScoped<InitializeLocalValidationDataset>();
builder.Services.AddScoped<InitializeLocalValidationPersonalUtangSeed>();
builder.Services.AddScoped<ListLocalValidationIdentities>();
builder.Services.AddScoped<ListLocalValidationQuickLoginIdentities>();
builder.Services.AddHostedService<LocalValidationHostedService>();

var app = builder.Build();

app.UsePlatformForwardedHeaders();
app.UsePlatformSecurity();
app.UseAuthentication();
app.UseMiddleware<AccountScopeGuardMiddleware>();
app.UseAuthorization();
app.UseStatusCodePages();

app.MapPlatformRootEndpoint();
app.MapPlatformHealthEndpoints();
app.MapCatalogEndpoints();
app.MapPersonalFeatureAdminEndpoints();
app.MapGlobalCatalogEndpoints();
app.MapMerchantCatalogDiscoveryEndpoints();
app.MapCommercialEndpoints();
app.MapOrganizationEndpoints();
app.MapBranchAndDeviceEndpoints();
app.MapIdentityEndpoints();
app.MapPublicIdentityEndpoints();
app.MapCredentialEndpoints();
app.MapAuthEndpoints();
app.MapExternalAuthEndpoints();
app.MapPersonalEndpoints();
app.MapMembershipEndpoints();
app.MapInvitationEndpoints();
app.MapBusinessCustomerEndpoints();
app.MapUtangMigrationEndpoints();
app.MapAccessEndpoints();
app.MapProductNavigationEndpoints();
app.MapSubscriptionEndpoints();
app.MapPaymentEndpoints();
app.MapEntitlementEndpoints();
app.MapAdminEndpoints();
app.MapAuthorizationEndpoints();
app.MapOrganizationRbacEndpoints();
app.MapAuditEndpoints();
app.MapPrivacyComplianceEndpoints();
app.MapLocalValidationEndpoints();

// Phase marker: P10-WP08-phase-10-closeout

app.Run();

public partial class Program;
