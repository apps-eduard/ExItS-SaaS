using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Privacy;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PosPrivacyReadinessClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosPrivacyReadinessClient
{
    private const string Path = "/api/v1/pos/privacy-readiness";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<OrganizationPrivacyReadinessDto>> GetAsync(CancellationToken ct = default) =>
        SendAsync(HttpMethod.Get, Path, ct);

    private async Task<ApiResult<OrganizationPrivacyReadinessDto>> SendAsync(
        HttpMethod method,
        string path,
        CancellationToken ct)
    {
        if (connectivityService is not null && !await connectivityService.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return new ApiResult<OrganizationPrivacyReadinessDto>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Offline", "No network connectivity detected.", null, null, null)
            };
        }

        try
        {
            using var request = new HttpRequestMessage(method, path);
            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<OrganizationPrivacyReadinessDto>
                {
                    Status = response.StatusCode switch
                    {
                        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ApiCallStatus.Unauthorized,
                        HttpStatusCode.NotFound => ApiCallStatus.NotFound,
                        _ => ApiCallStatus.Failed
                    },
                    Error = new ApiError(response.ReasonPhrase ?? "Request failed", content, null, null, null)
                };
            }

            var data = JsonSerializer.Deserialize<OrganizationPrivacyReadinessDto>(content, JsonOptions);
            return data is null
                ? new ApiResult<OrganizationPrivacyReadinessDto>
                {
                    Status = ApiCallStatus.Failed,
                    Error = new ApiError("Invalid response", "The API returned no content.", null, null, null)
                }
                : ApiResult<OrganizationPrivacyReadinessDto>.Success(data);
        }
        catch (HttpRequestException ex)
        {
            return new ApiResult<OrganizationPrivacyReadinessDto>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Network unavailable", ex.Message, null, null, null)
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new ApiResult<OrganizationPrivacyReadinessDto> { Status = ApiCallStatus.Cancelled };
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return new ApiResult<OrganizationPrivacyReadinessDto>
            {
                Status = ApiCallStatus.Timeout,
                Error = new ApiError("Request timed out", ex.Message, null, null, null)
            };
        }
    }
}
