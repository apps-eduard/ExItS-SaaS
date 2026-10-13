using ExItS.Platform.Api.Common;
using ExItS.Platform.Application.Geography;

namespace ExItS.Platform.Api.Reference;

public static class GeographyEndpoints
{
    public static void MapGeographyEndpoints(this WebApplication app)
    {
        var root = app.MapGroup("/api/v1/platform/reference/geography");

        root.MapGet("/countries", (ListCountries useCase, HttpContext http) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return PlatformApiResults.FromResult(useCase.Execute(), Results.Ok);
        });

        root.MapGet("/countries/{countryCode}/config", (
            string countryCode,
            GetCountryAddressConfig useCase,
            HttpContext http) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return PlatformApiResults.FromResult(useCase.Execute(countryCode), Results.Ok);
        });

        root.MapGet("/countries/{countryCode}/areas", (
            string countryCode,
            ListAdministrativeAreas useCase,
            HttpContext http) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return PlatformApiResults.FromResult(useCase.Execute(countryCode), Results.Ok);
        });

        root.MapGet("/cities/{cityId}/barangays", (
            string cityId,
            ListGeographyBarangays useCase,
            HttpContext http) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return PlatformApiResults.FromResult(useCase.Execute(cityId), Results.Ok);
        });

        root.MapGet("/areas/{areaId}/cities", (
            string areaId,
            ListGeographyCities useCase,
            HttpContext http) =>
        {
            if (http.User.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            return PlatformApiResults.FromResult(useCase.Execute(areaId), Results.Ok);
        });
    }
}
