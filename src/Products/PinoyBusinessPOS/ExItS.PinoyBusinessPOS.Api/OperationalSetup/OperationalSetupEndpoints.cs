using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;

namespace ExItS.PinoyBusinessPOS.Api.OperationalSetup;

/// <summary>Organization-scoped POS operational setup endpoints (P17-WP02).</summary>
internal static class OperationalSetupEndpoints
{
    public const string CompleteOperation = "pos.operational_setup.complete";

    public static IEndpointRouteBuilder MapOperationalSetupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/operational-setup");

        group.MapGet("/", async (
            HttpRequest request,
            GetOperationalSetupQuery query,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewOperationalSetup, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var dto = await query.ExecuteAsync(organizationId, actorId, ct).ConfigureAwait(false);
            return Results.Ok(dto);
        });

        group.MapPost("/complete", async (
            HttpRequest request,
            CompleteOperationalSetupRequest body,
            CompleteOperationalSetup useCase,
            IPosIdempotencyService idempotency,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageOperationalSetup, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            return await PosIdempotencyEndpointHelper.ExecuteMutationAsync(
                    request,
                    organizationId,
                    CompleteOperation,
                    idempotency,
                    ct2 => useCase.ExecuteAsync(organizationId, actorId, body, ct2),
                    dto => dto,
                    Results.Ok,
                    ct)
                .ConfigureAwait(false);
        });

        group.MapPut("/", async (
            HttpRequest request,
            UpdateOperationalSetupRequest body,
            UpdateOperationalSetup useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageOperationalSetup, out var organizationId, out var problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetActorId(request, out var actorId, out problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, actorId, body, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapGet("/cash-denominations", async (
            HttpRequest request,
            ListCashDenominationsQuery query,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ViewOperationalSetup, out var organizationId, out var problem))
            {
                return problem!;
            }

            var items = await query.ExecuteAsync(organizationId, ct).ConfigureAwait(false);
            return Results.Ok(items);
        });

        group.MapPut("/cash-denominations", async (
            HttpRequest request,
            ReplaceCashDenominationsRequest body,
            ReplaceCashDenominations useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageOperationalSetup, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase.ExecuteAsync(organizationId, body, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
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
}
