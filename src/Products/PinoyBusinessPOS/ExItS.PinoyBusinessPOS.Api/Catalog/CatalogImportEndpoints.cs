using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Api.Catalog;

internal static class CatalogImportEndpoints
{
    public static IEndpointRouteBuilder MapCatalogImportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/catalog-imports");

        group.MapGet("/imported-global-products", async (
            HttpRequest request,
            [AsParameters] ImportedGlobalProductsQuery query,
            ListImportedGlobalProducts useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, query.Ids, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapGet("/templates/{templateId:guid}/status", async (
            HttpRequest request,
            Guid templateId,
            GetTemplateImportStatus useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var platformSessionToken = ExtractPlatformSessionToken(request);
            if (string.IsNullOrWhiteSpace(platformSessionToken))
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportPlatformSessionRequired,
                    "Platform session is required to import catalog templates. Sign in again and retry.",
                    StatusCodes.Status401Unauthorized);
            }

            var result = await useCase
                .ExecuteAsync(organizationId, templateId, platformSessionToken, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/template", async (
            HttpRequest request,
            ImportTemplateBatchRequest body,
            ImportTemplateBatch useCase,
            IPosCommercialAccessAccessor access,
            IPosIdempotencyService idempotency,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var platformSessionToken = ExtractPlatformSessionToken(request);
            if (string.IsNullOrWhiteSpace(platformSessionToken))
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportPlatformSessionRequired,
                    "Platform session is required to import catalog templates. Sign in again and retry.",
                    StatusCodes.Status401Unauthorized);
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    "catalog-import-template",
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        body.PlatformTemplateId,
                        body.BatchNumber <= 0 ? 1 : body.BatchNumber,
                        actorId.ToString("D"),
                        platformSessionToken,
                        body.IdempotencyKey,
                        ct2),
                    dto => dto,
                    dto => Results.Accepted($"/api/v1/pos/catalog-imports/{dto.JobId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/products", async (
            HttpRequest request,
            ImportSelectedProductsRequest body,
            ImportSelectedProducts useCase,
            IPosCommercialAccessAccessor access,
            IPosIdempotencyService idempotency,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var platformSessionToken = ExtractPlatformSessionToken(request);
            if (string.IsNullOrWhiteSpace(platformSessionToken))
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportPlatformSessionRequired,
                    "Platform session is required to import catalog products. Sign in again and retry.",
                    StatusCodes.Status401Unauthorized);
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    "catalog-import-products",
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        body.PlatformGlobalProductIds ?? [],
                        actorId.ToString("D"),
                        platformSessionToken,
                        body.IdempotencyKey,
                        ct2),
                    dto => dto,
                    dto => Results.Accepted($"/api/v1/pos/catalog-imports/{dto.JobId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPost("/template/{templateId:guid}/next-batch", async (
            HttpRequest request,
            Guid templateId,
            ImportTemplateBatchRequest? body,
            ImportTemplateBatch useCase,
            IPosCommercialAccessAccessor access,
            IPosIdempotencyService idempotency,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var batchNumber = body?.BatchNumber is > 1 ? body.BatchNumber : 2;
            var platformSessionToken = ExtractPlatformSessionToken(request);
            if (string.IsNullOrWhiteSpace(platformSessionToken))
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportPlatformSessionRequired,
                    "Platform session is required to import catalog templates. Sign in again and retry.",
                    StatusCodes.Status401Unauthorized);
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    "catalog-import-template-next-batch",
                    idempotency,
                    ct2 => useCase.ExecuteAsync(
                        organizationId,
                        templateId,
                        batchNumber,
                        actorId.ToString("D"),
                        platformSessionToken,
                        body?.IdempotencyKey,
                        ct2),
                    dto => dto,
                    dto => Results.Accepted($"/api/v1/pos/catalog-imports/{dto.JobId:D}", dto),
                    ct)
                .ConfigureAwait(false);
        });

        group.MapGet("/{jobId:guid}", async (
            HttpRequest request,
            Guid jobId,
            CatalogImportQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var job = await queries.GetJobAsync(organizationId, jobId, ct).ConfigureAwait(false);
            return job is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportJobNotFound,
                    "Import job was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(job);
        });

        group.MapGet("/{jobId:guid}/items", async (
            HttpRequest request,
            Guid jobId,
            string? status,
            int? page,
            int? pageSize,
            CatalogImportQueryService queries,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewCatalog, out var organizationId, out var problem))
            {
                return problem!;
            }

            var job = await queries.GetJobAsync(organizationId, jobId, ct).ConfigureAwait(false);
            if (job is null)
            {
                return PosApiResults.Problem(
                    ApplicationErrorCodes.CatalogImportJobNotFound,
                    "Import job was not found.",
                    StatusCodes.Status404NotFound);
            }

            var items = await queries
                .GetItemsAsync(organizationId, jobId, status, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(items);
        });

        return app;
    }

    private static bool TryAuthorize(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        UtangCapability capability,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        return PosCommercialScope.TryAuthorize(access, capability, out problem);
    }

    private static string? ExtractPlatformSessionToken(HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-ExItS-Session-Token", out var values)
            && !string.IsNullOrWhiteSpace(values.FirstOrDefault()))
        {
            return values.FirstOrDefault()!.Trim();
        }

        if (!request.Headers.TryGetValue("Authorization", out var authValues))
        {
            return null;
        }

        var header = authValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(header)
            || !header.StartsWith("PlatformSession ", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var token = header["PlatformSession ".Length..].Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }
}

internal sealed class ImportedGlobalProductsQuery
{
    public Guid[]? Ids { get; init; }
}
