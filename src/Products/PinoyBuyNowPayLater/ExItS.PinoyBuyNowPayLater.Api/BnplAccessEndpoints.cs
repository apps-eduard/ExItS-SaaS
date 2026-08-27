using ExItS.PinoyBuyNowPayLater.Api.Access;
using ExItS.PinoyBuyNowPayLater.Application.Access;

namespace ExItS.PinoyBuyNowPayLater.Api;

internal static class BnplAccessEndpoints
{
    public static void MapBnplAccess(this WebApplication app)
    {
        app.MapGet("/api/v1/bnpl/access/me", async (IBnplOperationalAccessGuard guard, CancellationToken ct) =>
            {
                var decision = await guard.EvaluateAsync(BnplAccessRequirement.None, ct).ConfigureAwait(false);
                if (!decision.IsAllowed || decision.Context is null)
                {
                    return BnplApiResults.FromDenial(decision);
                }

                var context = decision.Context;
                return Results.Ok(new BnplAccessMeResponse(
                    context.ActorId,
                    context.OrganizationId,
                    context.ProductCode,
                    context.HasTrustedOrganizationMembership,
                    context.HasTrustedOrganizationEntitlement,
                    context.HasTrustedProductAssignment,
                    context.BranchScope.IsOrganizationWide,
                    context.BranchScope.AllowedBranchIds.OrderBy(id => id).ToArray(),
                    context.Capabilities.OrderBy(c => c, StringComparer.Ordinal).ToArray()));
            })
            .WithName("BnplAccessMe")
            .RequireBnplOperationalAccess();
    }
}

internal sealed record BnplAccessMeResponse(
    Guid ActorId,
    Guid OrganizationId,
    string ProductCode,
    bool HasOrganizationMembership,
    bool HasOrganizationEntitlement,
    bool HasProductAssignment,
    bool OrganizationWideBranchAccess,
    IReadOnlyList<Guid> AllowedBranchIds,
    IReadOnlyList<string> Capabilities);
