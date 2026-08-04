using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Registers;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS register client. Online-only for P10-WP07: offline calls fail fast with
/// <see cref="ApiCallStatus.Offline"/> and no mutation is ever queued locally.
/// </summary>
public sealed class PosRegisterClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosRegisterClient
{
    private const string Path = "/api/v1/pos/registers";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PagedResult<PosRegisterDto>>> ListAsync(
        string? registerCode = null,
        string? name = null,
        string? status = null,
        bool? hasOpenShift = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(Path).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "registerCode", registerCode);
        AppendOptional(query, "name", name);
        AppendOptional(query, "status", status);
        if (hasOpenShift is not null)
        {
            query.Append("&hasOpenShift=").Append(hasOpenShift.Value ? "true" : "false");
        }

        return SendAsync<PagedResult<PosRegisterDto>>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public async Task<ApiResult<IReadOnlyList<PosRegisterSummaryDto>>> ListAvailableForShiftAsync(
        CancellationToken ct = default)
    {
        // Deserialize to List<> (not IReadOnlyList<>) — STJ is reliable with concrete collections.
        var result = await SendAsync<List<PosRegisterSummaryDto>>(
                HttpMethod.Get,
                $"{Path}/available-for-shift",
                null,
                ct)
            .ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            return new ApiResult<IReadOnlyList<PosRegisterSummaryDto>>
            {
                Status = result.Status,
                Error = result.Error
            };
        }

        IReadOnlyList<PosRegisterSummaryDto> items = result.Data ?? [];
        return ApiResult<IReadOnlyList<PosRegisterSummaryDto>>.Success(items);
    }

    public Task<ApiResult<PosRegisterDto>> GetAsync(Guid registerId, CancellationToken ct = default) =>
        SendAsync<PosRegisterDto>(HttpMethod.Get, $"{Path}/{registerId:D}", null, ct);

    public Task<ApiResult<PosRegisterActivityDto>> GetActivityAsync(
        Guid registerId,
        DateTimeOffset? fromUtc = null,
        DateTimeOffset? toUtc = null,
        CancellationToken ct = default)
    {
        var query = new StringBuilder($"{Path}/{registerId:D}/activity?");
        if (fromUtc is not null)
        {
            query.Append("fromUtc=").Append(Uri.EscapeDataString(fromUtc.Value.ToString("O")));
        }

        if (toUtc is not null)
        {
            if (fromUtc is not null)
            {
                query.Append('&');
            }

            query.Append("toUtc=").Append(Uri.EscapeDataString(toUtc.Value.ToString("O")));
        }

        return SendAsync<PosRegisterActivityDto>(HttpMethod.Get, query.ToString().TrimEnd('?'), null, ct);
    }

    public Task<ApiResult<PosRegisterDto>> CreateAsync(CreateRegisterRequest request, CancellationToken ct = default) =>
        SendAsync<PosRegisterDto>(HttpMethod.Post, Path, request, ct);

    public Task<ApiResult<PosRegisterDto>> UpdateAsync(
        Guid registerId,
        UpdateRegisterRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosRegisterDto>(HttpMethod.Put, $"{Path}/{registerId:D}", request, ct);

    public Task<ApiResult<PosRegisterDto>> ActivateAsync(Guid registerId, CancellationToken ct = default) =>
        SendAsync<PosRegisterDto>(HttpMethod.Post, $"{Path}/{registerId:D}/activate", null, ct);

    public Task<ApiResult<PosRegisterDto>> DeactivateAsync(Guid registerId, CancellationToken ct = default) =>
        SendAsync<PosRegisterDto>(HttpMethod.Post, $"{Path}/{registerId:D}/deactivate", null, ct);

    private static void AppendOptional(StringBuilder query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            query.Append('&').Append(name).Append('=').Append(Uri.EscapeDataString(value.Trim()));
        }
    }

    private async Task<ApiResult<TResponse>> SendAsync<TResponse>(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken ct)
    {
        if (connectivityService is not null && !await connectivityService.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Offline", "No network connectivity detected.", null, null, null)
            };
        }

        try
        {
            using var request = new HttpRequestMessage(method, path);
            if (body is not null)
            {
                request.Content = JsonContent.Create(body, options: JsonOptions);
            }

            using var response = await httpClient.SendAsync(request, ct).ConfigureAwait(false);
            var content = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var correlationId = response.Headers.TryGetValues("X-Correlation-ID", out var values)
                ? values.FirstOrDefault()
                : null;

            if (!response.IsSuccessStatusCode)
            {
                return new ApiResult<TResponse>
                {
                    Status = Classify(response.StatusCode),
                    Error = ParseProblem(content, correlationId, (int)response.StatusCode)
                };
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                return new ApiResult<TResponse>
                {
                    Status = ApiCallStatus.Failed,
                    Error = new ApiError("Invalid response", "The API returned no content.", null, null, null)
                };
            }

            var data = JsonSerializer.Deserialize<TResponse>(content, JsonOptions);
            return data is null
                ? new ApiResult<TResponse>
                {
                    Status = ApiCallStatus.Failed,
                    Error = new ApiError("Invalid response", "The API returned no content.", null, null, null)
                }
                : ApiResult<TResponse>.Success(data);
        }
        catch (HttpRequestException ex)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Offline,
                Error = new ApiError("Network unavailable", ex.Message, null, null, null)
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            return new ApiResult<TResponse> { Status = ApiCallStatus.Cancelled };
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            return new ApiResult<TResponse>
            {
                Status = ApiCallStatus.Timeout,
                Error = new ApiError("Request timed out", ex.Message, null, null, null)
            };
        }
    }

    private static ApiError ParseProblem(string content, string? correlationId, int statusCode)
    {
        string? title = null;
        string? detail = null;
        string? errorCode = null;
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var document = JsonDocument.Parse(content);
                var root = document.RootElement;
                if (root.TryGetProperty("title", out var t) && t.ValueKind == JsonValueKind.String)
                {
                    title = t.GetString();
                }

                if (root.TryGetProperty("detail", out var d) && d.ValueKind == JsonValueKind.String)
                {
                    detail = d.GetString();
                }

                if (root.TryGetProperty("errorCode", out var e) && e.ValueKind == JsonValueKind.String)
                {
                    errorCode = e.GetString();
                }
            }
            catch (JsonException)
            {
            }
        }

        return new ApiError(title, detail, errorCode, correlationId, statusCode);
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
