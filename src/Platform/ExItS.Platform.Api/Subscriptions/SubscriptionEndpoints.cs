using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Subscriptions;
using ExItS.Platform.Domain.Audit;
using ExItS.Platform.Domain.Authorization;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Api.Subscriptions;

/// <summary>
/// Platform subscription lifecycle endpoints. Mutations enforce
/// <see cref="PlatformPermission.ManageSubscriptions"/>; platform-wide reads require
/// <see cref="PlatformPermission.ViewPortfolio"/>; organization-scoped reads allow trusted
/// organization membership via <see cref="PlatformOrganizationAuthz"/>. No hard deletes.
/// Plan/version changes are not supported on an existing subscription aggregate — create a new
/// subscription after terminating the prior one (historical snapshots retain plan references).
/// </summary>
internal static class SubscriptionEndpoints
{
    public static IEndpointRouteBuilder MapSubscriptionEndpoints(this IEndpointRouteBuilder app)
    {
        MapTopLevelSubscriptionEndpoints(app);
        MapOrganizationScopedSubscriptionEndpoints(app);
        return app;
    }

    private static void MapTopLevelSubscriptionEndpoints(IEndpointRouteBuilder app)
    {
        var subscriptions = app.MapGroup("/api/v1/platform/subscriptions");

        subscriptions.MapGet("/", async (
            Guid? organizationId,
            string? productCode,
            SubscriptionStatus? status,
            string? search,
            bool? isTrial,
            Guid? planId,
            string? sortBy,
            bool? sortDesc,
            int? page,
            int? pageSize,
            SubscriptionQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ViewPortfolio,
                PlatformAuditActions.PlatformAccessChecked,
                nameof(Subscription),
                "list",
                organizationId,
                productCode,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var parsedSort = ParseSort(sortBy);
                var result = await queries.ListAsync(
                    organizationId,
                    productCode,
                    status,
                    search,
                    isTrial,
                    planId,
                    parsedSort,
                    sortDesc ?? true,
                    page,
                    pageSize,
                    ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        subscriptions.MapGet("/{subscriptionId:guid}", async (
            Guid subscriptionId,
            SubscriptionQueryService queries,
            PlatformAuthz authz,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var subscription = await queries.GetByIdAsync(subscriptionId, ct).ConfigureAwait(false);
            if (subscription is null)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SubscriptionNotFound,
                    "Subscription was not found.",
                    StatusCodes.Status404NotFound);
            }

            var denied = await orgAuthz.EnsureCanViewOrganizationAsync(subscription.OrganizationId, ct)
                .ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            return Results.Ok(subscription);
        });

        subscriptions.MapPost("/{subscriptionId:guid}/activate", async (
            Guid subscriptionId,
            ActivateSubscriptionRequest body,
            ActivateSubscription useCase,
            SubscriptionQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsureSubscriptionMutationAsync(
                authz, queries, subscriptionId, PlatformAuditActions.SubscriptionActivated, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(
                SubscriptionId.From(subscriptionId),
                body.PeriodStartUtc,
                body.PeriodEndUtc,
                body.ExpectedVersion,
                ct).ConfigureAwait(false);
            await AuditSubscriptionSuccessAsync(authz, result, PlatformAuditActions.SubscriptionActivated, subscriptionId, ct)
                .ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, s => Results.Ok(MapSubscription(s)));
        });

        subscriptions.MapPost("/{subscriptionId:guid}/grace-period", async (
            Guid subscriptionId,
            GracePeriodRequest body,
            EnterSubscriptionGracePeriod useCase,
            SubscriptionQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsureSubscriptionMutationAsync(
                authz, queries, subscriptionId, PlatformAuditActions.SubscriptionEnteredGracePeriod, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(
                SubscriptionId.From(subscriptionId),
                body.GracePeriodEndUtc,
                body.ExpectedVersion,
                ct).ConfigureAwait(false);
            await AuditSubscriptionSuccessAsync(
                authz, result, PlatformAuditActions.SubscriptionEnteredGracePeriod, subscriptionId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, s => Results.Ok(MapSubscription(s)));
        });

        subscriptions.MapPost("/{subscriptionId:guid}/past-due", async (
            Guid subscriptionId,
            SubscriptionLifecycleRequest? body,
            MarkSubscriptionPastDue useCase,
            SubscriptionQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsureSubscriptionMutationAsync(
                authz, queries, subscriptionId, PlatformAuditActions.SubscriptionMarkedPastDue, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(SubscriptionId.From(subscriptionId), body?.ExpectedVersion, ct)
                .ConfigureAwait(false);
            await AuditSubscriptionSuccessAsync(
                authz, result, PlatformAuditActions.SubscriptionMarkedPastDue, subscriptionId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, s => Results.Ok(MapSubscription(s)));
        });

        subscriptions.MapPost("/{subscriptionId:guid}/suspend", async (
            Guid subscriptionId,
            SubscriptionLifecycleRequest? body,
            SuspendSubscription useCase,
            SubscriptionQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsureSubscriptionMutationAsync(
                authz, queries, subscriptionId, PlatformAuditActions.SubscriptionSuspended, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(SubscriptionId.From(subscriptionId), body?.ExpectedVersion, ct)
                .ConfigureAwait(false);
            await AuditSubscriptionSuccessAsync(
                authz, result, PlatformAuditActions.SubscriptionSuspended, subscriptionId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, s => Results.Ok(MapSubscription(s)));
        });

        subscriptions.MapPost("/{subscriptionId:guid}/reactivate", async (
            Guid subscriptionId,
            ReactivateSubscriptionRequest? body,
            ReactivateSubscription useCase,
            SubscriptionQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsureSubscriptionMutationAsync(
                authz, queries, subscriptionId, PlatformAuditActions.SubscriptionReactivated, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase.ExecuteAsync(
                SubscriptionId.From(subscriptionId),
                body?.PeriodStartUtc,
                body?.PeriodEndUtc,
                body?.ExpectedVersion,
                ct).ConfigureAwait(false);
            await AuditSubscriptionSuccessAsync(
                authz, result, PlatformAuditActions.SubscriptionReactivated, subscriptionId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, s => Results.Ok(MapSubscription(s)));
        });

        subscriptions.MapPost("/{subscriptionId:guid}/cancel", async (
            Guid subscriptionId,
            SubscriptionLifecycleRequest? body,
            CancelSubscription useCase,
            SubscriptionQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsureSubscriptionMutationAsync(
                authz, queries, subscriptionId, PlatformAuditActions.SubscriptionCancelled, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(SubscriptionId.From(subscriptionId), body?.ExpectedVersion, ct)
                .ConfigureAwait(false);
            await AuditSubscriptionSuccessAsync(
                authz, result, PlatformAuditActions.SubscriptionCancelled, subscriptionId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, s => Results.Ok(MapSubscription(s)));
        });

        subscriptions.MapPost("/{subscriptionId:guid}/expire", async (
            Guid subscriptionId,
            SubscriptionLifecycleRequest? body,
            ExpireSubscription useCase,
            SubscriptionQueryService queries,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await EnsureSubscriptionMutationAsync(
                authz, queries, subscriptionId, PlatformAuditActions.SubscriptionExpired, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            var result = await useCase
                .ExecuteAsync(SubscriptionId.From(subscriptionId), body?.ExpectedVersion, ct)
                .ConfigureAwait(false);
            await AuditSubscriptionSuccessAsync(
                authz, result, PlatformAuditActions.SubscriptionExpired, subscriptionId, ct).ConfigureAwait(false);
            return PlatformApiResults.FromResult(result, s => Results.Ok(MapSubscription(s)));
        });
    }

    private static async Task<IResult?> EnsureSubscriptionMutationAsync(
        PlatformAuthz authz,
        SubscriptionQueryService queries,
        Guid subscriptionId,
        string actionCode,
        CancellationToken ct)
    {
        var existing = await queries.GetByIdAsync(subscriptionId, ct).ConfigureAwait(false);
        return await authz.EnsureAsync(
            PlatformPermission.ManageSubscriptions,
            actionCode,
            nameof(Subscription),
            subscriptionId.ToString("D"),
            existing?.OrganizationId,
            existing?.ProductCode,
            cancellationToken: ct).ConfigureAwait(false);
    }

    private static Task AuditSubscriptionSuccessAsync(
        PlatformAuthz authz,
        ApplicationResult<Subscription> result,
        string actionCode,
        Guid subscriptionId,
        CancellationToken ct) =>
        result.IsSuccess
            ? authz.AuditSucceededAsync(
                actionCode,
                nameof(Subscription),
                subscriptionId.ToString("D"),
                result.Value!.OrganizationId.Value,
                result.Value.ProductCode.Value,
                cancellationToken: ct)
            : Task.CompletedTask;

    private static void MapOrganizationScopedSubscriptionEndpoints(IEndpointRouteBuilder app)
    {
        var orgSubscriptions = app.MapGroup("/api/v1/platform/organizations/{organizationId:guid}/subscriptions");

        orgSubscriptions.MapGet("/", async (
            Guid organizationId,
            SubscriptionStatus? status,
            string? search,
            bool? isTrial,
            Guid? planId,
            string? productCode,
            string? sortBy,
            bool? sortDesc,
            int? page,
            int? pageSize,
            SubscriptionQueryService queries,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await queries.ListAsync(
                    organizationId,
                    productCode,
                    status,
                    search,
                    isTrial,
                    planId,
                    ParseSort(sortBy),
                    sortDesc ?? true,
                    page,
                    pageSize,
                    ct).ConfigureAwait(false);
                return Results.Ok(result);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        orgSubscriptions.MapGet("/current", async (
            Guid organizationId,
            string productCode,
            SubscriptionQueryService queries,
            PlatformOrganizationAuthz orgAuthz,
            CancellationToken ct) =>
        {
            var denied = await orgAuthz.EnsureCanViewOrganizationAsync(organizationId, ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var subscription = await queries.GetCurrentAsync(organizationId, productCode, ct).ConfigureAwait(false);
                return subscription is null
                    ? PlatformApiResults.Problem(
                        ApplicationErrorCodes.SubscriptionNotFound,
                        "No current subscription was found for this organization and product.",
                        StatusCodes.Status404NotFound)
                    : Results.Ok(subscription);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        orgSubscriptions.MapPost("/", async (
            Guid organizationId,
            CreatePaidSubscriptionRequest body,
            ActivatePaidSubscription useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageSubscriptions,
                PlatformAuditActions.SubscriptionPaidStarted,
                nameof(Subscription),
                organizationId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase.ExecuteAsync(
                    PlatformOrganizationIdFrom(organizationId),
                    PlanIdFrom(body.PlanId),
                    PlanVersionIdFrom(body.PlanVersionId),
                    body.PeriodStartUtc,
                    body.PeriodEndUtc,
                    ct).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.SubscriptionPaidStarted,
                        nameof(Subscription),
                        result.Value!.Id.Value.ToString("D"),
                        organizationId,
                        result.Value.ProductCode.Value,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, s => Results.Created(
                    $"/api/v1/platform/subscriptions/{s.Id.Value}",
                    MapSubscription(s)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });

        orgSubscriptions.MapPost("/trials", async (
            Guid organizationId,
            StartTrialSubscriptionRequest body,
            StartTrialSubscription useCase,
            PlatformAuthz authz,
            CancellationToken ct) =>
        {
            var denied = await authz.EnsureAsync(
                PlatformPermission.ManageSubscriptions,
                PlatformAuditActions.SubscriptionTrialStarted,
                nameof(Subscription),
                organizationId.ToString("D"),
                organizationId,
                cancellationToken: ct).ConfigureAwait(false);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var result = await useCase.ExecuteAsync(
                    PlatformOrganizationIdFrom(organizationId),
                    PlanIdFrom(body.PlanId),
                    PlanVersionIdFrom(body.PlanVersionId),
                    TrialDefinitionIdFrom(body.TrialDefinitionId),
                    ct).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    await authz.AuditSucceededAsync(
                        PlatformAuditActions.SubscriptionTrialStarted,
                        nameof(Subscription),
                        result.Value!.Id.Value.ToString("D"),
                        organizationId,
                        result.Value.ProductCode.Value,
                        cancellationToken: ct).ConfigureAwait(false);
                }

                return PlatformApiResults.FromResult(result, s => Results.Created(
                    $"/api/v1/platform/subscriptions/{s.Id.Value}",
                    MapSubscription(s)));
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
        });
    }

    private static SubscriptionListSortBy ParseSort(string? sortBy) =>
        Enum.TryParse<SubscriptionListSortBy>(sortBy, ignoreCase: true, out var parsed)
            ? parsed
            : SubscriptionListSortBy.UpdatedAtUtc;

    private static Domain.Organizations.PlatformOrganizationId PlatformOrganizationIdFrom(Guid id) =>
        Domain.Organizations.PlatformOrganizationId.From(id);

    private static Domain.Catalog.PlanId PlanIdFrom(Guid id) => Domain.Catalog.PlanId.From(id);

    private static Domain.Catalog.PlanVersionId PlanVersionIdFrom(Guid id) => Domain.Catalog.PlanVersionId.From(id);

    private static Domain.Catalog.TrialDefinitionId TrialDefinitionIdFrom(Guid id) =>
        Domain.Catalog.TrialDefinitionId.From(id);

    private static object MapSubscription(Subscription subscription) => new
    {
        id = subscription.Id.Value,
        organizationId = subscription.OrganizationId.Value,
        productCode = subscription.ProductCode.Value,
        planId = subscription.PlanId.Value,
        planVersionId = subscription.PlanVersionId.Value,
        trialDefinitionId = subscription.TrialDefinitionId?.Value,
        status = subscription.Status.ToString(),
        trialStartUtc = subscription.TrialStartUtc,
        trialEndUtc = subscription.TrialEndUtc,
        paidPeriodStartUtc = subscription.PaidPeriodStartUtc,
        paidPeriodEndUtc = subscription.PaidPeriodEndUtc,
        gracePeriodEndUtc = subscription.GracePeriodEndUtc,
        suspendedAtUtc = subscription.SuspendedAtUtc,
        pastDueAtUtc = subscription.PastDueAtUtc,
        cancelledAtUtc = subscription.CancelledAtUtc,
        expiredAtUtc = subscription.ExpiredAtUtc,
        createdAtUtc = subscription.CreatedAtUtc,
        updatedAtUtc = subscription.UpdatedAtUtc,
        version = subscription.Version
    };
}

internal sealed record StartTrialSubscriptionRequest(Guid PlanId, Guid PlanVersionId, Guid TrialDefinitionId);

internal sealed record CreatePaidSubscriptionRequest(
    Guid PlanId,
    Guid PlanVersionId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc);

internal sealed record ActivateSubscriptionRequest(
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    int? ExpectedVersion = null);

internal sealed record GracePeriodRequest(DateTimeOffset GracePeriodEndUtc, int? ExpectedVersion = null);

internal sealed record ReactivateSubscriptionRequest(
    DateTimeOffset? PeriodStartUtc = null,
    DateTimeOffset? PeriodEndUtc = null,
    int? ExpectedVersion = null);

internal sealed record SubscriptionLifecycleRequest(int? ExpectedVersion = null);
