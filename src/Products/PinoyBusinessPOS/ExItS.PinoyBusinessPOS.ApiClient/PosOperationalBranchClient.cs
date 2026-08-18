using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Branches;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public sealed class PosOperationalBranchClient(
    HttpClient httpClient,
    IConnectivityService? connectivityService = null) : IPosOperationalBranchClient
{
    private const string Path = "/api/v1/pos/operational-branch";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public async Task<ApiResult<OperationalBranchContextDto>> SelectAsync(
        SelectOperationalBranchRequest request,
        CancellationToken ct = default)
    {
        if (connectivityService is not null && !await connectivityService.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return ApiResult<OperationalBranchContextDto>.Offline(
                new ApiError("Offline", "No network connectivity detected.", null, null, null));
        }

        try
        {
            using var response = await httpClient
                .PutAsJsonAsync(Path, request, JsonOptions, ct)
                .ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var correlationId = response.Headers.TryGetValues("X-Correlation-ID", out var values)
                ? values.FirstOrDefault()
                : null;

            if (!response.IsSuccessStatusCode)
            {
                return ApiResult<OperationalBranchContextDto>.Failure(
                    Classify(response.StatusCode),
                    ApiProblemParser.Parse(content, correlationId, (int)response.StatusCode));
            }

            var dto = JsonSerializer.Deserialize<OperationalBranchContextDto>(content, JsonOptions);
            return dto is null
                ? ApiResult<OperationalBranchContextDto>.Failed(
                    new ApiError("Invalid response", "The API returned no content.", null, null, null))
                : ApiResult<OperationalBranchContextDto>.Success(dto);
        }
        catch (HttpRequestException)
        {
            return ApiResult<OperationalBranchContextDto>.Unavailable();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return ApiResult<OperationalBranchContextDto>.Cancelled();
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiResult<OperationalBranchContextDto>.Timeout();
        }
    }

    private static ApiCallStatus Classify(HttpStatusCode statusCode) => statusCode switch
    {
        HttpStatusCode.NotFound => ApiCallStatus.NotFound,
        HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity => ApiCallStatus.Validation,
        HttpStatusCode.Conflict => ApiCallStatus.Conflict,
        HttpStatusCode.Unauthorized => ApiCallStatus.Unauthorized,
        HttpStatusCode.Forbidden => ApiCallStatus.Forbidden,
        HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ApiCallStatus.Timeout,
        _ when (int)statusCode >= 500 => ApiCallStatus.Unavailable,
        _ => ApiCallStatus.Failed
    };
}
