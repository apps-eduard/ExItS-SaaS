using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Commercial;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Domain.Common;

namespace ExItS.Platform.Api.Commercial;

internal static class CommercialEndpoints
{
    public static IEndpointRouteBuilder MapCommercialEndpoints(this IEndpointRouteBuilder app)
    {
        var commercial = app.MapGroup("/api/v1/commercial");

        commercial.MapGet("/plans", async (
            HttpContext http,
            string? productCode,
            CommercialCatalogQueryService queries,
            CancellationToken ct) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return PlatformApiResults.Problem(
                    ApplicationErrorCodes.SessionInvalid,
                    "Authentication is required.",
                    StatusCodes.Status401Unauthorized);
            }

            try
            {
                var plans = await queries.ListActivePlansAsync(productCode, ct).ConfigureAwait(false);
                return Results.Ok(plans);
            }
            catch (DomainException ex)
            {
                return PlatformApiResults.Problem(ex.ErrorCode, ex.Message, StatusCodes.Status400BadRequest);
            }
            catch (InvalidOperationException ex) when (ex.InnerException is DomainException domainEx)
            {
                return PlatformApiResults.Problem(domainEx.ErrorCode, domainEx.Message, StatusCodes.Status400BadRequest);
            }
        });

        return app;
    }
}
