using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Access;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;

namespace ExItS.Platform.Api.Access;

/// <summary>
/// Enabled-product discovery, product authorization evaluation, and product-local role assignment (P16-WP09).
/// Entitlement enables the Organization product; product-local role authorizes individual operations.
/// </summary>
internal static class ProductNavigationEndpoints
{
    public static IEndpointRouteBuilder MapProductNavigationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/v1/organizations/{organizationId:guid}/enabled-products", async (
            Guid organizationId,
            DiscoverEnabledProducts useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                PlatformAuditActions.EnabledProductsDiscovered,
                "EnabledProduct",
                organizationId.ToString("D"),
                organizationId,
                summary: "Discover enabled products for organization session.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            if (actor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AccountScopeDenied,
                    "Authenticated organization user is required.",
                    StatusCodes.Status403Forbidden);
            }

            var result = await useCase
                .ExecuteAsync(actor.PlatformUserId, PlatformOrganizationId.From(organizationId), ct)
                .ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.EnabledProductsDiscovered,
                    "EnabledProduct",
                    organizationId.ToString("D"),
                    organizationId,
                    summary: $"Discovered {result.Value!.Count} enabled product(s).",
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, items => Results.Ok(items));
        });

        app.MapGet("/api/v1/organizations/{organizationId:guid}/product-authorization", async (
            Guid organizationId,
            string productCode,
            Guid? userId,
            EvaluateProductAuthorization useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(productCode))
            {
                return PlatformApiResults.Problem(
                    DomainErrorCodes.InvalidProductCode,
                    "productCode is required.",
                    StatusCodes.Status400BadRequest);
            }

            var denied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                PlatformAuditActions.ProductAuthorizationChecked,
                "ProductAuthorization",
                organizationId.ToString("D"),
                organizationId,
                summary: "Evaluate product authorization (entitlement vs role).",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            var targetUserId = userId ?? actor.PlatformUserId?.Value;
            if (targetUserId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AccountScopeDenied,
                    "Authenticated organization user is required.",
                    StatusCodes.Status403Forbidden);
            }

            if (userId is Guid requested
                && actor.PlatformUserId is not null
                && requested != actor.PlatformUserId.Value)
            {
                var manageDenied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                    PlatformAuditActions.ProductAuthorizationChecked,
                    "ProductAuthorization",
                    requested.ToString("D"),
                    organizationId,
                    summary: "Evaluate product authorization for another member.",
                    cancellationToken: ct).ConfigureAwait(false);
                if (manageDenied is not null)
                {
                    return manageDenied;
                }
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformUserId.From(targetUserId.Value),
                        PlatformOrganizationId.From(organizationId),
                        productCode,
                        ct)
                    .ConfigureAwait(false);
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.ProductAuthorizationChecked,
                    "ProductAuthorization",
                    targetUserId.Value.ToString("D"),
                    organizationId,
                    productCode,
                    summary: $"Authorization canOperate={result.CanOperate}; reason={result.ReasonCode}.",
                    cancellationToken: ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapGet("/api/v1/organizations/{organizationId:guid}/product-local-roles", async (
            Guid organizationId,
            string? status,
            ProductLocalRoleGrantQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.PlatformAccessChecked,
                nameof(ProductLocalRoleGrant),
                organizationId.ToString("D"),
                organizationId,
                summary: "List product-local role grants.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            ProductLocalRoleGrantStatus? parsed = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (!Enum.TryParse<ProductLocalRoleGrantStatus>(status, ignoreCase: true, out var value))
                {
                    return PlatformApiResults.Problem(
                        DomainErrorCodes.InvalidProductLocalRoleCode,
                        $"Unrecognized product-local role status '{status}'.",
                        StatusCodes.Status400BadRequest);
                }

                parsed = value;
            }

            var items = await queries.ListByOrganizationAsync(organizationId, parsed, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/product-local-roles", async (
            Guid organizationId,
            AssignProductLocalRoleRequest body,
            AssignProductLocalRole useCase,
            ProductLocalRoleGrantQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.ProductLocalRoleGranted,
                nameof(ProductLocalRoleGrant),
                body.UserIdentityId.ToString("D"),
                organizationId,
                reason: body.Reason,
                summary: "Assign product-local role.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            if (actor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AccountScopeDenied,
                    "Authenticated organization user is required.",
                    StatusCodes.Status403Forbidden);
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        PlatformOrganizationId.From(organizationId),
                        PlatformUserId.From(body.UserIdentityId),
                        body.ProductCode,
                        body.RoleCode,
                        actor.PlatformUserId,
                        body.Reason,
                        ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.ProductLocalRoleGranted,
                        nameof(ProductLocalRoleGrant),
                        result.Value!.Id.Value.ToString("D"),
                        organizationId,
                        body.ProductCode,
                        reason: body.Reason,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                if (!result.IsSuccess)
                {
                    return PlatformApiResults.FromResult(result, _ => Results.Ok());
                }

                var dto = await queries.MapAsync(result.Value!, ct).ConfigureAwait(false);
                return Results.Created(
                    $"/api/v1/organizations/{organizationId:D}/product-local-roles/{result.Value!.Id.Value:D}",
                    dto);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/product-local-roles/{grantId:guid}/revoke", async (
            Guid organizationId,
            Guid grantId,
            RevokeProductLocalRoleRequest body,
            RevokeProductLocalRole useCase,
            ProductLocalRoleGrantQueryService queries,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureCanManageMembershipsAsync(
                PlatformAuditActions.ProductLocalRoleRevoked,
                nameof(ProductLocalRoleGrant),
                grantId.ToString("D"),
                organizationId,
                reason: body.Reason,
                summary: "Revoke product-local role.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            if (actor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AccountScopeDenied,
                    "Authenticated organization user is required.",
                    StatusCodes.Status403Forbidden);
            }

            var existing = (await queries.ListByOrganizationAsync(organizationId, cancellationToken: ct).ConfigureAwait(false))
                .FirstOrDefault(g => g.Id == grantId);
            if (existing is null || existing.OrganizationId != organizationId)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.ProductLocalRoleGrantNotFound,
                    "Product-local role grant was not found.",
                    StatusCodes.Status404NotFound);
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        ProductLocalRoleGrantId.From(grantId),
                        actor.PlatformUserId,
                        body.Reason,
                        ct)
                    .ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.ProductLocalRoleRevoked,
                        nameof(ProductLocalRoleGrant),
                        grantId.ToString("D"),
                        organizationId,
                        existing.ProductCode,
                        reason: body.Reason,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                if (!result.IsSuccess)
                {
                    return PlatformApiResults.FromResult(result, _ => Results.Ok());
                }

                return Results.Ok(await queries.MapAsync(result.Value!, ct).ConfigureAwait(false));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        app.MapPost("/api/v1/organizations/{organizationId:guid}/products/{productCode}/launch", async (
            Guid organizationId,
            string productCode,
            EvaluateProductAuthorization useCase,
            PlatformMembershipAuthz membershipAuthz,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await membershipAuthz.EnsureActiveOrganizationMemberAsync(
                PlatformAuditActions.ProductLaunched,
                "ProductLaunch",
                productCode,
                organizationId,
                summary: "Launch product navigation check.",
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var actor = authz.CurrentActor;
            if (actor.PlatformUserId is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.AccountScopeDenied,
                    "Authenticated organization user is required.",
                    StatusCodes.Status403Forbidden);
            }

            try
            {
                var result = await useCase
                    .ExecuteAsync(
                        actor.PlatformUserId,
                        PlatformOrganizationId.From(organizationId),
                        productCode,
                        ct)
                    .ConfigureAwait(false);
                if (!result.CanOperate)
                {
                    var denial = ProductAccessDenialDisplay.ToDisplay(result.ReasonCode);
                    return PlatformApiResults.Problem(
                        result.ReasonCode == EffectiveAccessReasonCodes.ProductLocalRoleMissing
                            ? ApplicationErrorCodes.ProductLocalRoleMissing
                            : ApplicationErrorCodes.ProductEntryDenied,
                        string.IsNullOrWhiteSpace(denial) ? "Product launch denied." : denial,
                        StatusCodes.Status403Forbidden);
                }

                await authz.AuditSucceededAsync(
                    PlatformAuditActions.ProductLaunched,
                    "ProductLaunch",
                    productCode,
                    organizationId,
                    productCode,
                    summary: $"Product launch authorized with role {result.ProductLocalRoleCode}.",
                    cancellationToken: ct).ConfigureAwait(false);

                return Results.Ok(new
                {
                    result.ProductCode,
                    result.CanOperate,
                    result.ProductLocalRoleCode,
                    result.MappedPosRoleCode,
                    launchPath = $"/admin/product-entry?organizationId={organizationId:D}&productCode={Uri.EscapeDataString(result.ProductCode)}",
                    result.ReasonCode
                });
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        return app;
    }
}

internal sealed record AssignProductLocalRoleRequest(
    Guid UserIdentityId,
    string ProductCode,
    string RoleCode,
    string? Reason);

internal sealed record RevokeProductLocalRoleRequest(string? Reason);
