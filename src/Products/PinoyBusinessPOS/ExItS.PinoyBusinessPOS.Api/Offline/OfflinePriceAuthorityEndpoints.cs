using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Api.Offline;

/// <summary>Request body: the products the sell floor just browsed, plus any sell units in play.</summary>
public sealed record IssueOfflinePriceAuthoritiesRequest(
    List<Guid> ProductIds,
    List<Guid>? SellingUnitIds = null);

public sealed record OfflinePriceAuthorityDto(
    Guid AuthorityId,
    Guid OrganizationId,
    Guid? BranchId,
    Guid ProductId,
    Guid? SellingUnitId,
    decimal UnitPrice,
    string UnitOfMeasure,
    string SellingMode,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    string Signature);

public sealed record IssueOfflinePriceAuthoritiesResponse(
    List<OfflinePriceAuthorityDto> Authorities,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc);

/// <summary>
/// Issues server-signed offline price leases for the products a device is about to sell.
///
/// Scoped exactly like catalog browse (organization + optional branch, <c>ViewCatalog</c>): a
/// cashier who may see a price may lease it. Issuing records nothing and moves no money — it only
/// commits the server to a price for a bounded window, so the device never has to invent one.
/// </summary>
internal static class OfflinePriceAuthorityEndpoints
{
    public static IEndpointRouteBuilder MapOfflinePriceAuthorityEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/v1/pos/offline-price-authorities", async (
            HttpRequest request,
            IssueOfflinePriceAuthoritiesRequest body,
            IOfflinePriceAuthorityService authorities,
            IPosCommercialAccessAccessor access,
            CancellationToken ct) =>
        {
            if (!PosOrganizationScope.TryGetOrganizationId(request, out var organizationId, out var problem)
                || !PosCommercialScope.TryAuthorize(access, UtangCapability.ViewCatalog, out problem))
            {
                return problem!;
            }

            if (!PosOrganizationScope.TryGetOptionalBranchId(request, out var branchId))
            {
                return PosApiResults.Problem(
                    DomainErrorCodes.InvalidBranchId,
                    $"Header '{PosOrganizationHeaders.BranchHeaderName}' must be a non-empty GUID.",
                    StatusCodes.Status400BadRequest);
            }

            if (!TryBuildItems(body, out var items, out var itemsProblem))
            {
                return itemsProblem!;
            }

            var result = await authorities
                .IssueAsync(organizationId, branchId, items, ct)
                .ConfigureAwait(false);

            return PosApiResults.FromResult(result, issued =>
            {
                var dtos = issued.Select(Map).ToList();
                return Results.Ok(new IssueOfflinePriceAuthoritiesResponse(
                    dtos,
                    issued[0].IssuedAtUtc,
                    issued[0].ExpiresAtUtc));
            });
        });

        return app;
    }

    /// <summary>
    /// <c>SellingUnitIds</c> is positional: index i pairs with <c>ProductIds[i]</c>, and an empty
    /// GUID means "base unit". A length mismatch is rejected rather than guessed, because guessing
    /// would lease the wrong price for the wrong unit.
    /// </summary>
    private static bool TryBuildItems(
        IssueOfflinePriceAuthoritiesRequest body,
        out List<OfflinePriceAuthorityRequestItem> items,
        out IResult? problem)
    {
        items = [];
        problem = null;

        var productIds = body.ProductIds ?? [];
        if (productIds.Count == 0)
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.OfflinePriceAuthorityRequestInvalid,
                "At least one productId is required.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        var sellingUnitIds = body.SellingUnitIds;
        if (sellingUnitIds is { Count: > 0 } && sellingUnitIds.Count != productIds.Count)
        {
            problem = PosApiResults.Problem(
                ApplicationErrorCodes.OfflinePriceAuthorityRequestInvalid,
                "sellingUnitIds must be empty or the same length as productIds.",
                StatusCodes.Status400BadRequest);
            return false;
        }

        for (var index = 0; index < productIds.Count; index += 1)
        {
            if (productIds[index] == Guid.Empty)
            {
                problem = PosApiResults.Problem(
                    ApplicationErrorCodes.OfflinePriceAuthorityRequestInvalid,
                    "productIds must contain non-empty GUIDs.",
                    StatusCodes.Status400BadRequest);
                return false;
            }

            Guid? sellingUnitId = null;
            if (sellingUnitIds is { Count: > 0 } && sellingUnitIds[index] != Guid.Empty)
            {
                sellingUnitId = sellingUnitIds[index];
            }

            items.Add(new OfflinePriceAuthorityRequestItem(productIds[index], sellingUnitId));
        }

        return true;
    }

    private static OfflinePriceAuthorityDto Map(OfflinePriceAuthority authority) =>
        new(
            authority.AuthorityId,
            authority.OrganizationId,
            authority.BranchId,
            authority.ProductId,
            authority.SellingUnitId,
            authority.UnitPrice,
            authority.UnitOfMeasure,
            authority.SellingMode,
            authority.IssuedAtUtc,
            authority.ExpiresAtUtc,
            authority.Signature);
}
