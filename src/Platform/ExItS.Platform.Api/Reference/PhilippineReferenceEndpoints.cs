using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Reference;

namespace ExItS.Platform.Api.Reference;

public static class PhilippineReferenceEndpoints
{
    public static void MapPhilippineReferenceEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/platform/reference/ph");

        root.MapGet("/localities", (
            string? query,
            int? limit,
            SearchPhilippineLocalities useCase,
            HttpContext http) =>
        {
            // Authenticated Platform session required. POS personal customer tokens do not authenticate here.
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return PlatformApiResults.FromResult(useCase.Execute(query, limit), Results.Ok);
        });
    }
}
