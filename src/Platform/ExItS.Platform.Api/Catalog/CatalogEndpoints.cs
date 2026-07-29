using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Api.Catalog;

internal static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var catalog = app.MapGroup("/api/v1/platform/catalog");

        MapProductEndpoints(catalog);
        MapFeatureEndpoints(catalog);
        MapPlanEndpoints(catalog);
        MapTrialEndpoints(catalog);

        return app;
    }

    private static void MapProductEndpoints(RouteGroupBuilder catalog)
    {
        var products = catalog.MapGroup("/products");

        products.MapGet("/", async (
            CatalogQueryService queries,
            ProductStatus? status,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var result = await queries.ListProductsAsync(status, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        products.MapGet("/{id:guid}", async (Guid id, CatalogQueryService queries, CancellationToken ct) =>
        {
            var product = await queries.GetProductByIdAsync(id, ct).ConfigureAwait(false);
            return product is null ? CatalogResults.NotFound("Product was not found.") : Results.Ok(product);
        });

        products.MapPost("/", async (CreateProductRequest body, CreateProduct useCase, CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(body.Code, body.DisplayName, ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, p => Results.Created($"/api/v1/platform/catalog/products/{p.Id.Value}", MapProduct(p)));
        });

        products.MapPatch("/{id:guid}/rename", async (Guid id, RenameRequest body, RenameProduct useCase, CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(ProductId.From(id), body.DisplayName, ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, p => Results.Ok(MapProduct(p)));
        });

        products.MapPost("/{id:guid}/activate", async (Guid id, ActivateProduct useCase, CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(ProductId.From(id), ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, p => Results.Ok(MapProduct(p)));
        });

        products.MapPost("/{id:guid}/deactivate", async (Guid id, DeactivateProduct useCase, CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(ProductId.From(id), ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, p => Results.Ok(MapProduct(p)));
        });

        products.MapPost("/{id:guid}/retire", async (Guid id, RetireProduct useCase, CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(ProductId.From(id), ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, p => Results.Ok(MapProduct(p)));
        });
    }

    private static void MapFeatureEndpoints(RouteGroupBuilder catalog)
    {
        var features = catalog.MapGroup("/products/{productCode}/features");

        features.MapGet("/", async (string productCode, CatalogQueryService queries, CancellationToken ct) =>
        {
            try
            {
                var items = await queries.ListFeaturesByProductAsync(productCode, ct).ConfigureAwait(false);
                return Results.Ok(items);
            }
            catch (DomainException ex)
            {
                return CatalogResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        features.MapPost("/", async (
            string productCode,
            CreateFeatureRequest body,
            CreateFeatureDefinition useCase,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<FeatureValueType>(body.ValueType, ignoreCase: true, out var valueType))
            {
                return CatalogResults.Problem(
                    DomainErrorCodes.InvalidFeatureValueType,
                    "Feature value type is not defined.",
                    StatusCodes.Status400BadRequest);
            }

            var result = await useCase.ExecuteAsync(
                productCode,
                body.FeatureCode,
                body.DisplayName,
                valueType,
                ct).ConfigureAwait(false);

            return CatalogResults.FromResult(result, f => Results.Created(
                $"/api/v1/platform/catalog/products/{productCode}/features/{f.Code.Value}",
                MapFeature(f)));
        });

        features.MapPost("/{featureCode}/retire", async (
            string productCode,
            string featureCode,
            RetireFeatureDefinition useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(productCode, featureCode, ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, f => Results.Ok(MapFeature(f)));
        });
    }

    private static void MapPlanEndpoints(RouteGroupBuilder catalog)
    {
        var plans = catalog.MapGroup("/products/{productCode}/plans");

        plans.MapGet("/", async (string productCode, CatalogQueryService queries, CancellationToken ct) =>
        {
            try
            {
                var items = await queries.ListPlansByProductAsync(productCode, ct).ConfigureAwait(false);
                return Results.Ok(items);
            }
            catch (DomainException ex)
            {
                return CatalogResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        plans.MapGet("/{planId:guid}", async (Guid planId, CatalogQueryService queries, CancellationToken ct) =>
        {
            var plan = await queries.GetPlanByIdAsync(planId, ct).ConfigureAwait(false);
            return plan is null ? CatalogResults.NotFound("Plan was not found.") : Results.Ok(plan);
        });

        plans.MapPost("/", async (
            string productCode,
            CreatePlanRequest body,
            CreatePlan useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(productCode, body.Code, body.DisplayName, ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, p => Results.Created(
                $"/api/v1/platform/catalog/products/{productCode}/plans/{p.Id.Value}",
                MapPlan(p)));
        });

        plans.MapPatch("/{planId:guid}/rename", async (
            Guid planId,
            RenameRequest body,
            RenamePlan useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(PlanId.From(planId), body.DisplayName, ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, p => Results.Ok(MapPlan(p)));
        });

        plans.MapPost("/{planId:guid}/activate", async (Guid planId, ActivatePlan useCase, CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(PlanId.From(planId), ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, p => Results.Ok(MapPlan(p)));
        });

        plans.MapPost("/{planId:guid}/retire", async (Guid planId, RetirePlan useCase, CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(PlanId.From(planId), ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, p => Results.Ok(MapPlan(p)));
        });

        var versions = plans.MapGroup("/{planId:guid}/versions");

        versions.MapGet("/", async (Guid planId, CatalogQueryService queries, CancellationToken ct) =>
        {
            var items = await queries.ListPlanVersionsAsync(planId, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        versions.MapGet("/{versionNumber:int}", async (
            Guid planId,
            int versionNumber,
            CatalogQueryService queries,
            CancellationToken ct) =>
        {
            var version = await queries.GetPlanVersionByNumberAsync(planId, versionNumber, ct).ConfigureAwait(false);
            return version is null
                ? CatalogResults.NotFound("Plan version was not found.")
                : Results.Ok(version);
        });

        versions.MapPost("/draft", async (
            Guid planId,
            CreateDraftPlanVersionRequest body,
            CreateDraftPlanVersion useCase,
            CancellationToken ct) =>
        {
            if (!Enum.TryParse<BillingPeriod>(body.BillingPeriod, ignoreCase: true, out var billingPeriod))
            {
                return CatalogResults.Problem(
                    DomainErrorCodes.InvalidPlanVersionNumber,
                    "Billing period is not defined.",
                    StatusCodes.Status400BadRequest);
            }

            var grants = MapGrants(body.Grants);
            var result = await useCase.ExecuteAsync(
                PlanId.From(planId),
                body.VersionNumber,
                billingPeriod,
                body.TrialEligible,
                grants,
                body.EffectiveFromUtc,
                body.EffectiveToUtc,
                ct).ConfigureAwait(false);

            return CatalogResults.FromResult(result, v => Results.Created(
                $"/api/v1/platform/catalog/products/{v.ProductCode.Value}/plans/{planId}/versions/{v.VersionNumber}",
                MapPlanVersion(v)));
        });

        versions.MapPut("/{versionNumber:int}/feature-grants/{featureCode}", async (
            Guid planId,
            int versionNumber,
            string featureCode,
            FeatureGrantRequest body,
            UpsertDraftFeatureGrant useCase,
            CancellationToken ct) =>
        {
            try
            {
                var grant = new FeatureGrantSpec(
                    FeatureCode.Create(featureCode),
                    body.Enabled,
                    body.NumericLimit);

                var result = await useCase.ExecuteAsync(
                    PlanId.From(planId),
                    versionNumber,
                    grant,
                    ct).ConfigureAwait(false);

                return CatalogResults.FromResult(result, v => Results.Ok(MapPlanVersion(v)));
            }
            catch (DomainException ex)
            {
                return CatalogResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        versions.MapPost("/{versionNumber:int}/publish", async (
            Guid planId,
            int versionNumber,
            PublishExistingPlanVersion useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(PlanId.From(planId), versionNumber, ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, v => Results.Ok(MapPlanVersion(v)));
        });
    }

    private static void MapTrialEndpoints(RouteGroupBuilder catalog)
    {
        var trials = catalog.MapGroup("/products/{productCode}/trials");

        trials.MapGet("/", async (string productCode, CatalogQueryService queries, CancellationToken ct) =>
        {
            try
            {
                var items = await queries.ListTrialsByProductAsync(productCode, ct).ConfigureAwait(false);
                return Results.Ok(items);
            }
            catch (DomainException ex)
            {
                return CatalogResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        trials.MapPost("/", async (
            string productCode,
            CreateTrialRequest body,
            CreateTrialDefinition useCase,
            CancellationToken ct) =>
        {
            if (!TryParseDuration(body, out var duration, out var durationError))
            {
                return CatalogResults.Problem(
                    DomainErrorCodes.InvalidTrialDuration,
                    durationError,
                    StatusCodes.Status400BadRequest);
            }

            var featureGrants = MapGrants(body.FeatureGrants);
            var postExpiryGrants = MapGrants(body.PostExpiryFeatureGrants);

            var result = await useCase.ExecuteAsync(
                productCode,
                body.DisplayName,
                duration,
                featureGrants,
                postExpiryGrants,
                body.PlanId,
                ct).ConfigureAwait(false);

            return CatalogResults.FromResult(result, t => Results.Created(
                $"/api/v1/platform/catalog/products/{productCode}/trials/{t.Id.Value}",
                MapTrial(t)));
        });

        trials.MapPost("/{trialId:guid}/retire", async (
            Guid trialId,
            RetireTrialDefinition useCase,
            CancellationToken ct) =>
        {
            var result = await useCase.ExecuteAsync(TrialDefinitionId.From(trialId), ct).ConfigureAwait(false);
            return CatalogResults.FromResult(result, t => Results.Ok(MapTrial(t)));
        });
    }

    private static bool TryParseDuration(CreateTrialRequest body, out TimeSpan duration, out string error)
    {
        if (body.DurationTicks is > 0)
        {
            duration = TimeSpan.FromTicks(body.DurationTicks.Value);
            error = string.Empty;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(body.DurationIso))
        {
            try
            {
                duration = System.Xml.XmlConvert.ToTimeSpan(body.DurationIso);
                error = string.Empty;
                return duration > TimeSpan.Zero;
            }
            catch (FormatException)
            {
                duration = default;
                error = "Duration ISO 8601 duration string is invalid.";
                return false;
            }
        }

        duration = default;
        error = "Duration must be supplied as durationTicks or durationIso.";
        return false;
    }

    private static IReadOnlyList<FeatureGrantSpec> MapGrants(IReadOnlyList<FeatureGrantRequest>? grants) =>
        (grants ?? [])
            .Select(g => new FeatureGrantSpec(
                FeatureCode.Create(g.FeatureCode),
                g.Enabled,
                g.NumericLimit))
            .ToList();

    private static object MapProduct(Product product) => new
    {
        id = product.Id.Value,
        code = product.Code.Value,
        displayName = product.DisplayName,
        status = product.Status.ToString(),
        createdAtUtc = product.CreatedAtUtc,
        updatedAtUtc = product.UpdatedAtUtc
    };

    private static object MapFeature(FeatureDefinition feature) => new
    {
        productCode = feature.ProductCode.Value,
        featureCode = feature.Code.Value,
        displayName = feature.DisplayName,
        valueType = feature.ValueType.ToString(),
        status = feature.Status.ToString(),
        createdAtUtc = feature.CreatedAtUtc,
        updatedAtUtc = feature.UpdatedAtUtc
    };

    private static object MapPlan(Plan plan) => new
    {
        id = plan.Id.Value,
        productCode = plan.ProductCode.Value,
        code = plan.Code.Value,
        displayName = plan.DisplayName,
        status = plan.Status.ToString(),
        createdAtUtc = plan.CreatedAtUtc,
        updatedAtUtc = plan.UpdatedAtUtc
    };

    private static object MapPlanVersion(PlanVersion version) => new
    {
        id = version.Id.Value,
        planId = version.PlanId.Value,
        productCode = version.ProductCode.Value,
        versionNumber = version.VersionNumber,
        effectiveFromUtc = version.EffectiveFromUtc,
        effectiveToUtc = version.EffectiveToUtc,
        billingPeriod = version.BillingPeriod.ToString(),
        trialEligible = version.TrialEligible,
        status = version.Status.ToString(),
        createdAtUtc = version.CreatedAtUtc,
        updatedAtUtc = version.UpdatedAtUtc,
        grants = version.Grants.Select(g => new
        {
            featureCode = g.FeatureCode.Value,
            enabled = g.Enabled,
            numericLimit = g.NumericLimit
        })
    };

    private static object MapTrial(TrialDefinition trial) => new
    {
        id = trial.Id.Value,
        productCode = trial.ProductCode.Value,
        planId = trial.PlanId?.Value,
        displayName = trial.DisplayName,
        durationTicks = trial.Duration.Ticks,
        durationIso = System.Xml.XmlConvert.ToString(trial.Duration),
        status = trial.Status.ToString(),
        createdAtUtc = trial.CreatedAtUtc,
        updatedAtUtc = trial.UpdatedAtUtc,
        featureGrants = trial.FeatureGrants.Select(g => new
        {
            featureCode = g.FeatureCode.Value,
            enabled = g.Enabled,
            numericLimit = g.NumericLimit
        }),
        postExpiryFeatureGrants = trial.PostExpiryFeatureGrants.Select(g => new
        {
            featureCode = g.FeatureCode.Value,
            enabled = g.Enabled,
            numericLimit = g.NumericLimit
        })
    };
}

internal sealed record CreateProductRequest(string Code, string DisplayName);
internal sealed record RenameRequest(string DisplayName);
internal sealed record CreateFeatureRequest(string FeatureCode, string DisplayName, string ValueType);
internal sealed record CreatePlanRequest(string Code, string DisplayName);
internal sealed record FeatureGrantRequest(string FeatureCode, bool Enabled, int? NumericLimit = null);

internal sealed record CreateDraftPlanVersionRequest(
    int VersionNumber,
    string BillingPeriod,
    bool TrialEligible,
    IReadOnlyList<FeatureGrantRequest>? Grants,
    DateTimeOffset? EffectiveFromUtc = null,
    DateTimeOffset? EffectiveToUtc = null);

internal sealed record CreateTrialRequest(
    string DisplayName,
    long? DurationTicks = null,
    string? DurationIso = null,
    Guid? PlanId = null,
    IReadOnlyList<FeatureGrantRequest>? FeatureGrants = null,
    IReadOnlyList<FeatureGrantRequest>? PostExpiryFeatureGrants = null);

internal static class CatalogResults
{
    public static IResult FromResult<T>(ApplicationResult<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        return Problem(result.ErrorCode!, result.ErrorMessage!, MapStatusCode(result.ErrorCode!));
    }

    public static IResult NotFound(string detail) =>
        Problem(ApplicationErrorCodes.ProductNotFound, detail, StatusCodes.Status404NotFound);

    public static IResult Problem(string errorCode, string detail, int statusCode) =>
        Results.Problem(
            detail: detail,
            statusCode: statusCode,
            extensions: new Dictionary<string, object?> { ["errorCode"] = errorCode });

    private static int MapStatusCode(string errorCode) => errorCode switch
    {
        ApplicationErrorCodes.ProductNotFound
            or ApplicationErrorCodes.PlanNotFound
            or ApplicationErrorCodes.PlanVersionNotFound
            or ApplicationErrorCodes.FeatureNotFound
            or ApplicationErrorCodes.TrialNotFound => StatusCodes.Status404NotFound,

        ApplicationErrorCodes.DuplicateProductCode
            or ApplicationErrorCodes.DuplicatePlanCode
            or ApplicationErrorCodes.DuplicateFeatureCode => StatusCodes.Status409Conflict,

        _ when errorCode.Contains(".duplicate", StringComparison.Ordinal) => StatusCodes.Status409Conflict,

        _ => StatusCodes.Status400BadRequest
    };
}
