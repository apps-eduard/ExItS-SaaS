using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Api.GlobalCatalog;

internal static class GlobalCatalogEndpoints
{
    public static IEndpointRouteBuilder MapGlobalCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var root = app.MapGroup("/api/v1/platform/global-catalog");
        MapBusinessTypeEndpoints(root);
        MapCategoryEndpoints(root);
        MapProductEndpoints(root);
        MapImportEndpoints(root);
        MapTemplateEndpoints(root);
        return app;
    }

    private static void MapBusinessTypeEndpoints(RouteGroupBuilder root)
    {
        var types = root.MapGroup("/business-types");

        types.MapGet("/", async (
            BusinessTypeQueryService queries,
            PlatformAuthz authz,
            BusinessTypeStatus? status,
            string? search,
            string? sortBy,
            bool? sortDesc,
            int? page,
            int? pageSize,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(BusinessType),
                "list",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            BusinessTypeListSortBy effectiveSort = BusinessTypeListSortBy.SortOrder;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<BusinessTypeListSortBy>(sortBy, ignoreCase: true, out var parsed)
                    || !Enum.IsDefined(parsed))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidGlobalCatalogBusinessType,
                        $"Unrecognized sort field '{sortBy}'.",
                        StatusCodes.Status400BadRequest);
                }

                effectiveSort = parsed;
            }

            var result = await queries
                .ListAsync(
                    status,
                    search,
                    page,
                    pageSize,
                    ct,
                    sortBy: effectiveSort,
                    sortDescending: sortDesc ?? false)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        types.MapGet("/{id:guid}", async (
            Guid id,
            BusinessTypeQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewGlobalCatalog,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(BusinessType),
                id.ToString("D"),
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var entity = await queries.GetByIdAsync(id, ct).ConfigureAwait(false);
            return entity is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.BusinessTypeNotFound,
                    "Business type was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(entity);
        });

        types.MapPost("/", async (
            CreateBusinessTypeRequest body,
            CreateBusinessType useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalCategories,
                PlatformAuditActions.BusinessTypeCreated,
                nameof(BusinessType),
                body.Code,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.BusinessTypeCreated,
                    nameof(BusinessType),
                    result.Value!.Id.ToString("D"),
                    summary: $"Created business type {result.Value.Code}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(
                result,
                b => Results.Created($"/api/v1/platform/global-catalog/business-types/{b.Id}", b));
        });

        types.MapPut("/{id:guid}", async (
            Guid id,
            UpdateBusinessTypeRequest body,
            UpdateBusinessType useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalCategories,
                PlatformAuditActions.BusinessTypeUpdated,
                nameof(BusinessType),
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
                    PlatformAuditActions.BusinessTypeUpdated,
                    nameof(BusinessType),
                    id.ToString("D"),
                    summary: $"Updated business type {result.Value!.Code}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        types.MapPost("/{id:guid}/status", async (
            Guid id,
            SetBusinessTypeStatusRequest body,
            SetBusinessTypeStatus useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageGlobalCategories,
                PlatformAuditActions.BusinessTypeStatusChanged,
                nameof(BusinessType),
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
                    PlatformAuditActions.BusinessTypeStatusChanged,
                    nameof(BusinessType),
                    id.ToString("D"),
                    summary: $"Changed business type status to {result.Value!.Status}.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });
    }

    private static void MapCategoryEndpoints(RouteGroupBuilder root)
    {
        var categories = root.MapGroup("/categories");

        categories.MapGet("/", async (
            GlobalCategoryQueryService queries,
            PlatformAuthz authz,
            GlobalCategoryStatus? status,
            Guid? parentId,
            Guid? businessTypeId,
            string? businessTypeCode,
            string? search,
            string? sortBy,
            bool? sortDesc,
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

            GlobalCategoryListSortBy effectiveSort = GlobalCategoryListSortBy.SortOrder;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<GlobalCategoryListSortBy>(sortBy, ignoreCase: true, out var parsed)
                    || !Enum.IsDefined(parsed))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidGlobalCategorySortField,
                        $"Unrecognized sort field '{sortBy}'.",
                        StatusCodes.Status400BadRequest);
                }

                effectiveSort = parsed;
            }

            var result = await queries
                .ListAsync(
                    status,
                    parentId,
                    businessTypeId,
                    businessTypeCode,
                    search,
                    page,
                    pageSize,
                    ct,
                    sortBy: effectiveSort,
                    sortDescending: sortDesc ?? false)
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

        categories.MapPost("/{id:guid}/business-types", async (
            Guid id,
            BulkAssignCategoryBusinessTypesRequest body,
            BulkAssignCategoryBusinessTypes useCase,
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
                    id.ToString("D"),
                    summary: $"Bulk-assigned business types ({body.Mode}).",
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
            Guid? businessTypeId,
            string? businessTypeCode,
            string? search,
            string? barcode,
            string? sku,
            string? sortBy,
            bool? sortDesc,
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

            GlobalProductListSortBy effectiveSort = GlobalProductListSortBy.Name;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<GlobalProductListSortBy>(sortBy, ignoreCase: true, out var parsed)
                    || !Enum.IsDefined(parsed))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidGlobalProductSortField,
                        $"Unrecognized sort field '{sortBy}'.",
                        StatusCodes.Status400BadRequest);
                }

                effectiveSort = parsed;
            }

            var result = await queries
                .ListAsync(
                    status,
                    categoryId,
                    businessTypeId,
                    businessTypeCode,
                    search,
                    barcode,
                    sku,
                    page,
                    pageSize,
                    ct,
                    sortBy: effectiveSort,
                    sortDescending: sortDesc ?? false)
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

        products.MapPut("/{id:guid}/image", async (
            HttpRequest request,
            Guid id,
            SetGlobalProductImage useCase,
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

            var bytes = await ReadImageUploadAsync(request, ct).ConfigureAwait(false);
            if (!bytes.IsSuccess)
            {
                return PlatformApiResults.Problem(
                    bytes.ErrorCode!,
                    bytes.ErrorMessage!,
                    PlatformApiResults.MapStatusCode(bytes.ErrorCode!));
            }

            var result = await useCase.ExecuteAsync(id, bytes.Value!, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalProductUpdated,
                    nameof(GlobalProduct),
                    id.ToString("D"),
                    summary: "Updated global product image.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        products.MapDelete("/{id:guid}/image", async (
            Guid id,
            RemoveGlobalProductImage useCase,
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

            var result = await useCase.ExecuteAsync(id, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.GlobalProductUpdated,
                    nameof(GlobalProduct),
                    id.ToString("D"),
                    summary: "Removed global product image.",
                    cancellationToken: ct).ConfigureAwait(false);
                return Results.NoContent();
            }

            return PlatformApiResults.Problem(
                result.ErrorCode!,
                result.ErrorMessage!,
                PlatformApiResults.MapStatusCode(result.ErrorCode!));
        });

        products.MapGet("/{id:guid}/image/{variant}", async (
            Guid id,
            string variant,
            HttpContext http,
            GetGlobalProductImage useCase,
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

            return PlatformApiResults.FromResult(
                await useCase.ExecuteAsync(id, variant, activeOnly: false, ct).ConfigureAwait(false),
                image => PlatformApiResults.ImageFile(http.Response, image));
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

        imports.MapGet("/template.csv", DownloadImportTemplateAsync);
        // Alias matching documented path without /products prefix.
        root.MapGet("/imports/template.csv", DownloadImportTemplateAsync);

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
            ConfirmCatalogImportRequest? body,
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

            if (body?.TargetTemplateId is not null)
            {
                var templateDenied = await authz.EnsureAsync(
                    PlatformPermission.ManageCatalogTemplates,
                    PlatformAuditActions.CatalogImportConfirmed,
                    nameof(CatalogTemplate),
                    body.TargetTemplateId.Value.ToString("D"),
                    cancellationToken: ct).ConfigureAwait(false);
                if (templateDenied is not null)
                {
                    return templateDenied;
                }
            }

            var result = await useCase.ExecuteAsync(jobId, body, ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.CatalogImportConfirmed,
                    nameof(CatalogImportJob),
                    result.Value!.Id.ToString("D"),
                    summary: result.Value.TargetTemplateId is Guid tid
                        ? $"Confirmed catalog import {result.Value.Id:D} with template {tid:D}."
                        : $"Confirmed catalog import {result.Value.Id:D}.",
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

    private static async Task<IResult> DownloadImportTemplateAsync(
        PlatformAuthz authz,
        CancellationToken ct)
    {
        var denied = await authz.EnsureAsync(
            PlatformPermission.ImportGlobalProducts,
            PlatformAuditActions.PlatformAccessChecked,
            nameof(CatalogImportJob),
            "template",
            cancellationToken: ct).ConfigureAwait(false);
        if (denied is not null)
        {
            return denied;
        }

        var bytes = CatalogImportCsvSchema.GenerateTemplateUtf8Bytes();
        return Results.File(
            bytes,
            contentType: CatalogImportCsvSchema.ContentType,
            fileDownloadName: CatalogImportCsvSchema.DownloadFileName);
    }

    private static void MapTemplateEndpoints(RouteGroupBuilder root)
    {
        var templates = root.MapGroup("/templates");

        templates.MapGet("/", async (
            CatalogTemplateQueryService queries,
            PlatformAuthz authz,
            CatalogTemplateStatus? status,
            Guid? primaryBusinessTypeId,
            string? primaryBusinessTypeCode,
            string? search,
            string? sortBy,
            bool? sortDesc,
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

            CatalogTemplateListSortBy effectiveSort = CatalogTemplateListSortBy.Name;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<CatalogTemplateListSortBy>(sortBy, ignoreCase: true, out var parsed)
                    || !Enum.IsDefined(parsed))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidCatalogTemplateSortField,
                        $"Unrecognized sort field '{sortBy}'.",
                        StatusCodes.Status400BadRequest);
                }

                effectiveSort = parsed;
            }

            var result = await queries
                .ListAsync(
                    status,
                    primaryBusinessTypeId,
                    primaryBusinessTypeCode,
                    search,
                    page,
                    pageSize,
                    ct,
                    sortBy: effectiveSort,
                    sortDescending: sortDesc ?? false)
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

        templates.MapGet("/{id:guid}/available-products", async (
            Guid id,
            CatalogTemplateQueryService queries,
            PlatformAuthz authz,
            GlobalProductStatus? status,
            Guid? categoryId,
            string? search,
            string? barcode,
            string? sku,
            string? sortBy,
            bool? sortDesc,
            int? page,
            int? pageSize,
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

            GlobalProductListSortBy effectiveSort = GlobalProductListSortBy.Name;
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                if (!Enum.TryParse<GlobalProductListSortBy>(sortBy, ignoreCase: true, out var parsed)
                    || !Enum.IsDefined(parsed))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidGlobalProductSortField,
                        $"Unrecognized sort field '{sortBy}'.",
                        StatusCodes.Status400BadRequest);
                }

                effectiveSort = parsed;
            }

            // Active is the default filter for the transfer-list available pane.
            var effectiveStatus = status ?? GlobalProductStatus.Active;
            var result = await queries
                .ListAvailableProductsAsync(
                    id,
                    effectiveStatus,
                    categoryId,
                    search,
                    barcode,
                    sku,
                    page,
                    pageSize,
                    ct,
                    sortBy: effectiveSort,
                    sortDescending: sortDesc ?? false)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        templates.MapPost("/{id:guid}/products/bulk", async (
            Guid id,
            BulkAssignCatalogTemplateProductsRequest body,
            BulkAssignCatalogTemplateProducts useCase,
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
                    summary: $"Bulk-assigned {body.GlobalProductIds.Count} product(s) to template.",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, Results.Ok);
        });

        templates.MapPost("/{id:guid}/products/bulk-remove", async (
            Guid id,
            BulkRemoveCatalogTemplateProductsRequest body,
            BulkRemoveCatalogTemplateProducts useCase,
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
                    summary: $"Bulk-removed {body.GlobalProductIds.Count} product(s) from template.",
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

    private static async Task<ApplicationResult<byte[]>> ReadImageUploadAsync(HttpRequest request, CancellationToken ct)
    {
        if (!request.HasFormContentType)
        {
            return ApplicationResult<byte[]>.Failure(
                DomainErrorCodes.InvalidGlobalProductImage,
                "Upload a JPEG, PNG, or WebP image file.");
        }

        var form = await request.ReadFormAsync(ct).ConfigureAwait(false);
        var file = form.Files.GetFile("file") ?? form.Files.FirstOrDefault();
        if (file is null || file.Length == 0)
        {
            return ApplicationResult<byte[]>.Failure(
                DomainErrorCodes.InvalidGlobalProductImage,
                "An image file is required.");
        }

        if (file.Length > GlobalProductImageUploadLimits.MaxBytes)
        {
            return ApplicationResult<byte[]>.Failure(
                DomainErrorCodes.GlobalProductImageTooLarge,
                "Image is too large. Use a file of 10 MB or less.");
        }

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, ct).ConfigureAwait(false);
        return ApplicationResult<byte[]>.Success(buffer.ToArray());
    }
}
