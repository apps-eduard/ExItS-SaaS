using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Api.GlobalCatalog;

internal static class GlobalCatalogEndpoints
{
    public static IEndpointRouteBuilder MapGlobalCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/api/v1/platform/global-catalog");
        MapCategoryEndpoints(root);
        MapProductEndpoints(root);
        MapImportEndpoints(root);
        MapTemplateEndpoints(root);
        return app;
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder root)
    {
        var categories = root.MapGroup("/categories");

        categories.MapGet("/", async (
            GlobalCategoryQueryService queries,
            PlatformAuthz authz,
            GlobalCategoryStatus? status,
            Guid? parentId,
            BusinessType? businessType,
            string? search,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(GlobalCategory),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries
                .ListAsync(status, parentId, businessType, search, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        categories.MapGet("/{id:guid}", async (
            Guid id,
            GlobalCategoryQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(GlobalCategory),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var category = await queries.GetByIdAsync(id, ct).ConfigureAwait(false);
            return category is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.GlobalCategoryNotFound,
                    "Category was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(category);
        });

        categories.MapPost("/", async (
            CreateGlobalCategoryRequest body,
            CreateGlobalCategory useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalCategories,
                PlatformAuditActions.GlobalCategoryCreated,
                nameof(GlobalCategory),
                body.Name,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalCategoryCreated,
                    nameof(GlobalCategory),
                    result.Value!.Id.ToString("D"),
                    summary: $"Created global category {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                c => Results.Created($"/api/v1/platform/global-catalog/categories/{c.Id}", c));
        });

        categories.MapPut("/{id:guid}", async (
            Guid id,
            UpdateGlobalCategoryRequest body,
            UpdateGlobalCategory useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalCategories,
                PlatformAuditActions.GlobalCategoryUpdated,
                nameof(GlobalCategory),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalCategoryUpdated,
                    nameof(GlobalCategory),
                    result.Value!.Id.ToString("D"),
                    summary: $"Updated global category {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        categories.MapPatch("/{id:guid}/status", async (
            Guid id,
            SetGlobalCategoryStatusRequest body,
            SetGlobalCategoryStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalCategories,
                PlatformAuditActions.GlobalCategoryStatusChanged,
                nameof(GlobalCategory),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalCategoryStatusChanged,
                    nameof(GlobalCategory),
                    result.Value!.Id.ToString("D"),
                    summary: $"Changed global category status to {result.Value.Status}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
    }

    private static void MapProductEndpoints(RouteGroupBuilder root)
    {
        var products = root.MapGroup("/products");

        products.MapGet("/", async (
            GlobalProductQueryService queries,
            PlatformAuthz authz,
            GlobalProductStatus? status,
            Guid? categoryId,
            BusinessType? businessType,
            string? search,
            string? barcode,
            string? sku,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(GlobalProduct),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries
                .ListAsync(status, categoryId, businessType, search, barcode, sku, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        products.MapGet("/{id:guid}", async (
            Guid id,
            GlobalProductQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(GlobalProduct),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var product = await queries.GetByIdAsync(id, ct).ConfigureAwait(false);
            return product is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.GlobalProductNotFound,
                    "Product was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(product);
        });

        products.MapPost("/", async (
            CreateGlobalProductRequest body,
            CreateGlobalProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalProducts,
                PlatformAuditActions.GlobalProductCreated,
                nameof(GlobalProduct),
                body.Name,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalProductCreated,
                    nameof(GlobalProduct),
                    result.Value!.Id.ToString("D"),
                    summary: $"Created global product {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                p => Results.Created($"/api/v1/platform/global-catalog/products/{p.Id}", p));
        });

        products.MapPut("/{id:guid}", async (
            Guid id,
            UpdateGlobalProductRequest body,
            UpdateGlobalProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalProducts,
                PlatformAuditActions.GlobalProductUpdated,
                nameof(GlobalProduct),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalProductUpdated,
                    nameof(GlobalProduct),
                    result.Value!.Id.ToString("D"),
                    summary: $"Updated global product {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        products.MapPatch("/{id:guid}/status", async (
            Guid id,
            SetGlobalProductStatusRequest body,
            SetGlobalProductStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalProducts,
                PlatformAuditActions.GlobalProductStatusChanged,
                nameof(GlobalProduct),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalProductStatusChanged,
                    nameof(GlobalProduct),
                    result.Value!.Id.ToString("D"),
                    summary: $"Changed global product status to {result.Value.Status}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
    }

    private static void MapImportEndpoints(RouteGroupBuilder root)
    {
        var imports = root.MapGroup("/products/imports");

        imports.MapGet("/", async (
            CatalogImportQueryService queries,
            PlatformAuthz authz,
            CatalogImportJobStatus? status,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ImportGlobalProducts,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(CatalogImportJob),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries.ListAsync(status, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        imports.MapPost("/", async (
            HttpRequest request,
            CreateCatalogImport useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ImportGlobalProducts,
                PlatformAuditActions.CatalogImportCreated,
                nameof(CatalogImportJob),
                "upload",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            if (!request.HasFormContentType)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportUnsupportedType,
                    "multipart/form-data upload is required.",
                    StatusCodes.Status400BadRequest);
            }

            var form = await request.ReadFormAsync(ct).ConfigureAwait(false);
            var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
            if (file is null || file.Length <= 0)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportEmpty,
                    "A non-empty file field named 'file' is required.",
                    StatusCodes.Status400BadRequest);
            }

            var idempotencyKey = form["idempotencyKey"].FirstOrDefault()
                ?? request.Headers["Idempotency-Key"].FirstOrDefault();

            await using var stream = file.OpenReadStream();
            var result = await useCase
                .ExecuteAsync(stream, file.FileName, file.ContentType, file.Length, idempotencyKey, ct)
                .ConfigureAwait(false);

            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogImportCreated,
                    nameof(CatalogImportJob),
                    result.Value!.Id.ToString("D"),
                    summary: $"Uploaded catalog import {result.Value.FileName} ({result.Value.TotalCount} rows).",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                job => Results.Created($"/api/v1/platform/global-catalog/products/imports/{job.Id}", job));
        }).DisableAntiforgery();

        imports.MapGet("/{jobId:guid}", async (
            Guid jobId,
            CatalogImportQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ImportGlobalProducts,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(CatalogImportJob),
                jobId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var job = await queries.GetByIdAsync(jobId, includePreview: true, ct).ConfigureAwait(false);
            return job is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportJobNotFound,
                    "Import job was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(job);
        });

        imports.MapPost("/{jobId:guid}/confirm", async (
            Guid jobId,
            ConfirmCatalogImport useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ImportGlobalProducts,
                PlatformAuditActions.CatalogImportConfirmed,
                nameof(CatalogImportJob),
                jobId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(jobId, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogImportConfirmed,
                    nameof(CatalogImportJob),
                    result.Value!.Id.ToString("D"),
                    summary: $"Confirmed catalog import {result.Value.Id:D}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        imports.MapGet("/{jobId:guid}/errors", async (
            Guid jobId,
            CatalogImportQueryService queries,
            PlatformAuthz authz,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ImportGlobalProducts,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(CatalogImportJob),
                jobId.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var job = await queries.GetByIdAsync(jobId, includePreview: false, ct).ConfigureAwait(false);
            if (job is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportJobNotFound,
                    "Import job was not found.",
                    StatusCodes.Status404NotFound);
            }

            var errors = await queries.ListErrorsAsync(jobId, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(errors);
        });
    }

    private static void MapTemplateEndpoints(RouteGroupBuilder root)
    {
        var templates = root.MapGroup("/templates");

        templates.MapGet("/", async (
            CatalogTemplateQueryService queries,
            PlatformAuthz authz,
            CatalogTemplateStatus? status,
            BusinessType? primaryBusinessType,
            string? search,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(CatalogTemplate),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await queries
                .ListAsync(status, primaryBusinessType, search, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        templates.MapGet("/{id:guid}", async (
            Guid id,
            CatalogTemplateQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(CatalogTemplate),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var template = await queries.GetByIdAsync(id, ct).ConfigureAwait(false);
            return template is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.CatalogTemplateNotFound,
                    "Template was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(template);
        });

        templates.MapPost("/", async (
            CreateCatalogTemplateRequest body,
            CreateCatalogTemplate useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalogTemplates,
                PlatformAuditActions.CatalogTemplateCreated,
                nameof(CatalogTemplate),
                body.Name,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogTemplateCreated,
                    nameof(CatalogTemplate),
                    result.Value!.Id.ToString("D"),
                    summary: $"Created catalog template {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                t => Results.Created($"/api/v1/platform/global-catalog/templates/{t.Id}", t));
        });

        templates.MapPut("/{id:guid}", async (
            Guid id,
            UpdateCatalogTemplateRequest body,
            UpdateCatalogTemplate useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalogTemplates,
                PlatformAuditActions.CatalogTemplateUpdated,
                nameof(CatalogTemplate),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogTemplateUpdated,
                    nameof(CatalogTemplate),
                    result.Value!.Id.ToString("D"),
                    summary: $"Updated catalog template {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        templates.MapPost("/{id:guid}/publish", async (
            Guid id,
            CatalogTemplateLifecycleRequest? body,
            PublishCatalogTemplate useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.PublishCatalogTemplates,
                PlatformAuditActions.CatalogTemplatePublished,
                nameof(CatalogTemplate),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogTemplatePublished,
                    nameof(CatalogTemplate),
                    result.Value!.Id.ToString("D"),
                    summary: $"Published catalog template {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        templates.MapPost("/{id:guid}/unpublish", async (
            Guid id,
            CatalogTemplateLifecycleRequest? body,
            UnpublishCatalogTemplate useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.PublishCatalogTemplates,
                PlatformAuditActions.CatalogTemplateUnpublished,
                nameof(CatalogTemplate),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogTemplateUnpublished,
                    nameof(CatalogTemplate),
                    result.Value!.Id.ToString("D"),
                    summary: $"Unpublished catalog template {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        templates.MapPost("/{id:guid}/archive", async (
            Guid id,
            CatalogTemplateLifecycleRequest? body,
            ArchiveCatalogTemplate useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.PublishCatalogTemplates,
                PlatformAuditActions.CatalogTemplateArchived,
                nameof(CatalogTemplate),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogTemplateArchived,
                    nameof(CatalogTemplate),
                    result.Value!.Id.ToString("D"),
                    summary: $"Archived catalog template {result.Value.Name}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        templates.MapPost("/{id:guid}/products", async (
            Guid id,
            AssignCatalogTemplateProductRequest body,
            AssignCatalogTemplateProduct useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalogTemplates,
                PlatformAuditActions.CatalogTemplateCompositionChanged,
                nameof(CatalogTemplate),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogTemplateCompositionChanged,
                    nameof(CatalogTemplate),
                    result.Value!.Id.ToString("D"),
                    summary: $"Assigned product {body.GlobalProductId:D} to template.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        templates.MapPut("/{id:guid}/products/order", async (
            Guid id,
            ReorderCatalogTemplateProductsRequest body,
            ReorderCatalogTemplateProducts useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalogTemplates,
                PlatformAuditActions.CatalogTemplateCompositionChanged,
                nameof(CatalogTemplate),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogTemplateCompositionChanged,
                    nameof(CatalogTemplate),
                    result.Value!.Id.ToString("D"),
                    summary: "Reordered template products.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        templates.MapPatch("/{id:guid}/products/{productId:guid}", async (
            Guid id,
            Guid productId,
            UpdateCatalogTemplateProductFlagsRequest body,
            UpdateCatalogTemplateProductFlags useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalogTemplates,
                PlatformAuditActions.CatalogTemplateCompositionChanged,
                nameof(CatalogTemplate),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, productId, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogTemplateCompositionChanged,
                    nameof(CatalogTemplate),
                    result.Value!.Id.ToString("D"),
                    summary: $"Updated flags for product {productId:D}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        templates.MapDelete("/{id:guid}/products/{productId:guid}", async (
            Guid id,
            Guid productId,
            RemoveCatalogTemplateProduct useCase,
            PlatformAuthz authz,
            DateTimeOffset? expectedUpdatedAtUtc,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageCatalogTemplates,
                PlatformAuditActions.CatalogTemplateCompositionChanged,
                nameof(CatalogTemplate),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(id, productId, expectedUpdatedAtUtc, ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogTemplateCompositionChanged,
                    nameof(CatalogTemplate),
                    result.Value!.Id.ToString("D"),
                    summary: $"Removed product {productId:D} from template.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
    }
}
