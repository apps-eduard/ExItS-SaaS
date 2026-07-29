using ExItS.Platform.Api.Catalog;
using ExItS.Platform.Application.Catalog;
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

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapGet("/", () => Results.Json(new
{
    service = "ExItS.Platform.Api",
    status = "ok",
    phase = "P3-WP01-product-plan-catalog"
}));

app.MapHealthChecks("/health");
app.MapCatalogEndpoints();

app.Run();

public partial class Program;
