using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Onboarding;

namespace ExItS.PinoyBusinessPOS.Api.Onboarding;

/// <summary>
/// Server-authoritative post-subscription onboarding progress (NEW orgs via ensure only; no backfill).
/// </summary>
internal static class OnboardingEndpoints
{
    public static IEndpointRouteBuilder MapOnboardingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/pos/onboarding");

        group.MapGet("/progress", async (
            HttpRequest request,
            GetOrganizationOnboardingProgress query,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorizeAny(
                    request,
                    access,
                    [UtangCapability.ManageOperationalSetup, UtangCapability.ViewCatalog],
                    out var organizationId,
                    out var problem))
            {
                return problem!;
            }

            var result = await query.ExecuteAsync(organizationId, ct).ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPost("/progress/ensure", async (
            HttpRequest request,
            EnsureOrganizationOnboardingProgressRequest? body,
            EnsureOrganizationOnboardingProgress useCase,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!TryAuthorize(request, access, UtangCapability.ManageOperationalSetup, out var organizationId, out var problem))
            {
                return problem!;
            }

            var result = await useCase
                .ExecuteAsync(organizationId, body, ct)
                .ConfigureAwait(false);
            return PosApiResults.FromResult(result, Results.Ok);
        });

        group.MapPut("/progress", async (
            HttpRequest request,
            UpdateOrganizationOnboardingProgressRequest body,
            UpdateOrganizationOnboardingProgress useCase,
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

    private static bool TryAuthorizeAny(
        HttpRequest request,
        IPosCommercialAccessAccessor access,
        IReadOnlyList<UtangCapability> capabilities,
        out Guid organizationId,
        out IResult? problem)
    {
        if (!PosOrganizationScope.TryGetOrganizationId(request, out organizationId, out problem))
        {
            return false;
        }

        problem = null;
        foreach (var capability in capabilities)
        {
            if (PosCommercialScope.TryAuthorize(access, capability, out problem))
            {
                return true;
            }
        }

        return false;
    }
}
