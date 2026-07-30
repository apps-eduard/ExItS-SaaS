using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Suppliers;

namespace ExItS.PinoyBusinessPOS.ApiClient;

/// <summary>
/// Typed POS supplier client. Online-only for P10-WP01: offline calls fail fast with
/// <see cref="ApiCallStatus.Offline"/> and no mutation is ever queued locally.
/// </summary>
public sealed class PosSupplierClient(HttpClient httpClient, IConnectivityService? connectivityService = null)
    : IPosSupplierClient
{
    private const string Path = "/api/v1/pos/suppliers";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public Task<ApiResult<PagedResult<PosSupplierDto>>> ListAsync(
        string? supplierCode = null,
        string? name = null,
        string? contactPerson = null,
        string? email = null,
        string? mobile = null,
        string? taxOrRegistrationNumber = null,
        string? status = null,
        int page = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StringBuilder(Path).Append('?');
        query.Append("page=").Append(page.ToString(CultureInfo.InvariantCulture));
        query.Append("&pageSize=").Append(pageSize.ToString(CultureInfo.InvariantCulture));
        AppendOptional(query, "supplierCode", supplierCode);
        AppendOptional(query, "name", name);
        AppendOptional(query, "contactPerson", contactPerson);
        AppendOptional(query, "email", email);
        AppendOptional(query, "mobile", mobile);
        AppendOptional(query, "taxOrRegistrationNumber", taxOrRegistrationNumber);
        AppendOptional(query, "status", status);
        return SendAsync<PagedResult<PosSupplierDto>>(HttpMethod.Get, query.ToString(), null, ct);
    }

    public Task<ApiResult<PosSupplierDto>> GetAsync(Guid supplierId, CancellationToken ct = default) =>
        SendAsync<PosSupplierDto>(HttpMethod.Get, $"{Path}/{supplierId:D}", null, ct);

    public Task<ApiResult<PosSupplierDto>> CreateAsync(CreateSupplierRequest request, CancellationToken ct = default) =>
        SendAsync<PosSupplierDto>(HttpMethod.Post, Path, request, ct);

    public Task<ApiResult<PosSupplierDto>> UpdateAsync(
        Guid supplierId,
        UpdateSupplierRequest request,
        CancellationToken ct = default) =>
        SendAsync<PosSupplierDto>(HttpMethod.Put, $"{Path}/{supplierId:D}", request, ct);

    public Task<ApiResult<PosSupplierDto>> ActivateAsync(Guid supplierId, CancellationToken ct = default) =>
        SendAsync<PosSupplierDto>(HttpMethod.Post, $"{Path}/{supplierId:D}/activate", null, ct);

    public Task<ApiResult<PosSupplierDto>> DeactivateAsync(Guid supplierId, CancellationToken ct = default) =>
        SendAsync<PosSupplierDto>(HttpMethod.Post, $"{Path}/{supplierId:D}/deactivate", null, ct);

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
