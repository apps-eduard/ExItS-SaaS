using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Entitlements;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Entitlements;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;

namespace ExItS.Platform.Api.Entitlements;

/// <summary>
/// Development-stage entitlement snapshot and feature override endpoints
/// (P3-WP04-entitlement-snapshots-grace-rules). Actor identity is unauthenticated, no broker/outbox
/// delivery — snapshots and overrides are persisted for later product-local projection only. Feature
/// override mutations enforce <see cref="PlatformPermission.ManageEntitlementOverrides"/> and record
/// audit trail entries.
/// </summary>
internal static class EntitlementEndpoints
{
    public static IEndpointRouteBuilder MapEntitlementEndpoints(this IEndpointRouteBuilder app)
    {
        MapOrganizationScopedEntitlementEndpoints(app);
        MapTopLevelEntitlementEndpoints(app);
        MapOrganizationScopedFeatureOverrideEndpoints(app);
        MapTopLevelFeatureOverrideEndpoints(app);
        return app;
    }

    private static void MapOrganizationScopedEntitlementEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
            "/api/v1/platform/organizations/{organizationId:guid}/products/{productCode}/entitlements");

        group.MapPost("/snapshots", async (
            Guid organizationId,
            string productCode,
            GenerateSnapshotRequest? body,
            GenerateEntitlementSnapshot useCase,
            CancellationToken ct) =>
        {
            try
            {
                var result = await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    ProductCode.Create(productCode),
                    body?.ExpectedNextVersion,
                    ct).ConfigureAwait(false);

                return PlatformApiResults.FromResult(result, s => Results.Created(
                    $"/api/v1/platform/entitlements/snapshots/{s.Id.Value}",
                    MapSnapshot(s)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        group.MapGet("/snapshots/latest", async (
            Guid organizationId,
            string productCode,
            EntitlementQueryService queries,
            CancellationToken ct) =>
        {
            var snapshot = await queries.GetLatestAsync(organizationId, productCode, ct).ConfigureAwait(false);
            return snapshot is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.EntitlementSnapshotNotFound,
                    "No entitlement snapshot was found for this organization and product.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(snapshot);
        });

        group.MapGet("/snapshots", async (
            Guid organizationId,
            string productCode,
            int? page,
            int? pageSize,
            EntitlementQueryService queries,
            CancellationToken ct) =>
        {
            var result = await queries
                .ListHistoryAsync(organizationId, productCode, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });

        group.MapGet("/snapshots/{snapshotVersion:int}", async (
            Guid organizationId,
            string productCode,
            int snapshotVersion,
            EntitlementQueryService queries,
            CancellationToken ct) =>
        {
            var snapshot = await queries
                .GetByVersionAsync(organizationId, productCode, snapshotVersion, ct)
                .ConfigureAwait(false);
            return snapshot is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.EntitlementSnapshotNotFound,
                    "No entitlement snapshot was found for this version.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(snapshot);
        });

        group.MapPost("/reconcile", async (
            Guid organizationId,
            string productCode,
            ReconcileSnapshotRequest? body,
            ReconcileEntitlementSnapshot useCase,
            CancellationToken ct) =>
        {
            try
            {
                var result = await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    ProductCode.Create(productCode),
                    body?.Reason,
                    ct).ConfigureAwait(false);

                return PlatformApiResults.FromResult(result, s => Results.Created(
                    $"/api/v1/platform/entitlements/snapshots/{s.Id.Value}",
                    MapSnapshot(s)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });
    }

    private static void MapTopLevelEntitlementEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/entitlements/snapshots");

        group.MapGet("/{snapshotId:guid}", async (
            Guid snapshotId,
            EntitlementQueryService queries,
            CancellationToken ct) =>
        {
            var snapshot = await queries.GetSnapshotByIdAsync(snapshotId, ct).ConfigureAwait(false);
            return snapshot is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.EntitlementSnapshotNotFound,
                    "Entitlement snapshot was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(snapshot);
        });
    }

    private static void MapOrganizationScopedFeatureOverrideEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(
            "/api/v1/platform/organizations/{organizationId:guid}/products/{productCode}/feature-overrides");

        group.MapPost("/", async (
            Guid organizationId,
            string productCode,
            CreateFeatureOverrideRequest body,
            CreateFeatureOverride useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageEntitlementOverrides,
                PlatformAuditActions.FeatureOverrideCreated,
                "FeatureOverride",
                body.FeatureCode,
                organizationId,
                productCode,
                reason: body.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase.ExecuteAsync(
                    PlatformOrganizationId.From(organizationId),
                    ProductCode.Create(productCode),
                    FeatureCode.Create(body.FeatureCode),
                    body.Enabled,
                    body.Reason,
                    PlatformUserId.From(body.CreatedByUserId),
                    body.NumericLimit,
                    body.ExpiresAtUtc,
                    ct).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.FeatureOverrideCreated,
                        "FeatureOverride",
                        result.Value!.Id.Value.ToString("D"),
                        organizationId,
                        productCode,
                        reason: body.Reason,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, o => Results.Created(
                    $"/api/v1/platform/feature-overrides/{o.Id.Value}",
                    MapOverride(o)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        group.MapGet("/", async (
            Guid organizationId,
            string productCode,
            FeatureOverrideStatus? status,
            int? page,
            int? pageSize,
            FeatureOverrideQueryService queries,
            CancellationToken ct) =>
        {
            var result = await queries
                .ListByOrganizationProductAsync(organizationId, productCode, status, page, pageSize, ct)
                .ConfigureAwait(false);
            return Results.Ok(result);
        });
    }

    private static void MapTopLevelFeatureOverrideEndpoints(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/platform/feature-overrides");

        group.MapGet("/{overrideId:guid}", async (
            Guid overrideId,
            FeatureOverrideQueryService queries,
            CancellationToken ct) =>
        {
            var featureOverride = await queries.GetByIdAsync(overrideId, ct).ConfigureAwait(false);
            return featureOverride is null
                ? PlatformApiResults.Problem(
                    ApplicationErrorCodes.FeatureOverrideNotFound,
                    "Feature override was not found.",
                    StatusCodes.Status404NotFound)
                : Results.Ok(featureOverride);
        });

        group.MapPost("/{overrideId:guid}/revoke", async (
            Guid overrideId,
            RevokeFeatureOverrideRequest body,
            RevokeFeatureOverride useCase,
            FeatureOverrideQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var existing = await queries.GetByIdAsync(overrideId, ct).ConfigureAwait(false);
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageEntitlementOverrides,
                PlatformAuditActions.FeatureOverrideRevoked,
                "FeatureOverride",
                overrideId.ToString("D"),
                existing?.OrganizationId,
                existing?.ProductCode,
                reason: body.Reason,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(
                FeatureOverrideId.From(overrideId),
                body.Reason,
                PlatformUserId.From(body.RevokedByUserId),
                ct).ConfigureAwait(false);
            if (result.IsSuccess)
            {
                await authz.AuditSucceededAsync(
                    PlatformAuditActions.FeatureOverrideRevoked,
                    "FeatureOverride",
                    overrideId.ToString("D"),
                    result.Value!.OrganizationId.Value,
                    result.Value.ProductCode.Value,
                    reason: body.Reason,
                    cancellationToken: ct).ConfigureAwait(false);
            }

            return PlatformApiResults.FromResult(result, o => Results.Ok(MapOverride(o)));
        });
    }

    private static object MapSnapshot(EntitlementSnapshot snapshot) => new
    {
        id = snapshot.Id.Value,
        organizationId = snapshot.OrganizationId.Value,
        productCode = snapshot.ProductCode.Value,
        subscriptionId = snapshot.SubscriptionId.Value,
        planCode = snapshot.PlanCode.Value,
        planVersionNumber = snapshot.PlanVersionNumber,
        snapshotVersion = snapshot.SnapshotVersion,
        schemaVersion = snapshot.SchemaVersion,
        subscriptionStatus = snapshot.SubscriptionStatus.ToString(),
        inGracePeriod = snapshot.InGracePeriod,
        generatedAtUtc = snapshot.GeneratedAtUtc,
        effectiveAtUtc = snapshot.EffectiveAtUtc,
        refreshByUtc = snapshot.RefreshByUtc,
        expiresAtUtc = snapshot.ExpiresAtUtc,
        sourceAggregateVersion = snapshot.SourceAggregateVersion,
        grants = snapshot.Grants.Select(g => new
        {
            featureCode = g.FeatureCode.Value,
            enabled = g.Enabled,
            numericLimit = g.NumericLimit,
            source = g.Source.ToString(),
            effectiveAtUtc = g.EffectiveAtUtc,
            expiresAtUtc = g.ExpiresAtUtc
        })
    };

    private static object MapOverride(FeatureOverride featureOverride) => new
    {
        id = featureOverride.Id.Value,
        organizationId = featureOverride.OrganizationId.Value,
        productCode = featureOverride.ProductCode.Value,
        featureCode = featureOverride.FeatureCode.Value,
        enabled = featureOverride.Enabled,
        numericLimit = featureOverride.NumericLimit,
        reason = featureOverride.Reason,
        effectiveFromUtc = featureOverride.EffectiveFromUtc,
        expiresAtUtc = featureOverride.ExpiresAtUtc,
        status = featureOverride.Status.ToString(),
        createdAtUtc = featureOverride.CreatedAtUtc,
        createdByUserId = featureOverride.CreatedByUserId.Value,
        updatedAtUtc = featureOverride.UpdatedAtUtc,
        revokedAtUtc = featureOverride.RevokedAtUtc,
        revokedByUserId = featureOverride.RevokedByUserId?.Value,
        revocationReason = featureOverride.RevocationReason
    };
}

internal sealed record GenerateSnapshotRequest(int? ExpectedNextVersion);

internal sealed record ReconcileSnapshotRequest(string? Reason);

internal sealed record CreateFeatureOverrideRequest(
    string FeatureCode,
    bool Enabled,
    string Reason,
    Guid CreatedByUserId,
    int? NumericLimit,
    DateTimeOffset? ExpiresAtUtc);

internal sealed record RevokeFeatureOverrideRequest(string Reason, Guid RevokedByUserId);
