using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Api.Access;

/// <summary>
/// Product-access assignment and effective commercial access evaluation.
/// Grants commercial entry eligibility only — never product-local roles.
/// Development-stage: actor identity is unauthenticated, but mutations enforce
/// <see cref="PlatformPermission.ManageProductAccess"/> scoped to the organization and record audit trail entries.
/// </summary>
internal static class AccessEndpoints
{
    public static IEndpointRouteBuilder MapAccessEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/platform/organizations/{organizationId:guid}/product-access", async (
            Guid organizationId,
            string? status,
            int? page,
            int? pageSize,
            ProductAccessQueryService queries,
            CancellationToken ct) =>
        {
            if (!TryParseStatus(status, out var parsed, out var error))
            {
                return error!;
            }

            var result = await queries
                .ListByOrganizationAsync(organizationId, parsed, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapPost("/api/v1/platform/organizations/{organizationId:guid}/product-access", async (
            Guid organizationId,
            GrantProductAccessRequest body,
            GrantProductAccess useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageProductAccess,
                PlatformAuditActions.ProductAccessGranted,
                "ProductAccessAssignment",
                body.UserId.ToString("D"),
                organizationId,
                body.ProductCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        PlatformUserId.From(body.UserId),
                        body.ProductCode,
                        body.GrantedByActor,
                        body.Reason,
                        ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.ProductAccessGranted,
                        "ProductAccessAssignment",
                        result.Value!.Id.Value.ToString("D"),
                        organizationId,
                        body.ProductCode,
                        reason: body.Reason,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, a => Results.Created(
                    $"/api/v1/platform/product-access/{a.Id.Value}",
                    ProductAccessQueryService.Map(a)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapGet("/api/v1/platform/users/{userId:guid}/product-access", async (
            Guid userId,
            string? status,
            int? page,
            int? pageSize,
            ProductAccessQueryService queries,
            CancellationToken ct) =>
        {
            if (!TryParseStatus(status, out var parsed, out var error))
            {
                return error!;
            }

            var result = await queries.ListByUserAsync(userId, parsed, page, pageSize, ct).ConfigureAwait(false);
            return Results.Ok(result);
        });

        app.MapPost("/api/v1/platform/product-access/{assignmentId:guid}/revoke", async (
            Guid assignmentId,
            RevokeProductAccessRequest body,
            RevokeProductAccess useCase,
            ProductAccessQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var existing = await queries.GetByIdAsync(assignmentId, ct).ConfigureAwait(false);
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageProductAccess,
                PlatformAuditActions.ProductAccessRevoked,
                "ProductAccessAssignment",
                assignmentId.ToString("D"),
                existing?.OrganizationId,
                existing?.ProductCode,
                reason: body.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        ProductAccessAssignmentId.From(assignmentId),
                        body.RevokedByActor,
                        body.Reason,
                        ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.ProductAccessRevoked,
                        "ProductAccessAssignment",
                        assignmentId.ToString("D"),
                        result.Value!.OrganizationId.Value,
                        result.Value.ProductCode.Value,
                        reason: body.Reason,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, a => Results.Ok(ProductAccessQueryService.Map(a)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapGet("/api/v1/platform/access/evaluate", async (
            Guid userId,
            Guid organizationId,
            string productCode,
            EvaluateEffectiveProductAccess useCase,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidProductCode,
                    "productCode is required.",
                    StatusCodes.Status400BadRequest);
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformUserId.From(userId),
                        PlatformOrganizationId.From(organizationId),
                        productCode,
                        ct)
                    .ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        return app;
    }

    private static bool TryParseStatus(string? status, out ProductAccessStatus? parsed, out IResult? error)
    {
        parsed = null;
        error = null;
        if (string.IsNullOrWhiteSpace(status))
        {
            return true;
        }

        if (!Enum.TryParse<ProductAccessStatus>(status, ignoreCase: true, out var value))
        {
            error = PlatformApiResults.Problem(
                "platform.product_access.status.invalid",
                $"Unrecognized product-access status '{status}'.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        parsed = value;
        return true;
    }
}

internal sealed record GrantProductAccessRequest(Guid UserId, string ProductCode, string GrantedByActor, string? Reason);
internal sealed record RevokeProductAccessRequest(string RevokedByActor, string? Reason);
