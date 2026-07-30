using ExItS.Platform.Api.Access;
using ExItS.Platform.Api.Admin;
using ExItS.Platform.Api.Audit;
using ExItS.Platform.Api.Authorization;
using ExItS.Platform.Api.Catalog;
using ExItS.Platform.Api.Common;
using ExItS.Platform.Api.Entitlements;
using ExItS.Platform.Api.Identity;
using ExItS.Platform.Api.Organizations;
using ExItS.Platform.Api.Payments;
using ExItS.Platform.Api.Subscriptions;
using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Admin;
using ExItS.Platform.Application.Audit;
using ExItS.Platform.Application.Authorization;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Identity;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddPlatformPersistence(builder.Configuration);

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
builder.Services.AddScoped<RetirePlan>();
builder.Services.AddScoped<CreateDraftPlanVersion>();
builder.Services.AddScoped<ReplaceDraftPlanVersionGrants>();
builder.Services.AddScoped<UpsertDraftFeatureGrant>();
builder.Services.AddScoped<PublishExistingPlanVersion>();
builder.Services.AddScoped<PublishPlanVersion>();
builder.Services.AddScoped<CreateTrialDefinition>();
builder.Services.AddScoped<RetireTrialDefinition>();

builder.Services.AddScoped<OrganizationQueryService>();
builder.Services.AddScoped<CreatePlatformOrganization>();
builder.Services.AddScoped<SuspendPlatformOrganization>();

builder.Services.AddScoped<PlatformUserQueryService>();
builder.Services.AddScoped<CreatePlatformUser>();
builder.Services.AddScoped<UpdatePlatformUserProfile>();
builder.Services.AddScoped<SuspendPlatformUser>();
builder.Services.AddScoped<ReactivatePlatformUser>();
builder.Services.AddScoped<DeactivatePlatformUser>();

builder.Services.AddScoped<MembershipQueryService>();
builder.Services.AddScoped<AddOrganizationMembership>();
builder.Services.AddScoped<ChangeOrganizationRole>();
builder.Services.AddScoped<SuspendOrganizationMembership>();
builder.Services.AddScoped<ReactivateOrganizationMembership>();
builder.Services.AddScoped<RevokeOrganizationMembership>();

builder.Services.AddScoped<ProductAccessQueryService>();
builder.Services.AddScoped<GrantProductAccess>();
builder.Services.AddScoped<RevokeProductAccess>();
builder.Services.AddScoped<EvaluateEffectiveProductAccess>();

builder.Services.AddScoped<SubscriptionQueryService>();
builder.Services.AddScoped<StartTrialSubscription>();
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
builder.Services.AddScoped<QueryAuditRecords>();
builder.Services.AddScoped<GetAuditRecord>();
builder.Services.AddScoped<PlatformAuthz>();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapGet("/", () => Results.Json(new
{
    service = "ExItS.Platform.Api",
    status = "ok",
    phase = "P5-WP05-authentication-onboarding-closeout"
}));

app.MapHealthChecks("/health");
app.MapCatalogEndpoints();
app.MapOrganizationEndpoints();
app.MapIdentityEndpoints();
app.MapMembershipEndpoints();
app.MapAccessEndpoints();
app.MapSubscriptionEndpoints();
app.MapPaymentEndpoints();
app.MapEntitlementEndpoints();
app.MapAdminEndpoints();
app.MapAuthorizationEndpoints();
app.MapAuditEndpoints();

app.Run();

public partial class Program;
