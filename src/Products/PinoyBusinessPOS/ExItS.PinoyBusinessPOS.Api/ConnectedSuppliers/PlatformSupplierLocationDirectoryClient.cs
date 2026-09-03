using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.ConnectedSuppliers;

/// <summary>
/// Calls Platform public store branches API. Testing environment returns empty until faked in tests.
/// </summary>
public sealed class PlatformSupplierLocationDirectoryClient(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options,
    IHostEnvironment environment) : IPlatformSupplierLocationDirectory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>> ListActiveLocationsAsync(
        string publicOrganizationId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicOrganizationId))
        {
            return ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>.Failure(
                DomainErrorCodes.ConnectedSupplierRequiresBusinessQr,
                "Supplier public organization id is required.");
        }

        if (environment.IsEnvironment("Testing"))
        {
            // Default single Active location so RequestConnection auto-selects without Platform HTTP.
            return ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>.Success(
            [
                new PlatformSupplierLocationDto(
                    Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    "Main Branch",
                    "BR-MAIN",
                    IsPrimary: true)
            ]);
        }

        EnsureBaseAddress();
        var encoded = Uri.EscapeDataString(publicOrganizationId.Trim().ToUpperInvariant());
        using var request = CreateRequest(HttpMethod.Get, $"api/v1/public/stores/{encoded}/branches");
        using var response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var message = response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest
                ? "Supplier locations could not be loaded for that business."
                : $"Supplier location lookup failed with {(int)response.StatusCode}.";
            return ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>.Failure(
                DomainErrorCodes.ConnectedSupplierBranchInvalid,
                message);
        }

        var dto = await response.Content
            .ReadFromJsonAsync<PublicStoreBranchesResponse>(JsonOptions, cancellationToken)
            .ConfigureAwait(false);
        var branches = dto?.Branches ?? [];
        return ApplicationResult<IReadOnlyList<PlatformSupplierLocationDto>>.Success(
            branches
                .Where(b => b.BranchId != Guid.Empty && !string.IsNullOrWhiteSpace(b.Name))
                .Select(b => new PlatformSupplierLocationDto(
                    b.BranchId,
                    b.Name.Trim(),
                    string.IsNullOrWhiteSpace(b.Code) ? b.Name.Trim() : b.Code.Trim(),
                    b.IsPrimary))
                .ToList());
    }

    private void EnsureBaseAddress()
    {
        if (httpClient.BaseAddress is not null)
        {
            return;
        }

        var baseUrl = options.Value.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("PlatformAuth:BaseUrl is required for supplier location lookup.");
        }

        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(method, relativePath);
        PlatformCallerCredentialForwarder.CopyTo(httpContextAccessor.HttpContext?.Request, request);
        var token = PlatformCallerCredentialForwarder.ResolvePlatformSessionToken(
            httpContextAccessor.HttpContext?.Request);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("PlatformSession", token);
        }

        return request;
    }

    private sealed record PublicStoreBranchesResponse(
        string? PublicOrganizationId,
        string? DisplayName,
        IReadOnlyList<PublicStoreBranchItem>? Branches);

    private sealed record PublicStoreBranchItem(
        Guid BranchId,
        string Name,
        string? Code,
        bool IsPrimary);
}
