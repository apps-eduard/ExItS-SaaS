using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
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
        MapCatalogPlansEndpoints(catalog);
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
            PlatformAuthz authz,
            ProductStatus? status,
            string? search,
            string? sortBy,
            bool? sortDesc,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(Product),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            CatalogListSortBy? parsedSort = null;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<CatalogListSortBy>(sortBy, ignoreCase: true, out var sortValue))
                {
                    return CatalogResults.Problem(
                        DomainErrorCodes.InvalidProductCode,
                        $"Unrecognized sort field '{sortBy}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedSort = sortValue;
            }

            var result = await queries
                .ListProductsAsync(status, page, pageSize, search, parsedSort, sortDesc, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        products.MapGet("/{id:guid}", async (
            Guid id,
            CatalogQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(Product),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var product = await queries.GetProductByIdAsync(id, ct).ConfigureAwait(false);
            return product is null ? CatalogResults.NotFound("Product was not found.") : Results.Ok(product);
        });

        products.MapPost("/", async (
            CreateProductRequest body,
            CreateProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogProductCreated,
                nameof(Product),
                body.Code,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body.Code, body.DisplayName, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogProductCreated,
                    nameof(Product),
                    result.Value!.Id.Value.ToString("D"),
                    productCode: result.Value.Code.Value,
                    summary: $"Created catalog product {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Created(
                $"/api/v1/platform/catalog/products/{p.Id.Value}",
                MapProduct(p)));
        });

        products.MapPatch("/{id:guid}/rename", async (
            Guid id,
            RenameRequest body,
            RenameProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogProductUpdated,
                nameof(Product),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(ProductId.From(id), body.DisplayName, body.ExpectedUpdatedAtUtc, ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogProductUpdated,
                    nameof(Product),
                    id.ToString("D"),
                    productCode: result.Value!.Code.Value,
                    summary: $"Renamed catalog product {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Ok(MapProduct(p)));
        });

        products.MapPost("/{id:guid}/activate", async (
            Guid id,
            ActivateProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogProductActivated,
                nameof(Product),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(ProductId.From(id), ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogProductActivated,
                    nameof(Product),
                    id.ToString("D"),
                    productCode: result.Value!.Code.Value,
                    summary: $"Activated catalog product {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Ok(MapProduct(p)));
        });

        products.MapPost("/{id:guid}/deactivate", async (
            Guid id,
            DeactivateProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogProductDeactivated,
                nameof(Product),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(ProductId.From(id), ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogProductDeactivated,
                    nameof(Product),
                    id.ToString("D"),
                    productCode: result.Value!.Code.Value,
                    summary: $"Deactivated catalog product {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Ok(MapProduct(p)));
        });

        products.MapPost("/{id:guid}/retire", async (
            Guid id,
            RetireProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogProductRetired,
                nameof(Product),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(ProductId.From(id), ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogProductRetired,
                    nameof(Product),
                    id.ToString("D"),
                    productCode: result.Value!.Code.Value,
                    summary: $"Retired catalog product {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Ok(MapProduct(p)));
        });
    }

    private static void MapCatalogPlansEndpoints(RouteGroupBuilder catalog)
    {
        var plans = catalog.MapGroup("/plans");

        plans.MapGet("/", async (
            CatalogQueryService queries,
            PlatformAuthz authz,
            string? productCode,
            PlanStatus? status,
            string? search,
            string? sortBy,
            bool? sortDesc,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(Plan),
                "list",
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            CatalogListSortBy? parsedSort = null;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<CatalogListSortBy>(sortBy, ignoreCase: true, out var sortValue))
                {
                    return CatalogResults.Problem(
                        DomainErrorCodes.InvalidPlanCode,
                        $"Unrecognized sort field '{sortBy}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsedSort = sortValue;
            }

            var result = await queries
                .ListPlansAsync(productCode, status, page, pageSize, search, parsedSort, sortDesc, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        plans.MapGet("/{planId:guid}", async (
            Guid planId,
            CatalogQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(Plan),
                planId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var plan = await queries.GetPlanByIdAsync(planId, ct).ConfigureAwait(false);
            return plan is null ? CatalogResults.NotFound("Plan was not found.") : Results.Ok(plan);
        });
    }

    private static void MapFeatureEndpoints(RouteGroupBuilder catalog)
    {
        var features = catalog.MapGroup("/products/{productCode}/features");

        features.MapGet("/", async (
            string productCode,
            CatalogQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(FeatureDefinition),
                productCode,
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

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
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(FeatureDefinition),
                body.FeatureCode,
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

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

            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogProductUpdated,
                    nameof(FeatureDefinition),
                    result.Value!.Code.Value,
                    productCode: productCode,
                    summary: $"Created feature {result.Value.Code.Value} for product {productCode}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, f => Results.Created(
                $"/api/v1/platform/catalog/products/{productCode}/features/{f.Code.Value}",
                MapFeature(f)));
        });

        features.MapPost("/{featureCode}/retire", async (
            string productCode,
            string featureCode,
            RetireFeatureDefinition useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(FeatureDefinition),
                featureCode,
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(productCode, featureCode, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogProductUpdated,
                    nameof(FeatureDefinition),
                    featureCode,
                    productCode: productCode,
                    summary: $"Retired feature {featureCode} for product {productCode}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, f => Results.Ok(MapFeature(f)));
        });
    }

    private static void MapPlanEndpoints(RouteGroupBuilder catalog)
    {
        var plans = catalog.MapGroup("/products/{productCode}/plans");

        plans.MapGet("/", async (
            string productCode,
            CatalogQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(Plan),
                productCode,
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

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

        plans.MapGet("/{planId:guid}", async (
            Guid planId,
            CatalogQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(Plan),
                planId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var plan = await queries.GetPlanByIdAsync(planId, ct).ConfigureAwait(false);
            return plan is null ? CatalogResults.NotFound("Plan was not found.") : Results.Ok(plan);
        });

        plans.MapPost("/", async (
            string productCode,
            CreatePlanRequest body,
            CreatePlan useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogPlanCreated,
                nameof(Plan),
                body.Code,
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(
                productCode,
                body.Code,
                body.DisplayName,
                body.Description,
                body.MaxBranches,
                body.MaxActiveStaff,
                body.CustomerCreditEnabled,
                body.AdvancedReportsEnabled,
                body.ExportEnabled,
                body.TrialAllowed,
                body.DefaultTrialDays,
                body.SortOrder,
                body.MonthlyPrice,
                body.AnnualPrice,
                body.CurrencyCode,
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogPlanCreated,
                    nameof(Plan),
                    result.Value!.Id.Value.ToString("D"),
                    productCode: productCode,
                    summary: $"Created plan {result.Value.Code.Value} for product {productCode}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Created(
                $"/api/v1/platform/catalog/products/{productCode}/plans/{p.Id.Value}",
                MapPlan(p)));
        });

        plans.MapPatch("/{planId:guid}/rename", async (
            Guid planId,
            RenameRequest body,
            RenamePlan useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogPlanUpdated,
                nameof(Plan),
                planId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(PlanId.From(planId), body.DisplayName, body.ExpectedUpdatedAtUtc, ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogPlanUpdated,
                    nameof(Plan),
                    planId.ToString("D"),
                    productCode: result.Value!.ProductCode.Value,
                    summary: $"Renamed plan {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Ok(MapPlan(p)));
        });

        plans.MapPatch("/{planId:guid}/commercial", async (
            Guid planId,
            UpdatePlanCommercialRequest body,
            UpdatePlanCommercialPackage useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogPlanUpdated,
                nameof(Plan),
                planId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(
                PlanId.From(planId),
                body.DisplayName,
                body.Description,
                body.MaxBranches,
                body.MaxActiveStaff,
                body.CustomerCreditEnabled,
                body.AdvancedReportsEnabled,
                body.ExportEnabled,
                body.TrialAllowed,
                body.DefaultTrialDays,
                body.SortOrder,
                body.MonthlyPrice,
                body.AnnualPrice,
                body.CurrencyCode,
                body.ExpectedUpdatedAtUtc,
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogPlanUpdated,
                    nameof(Plan),
                    planId.ToString("D"),
                    productCode: result.Value!.ProductCode.Value,
                    summary: $"Updated commercial package for plan {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Ok(MapPlan(p)));
        });

        plans.MapPost("/{planId:guid}/activate", async (
            Guid planId,
            ActivatePlan useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogPlanActivated,
                nameof(Plan),
                planId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(PlanId.From(planId), ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogPlanActivated,
                    nameof(Plan),
                    planId.ToString("D"),
                    productCode: result.Value!.ProductCode.Value,
                    summary: $"Activated plan {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Ok(MapPlan(p)));
        });

        plans.MapPost("/{planId:guid}/deactivate", async (
            Guid planId,
            DeactivatePlan useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogPlanUpdated,
                nameof(Plan),
                planId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(PlanId.From(planId), ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogPlanUpdated,
                    nameof(Plan),
                    planId.ToString("D"),
                    productCode: result.Value!.ProductCode.Value,
                    summary: $"Deactivated plan {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Ok(MapPlan(p)));
        });

        plans.MapPost("/{planId:guid}/retire", async (
            Guid planId,
            RetirePlan useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogPlanRetired,
                nameof(Plan),
                planId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(PlanId.From(planId), ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogPlanRetired,
                    nameof(Plan),
                    planId.ToString("D"),
                    productCode: result.Value!.ProductCode.Value,
                    summary: $"Retired plan {result.Value.Code.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, p => Results.Ok(MapPlan(p)));
        });

        var versions = plans.MapGroup("/{planId:guid}/versions");

        versions.MapGet("/", async (
            Guid planId,
            CatalogQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlanVersion),
                planId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var items = await queries.ListPlanVersionsAsync(planId, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        versions.MapGet("/{versionNumber:int}", async (
            Guid planId,
            int versionNumber,
            CatalogQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(PlanVersion),
                $"{planId:D}:{versionNumber}",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var version = await queries.GetPlanVersionByNumberAsync(planId, versionNumber, ct).ConfigureAwait(false);
            return version is null
                ? CatalogResults.NotFound("Plan version was not found.")
                : Results.Ok(version);
        });

        versions.MapPost("/draft", async (
            Guid planId,
            CreateDraftPlanVersionRequest body,
            CreateDraftPlanVersion useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogPlanUpdated,
                nameof(PlanVersion),
                planId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

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

            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogPlanUpdated,
                    nameof(PlanVersion),
                    $"{planId:D}:{result.Value!.VersionNumber}",
                    productCode: result.Value.ProductCode.Value,
                    summary: $"Created draft plan version {result.Value.VersionNumber}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

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
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogPlanUpdated,
                nameof(PlanVersion),
                $"{planId:D}:{versionNumber}",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

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

                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.CatalogPlanUpdated,
                        nameof(PlanVersion),
                        $"{planId:D}:{versionNumber}",
                        productCode: result.Value!.ProductCode.Value,
                        summary: $"Updated feature grant {featureCode} on plan version {versionNumber}.",
                        cancellationToken: ct).ConfigureAwait(false);
                }

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
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.CatalogPlanVersionPublished,
                nameof(PlanVersion),
                $"{planId:D}:{versionNumber}",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(PlanId.From(planId), versionNumber, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogPlanVersionPublished,
                    nameof(PlanVersion),
                    $"{planId:D}:{versionNumber}",
                    productCode: result.Value!.ProductCode.Value,
                    summary: $"Published plan version {versionNumber}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, v => Results.Ok(MapPlanVersion(v)));
        });
    }

    private static void MapTrialEndpoints(RouteGroupBuilder catalog)
    {
        var trials = catalog.MapGroup("/products/{productCode}/trials");

        trials.MapGet("/", async (
            string productCode,
            CatalogQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(TrialDefinition),
                productCode,
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

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
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(TrialDefinition),
                productCode,
                productCode: productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

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

            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogProductUpdated,
                    nameof(TrialDefinition),
                    result.Value!.Id.Value.ToString("D"),
                    productCode: productCode,
                    summary: $"Created trial definition for product {productCode}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return CatalogResults.FromResult(result, t => Results.Created(
                $"/api/v1/platform/catalog/products/{productCode}/trials/{t.Id.Value}",
                MapTrial(t)));
        });

        trials.MapPost("/{trialId:guid}/retire", async (
            Guid trialId,
            RetireTrialDefinition useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(TrialDefinition),
                trialId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(TrialDefinitionId.From(trialId), ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogProductUpdated,
                    nameof(TrialDefinition),
                    trialId.ToString("D"),
                    productCode: result.Value!.ProductCode.Value,
                    summary: $"Retired trial definition for product {result.Value.ProductCode.Value}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

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
        updatedAtUtc = plan.UpdatedAtUtc,
        planKey = plan.PlanKey,
        description = plan.Description,
        maxBranches = plan.MaxBranches,
        maxActiveStaff = plan.MaxActiveStaff,
        customerCreditEnabled = plan.CustomerCreditEnabled,
        advancedReportsEnabled = plan.AdvancedReportsEnabled,
        exportEnabled = plan.ExportEnabled,
        trialAllowed = plan.TrialAllowed,
        defaultTrialDays = plan.DefaultTrialDays,
        sortOrder = plan.SortOrder,
        monthlyPrice = plan.MonthlyPrice,
        annualPrice = plan.AnnualPrice,
        currencyCode = plan.CurrencyCode
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
internal sealed record RenameRequest(string DisplayName, DateTimeOffset? ExpectedUpdatedAtUtc);
internal sealed record CreateFeatureRequest(string FeatureCode, string DisplayName, string ValueType);
internal sealed record CreatePlanRequest(
    string Code,
    string DisplayName,
    string? Description = null,
    int MaxBranches = 1,
    int MaxActiveStaff = 3,
    bool CustomerCreditEnabled = false,
    bool AdvancedReportsEnabled = false,
    bool ExportEnabled = false,
    bool TrialAllowed = true,
    int DefaultTrialDays = 14,
    int SortOrder = 100,
    decimal MonthlyPrice = 0m,
    decimal AnnualPrice = 0m,
    string CurrencyCode = "PHP");
internal sealed record UpdatePlanCommercialRequest(
    string DisplayName,
    string? Description,
    int MaxBranches,
    int MaxActiveStaff,
    bool CustomerCreditEnabled,
    bool AdvancedReportsEnabled,
    bool ExportEnabled,
    bool TrialAllowed,
    int DefaultTrialDays,
    int SortOrder,
    decimal MonthlyPrice,
    decimal AnnualPrice,
    string CurrencyCode,
    DateTimeOffset? ExpectedUpdatedAtUtc = null);
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
            or ApplicationErrorCodes.DuplicateFeatureCode
            or ApplicationErrorCodes.ConcurrencyConflict
            or ApplicationErrorCodes.SubscriptionIneligible
            or ApplicationErrorCodes.ProductNotActive => StatusCodes.Status409Conflict,

        _ when errorCode.Contains(".duplicate", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("conflict", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,
        _ when errorCode.Contains("invalid_transition", StringComparison.OrdinalIgnoreCase) => StatusCodes.Status409Conflict,

        _ => StatusCodes.Status400BadRequest
    };
}
