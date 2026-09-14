using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Api.ConnectedSuppliers;

/// <summary>
/// Calls Platform privacy-safe B2B business-contact projection.
/// Forwards the caller's Platform session; POS must gate Connected before calling.
/// </summary>
public sealed class PlatformConnectedBuyerBusinessContactDirectory(
    HttpClient httpClient,
    IHttpContextAccessor httpContextAccessor,
    IOptions<PlatformAuthOptions> options) : IConnectedBuyerBusinessContactDirectory
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<ApplicationResult<IReadOnlyList<BuyerOrganizationBusinessContactDto>>> ListAsync(
        Guid buyerOrganizationId,
        Guid requesterSupplierOrganizationId,
        string? search = null,
        CancellationToken ct = default)
    {
        EnsureBaseAddress();
        if (!HasSession())
        {
            return ApplicationResult<IReadOnlyList<BuyerOrganizationBusinessContactDto>>.Failure(
                ConnectedSupplierErrorCodes.OrganizationContactInvalid,
                "Your Platform sign-in is missing for this request. Refresh the page or sign in again, then retry.");
        }

        var path = new StringBuilder(
            $"api/v1/platform/organizations/{buyerOrganizationId:D}/b2b-business-contacts?requesterOrganizationId={requesterSupplierOrganizationId:D}");
        if (!string.IsNullOrWhiteSpace(search))
        {
            path.Append("&search=").Append(Uri.EscapeDataString(search.Trim()));
        }

        using var request = CreateRequest(HttpMethod.Get, path.ToString());
        using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return await FailureListAsync(response, ct).ConfigureAwait(false);
        }

        var items = await response.Content
            .ReadFromJsonAsync<List<PlatformB2bBusinessContactDto>>(JsonOptions, ct)
            .ConfigureAwait(false);
        var mapped = (items ?? [])
            .Select(Map)
            .ToList();
        return ApplicationResult<IReadOnlyList<BuyerOrganizationBusinessContactDto>>.Success(mapped);
    }

    public async Task<ApplicationResult<BuyerOrganizationBusinessContactDto?>> GetAsync(
        Guid buyerOrganizationId,
        Guid requesterSupplierOrganizationId,
        Guid organizationMemberId,
        CancellationToken ct = default)
    {
        EnsureBaseAddress();
        if (!HasSession())
        {
            return ApplicationResult<BuyerOrganizationBusinessContactDto?>.Failure(
                ConnectedSupplierErrorCodes.OrganizationContactInvalid,
                "Your Platform sign-in is missing for this request. Refresh the page or sign in again, then retry.");
        }

        var path =
            $"api/v1/platform/organizations/{buyerOrganizationId:D}/b2b-business-contacts/{organizationMemberId:D}?requesterOrganizationId={requesterSupplierOrganizationId:D}";
        using var request = CreateRequest(HttpMethod.Get, path);
        using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return ApplicationResult<BuyerOrganizationBusinessContactDto?>.Success(null);
        }

        if (!response.IsSuccessStatusCode)
        {
            return await FailureSingleAsync(response, ct).ConfigureAwait(false);
        }

        var dto = await response.Content
            .ReadFromJsonAsync<PlatformB2bBusinessContactDto>(JsonOptions, ct)
            .ConfigureAwait(false);
        return ApplicationResult<BuyerOrganizationBusinessContactDto?>.Success(
            dto is null ? null : Map(dto));
    }

    private bool HasSession() =>
        !string.IsNullOrWhiteSpace(
            PlatformCallerCredentialForwarder.ResolvePlatformSessionToken(
                httpContextAccessor.HttpContext?.Request));

    private void EnsureBaseAddress()
    {
        if (httpClient.BaseAddress is not null)
        {
            return;
        }

        var baseUrl = options.Value.BaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("PlatformAuth:BaseUrl is required for B2B business contacts.");
        }

        httpClient.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    }

    private HttpRequestMessage CreateRequest(HttpMethod method, string relativePath)
    {
        var request = new HttpRequestMessage(method, relativePath);
        var httpRequest = httpContextAccessor.HttpContext?.Request;
        PlatformCallerCredentialForwarder.CopyTo(httpRequest, request);
        var token = PlatformCallerCredentialForwarder.ResolvePlatformSessionToken(httpRequest);
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("PlatformSession", token);
            if (!request.Headers.Contains("X-ExItS-Session-Token"))
            {
                request.Headers.TryAddWithoutValidation("X-ExItS-Session-Token", token);
            }
        }

        return request;
    }

    private static BuyerOrganizationBusinessContactDto Map(PlatformB2bBusinessContactDto dto) =>
        new(
            dto.OrganizationMemberId,
            dto.UserId,
            dto.DisplayName,
            dto.RoleTitle,
            dto.IsOwner,
            dto.Department,
            dto.Phone,
            dto.Email,
            dto.EmployeeCode);

    private static async Task<ApplicationResult<IReadOnlyList<BuyerOrganizationBusinessContactDto>>> FailureListAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return ApplicationResult<IReadOnlyList<BuyerOrganizationBusinessContactDto>>.Failure(
            ConnectedSupplierErrorCodes.OrganizationContactInvalid,
            string.IsNullOrWhiteSpace(body)
                ? "Could not load organization contacts."
                : Truncate(body));
    }

    private static async Task<ApplicationResult<BuyerOrganizationBusinessContactDto?>> FailureSingleAsync(
        HttpResponseMessage response,
        CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        return ApplicationResult<BuyerOrganizationBusinessContactDto?>.Failure(
            ConnectedSupplierErrorCodes.OrganizationContactInvalid,
            string.IsNullOrWhiteSpace(body)
                ? "Could not validate organization contact."
                : Truncate(body));
    }

    private static string Truncate(string value) =>
        value.Length <= 400 ? value : value[..400];

    private sealed record PlatformB2bBusinessContactDto(
        Guid OrganizationMemberId,
        Guid UserId,
        string DisplayName,
        string RoleTitle,
        bool IsOwner,
        string? Department,
        string? Phone,
        string? Email,
        string? EmployeeCode);
}
