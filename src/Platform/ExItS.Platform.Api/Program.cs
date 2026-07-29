using ExItS.Platform.Api.Catalog;
using ExItS.Platform.Api.Entitlements;
using ExItS.Platform.Api.Organizations;
using ExItS.Platform.Api.Payments;
using ExItS.Platform.Api.Subscriptions;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddPlatformPersistence(builder.Configuration);

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

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapGet("/", () => Results.Json(new
{
    service = "ExItS.Platform.Api",
    status = "ok",
    phase = "P3-WP05-billing-closeout"
}));

app.MapHealthChecks("/health");
app.MapCatalogEndpoints();
app.MapOrganizationEndpoints();
app.MapSubscriptionEndpoints();
app.MapPaymentEndpoints();
app.MapEntitlementEndpoints();

app.Run();

public partial class Program;
