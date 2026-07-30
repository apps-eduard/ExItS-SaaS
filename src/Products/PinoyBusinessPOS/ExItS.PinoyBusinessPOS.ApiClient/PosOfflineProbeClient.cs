using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.ApiClient;

public interface IPosOfflineProbeClient
{
    Task<ApiResult<DevOfflineProbeClientResponse>> SubmitAsync(
        DevOfflineProbeClientRequest request,
        CancellationToken ct = default);
}

public sealed record DevOfflineProbeClientRequest(
    Guid OperationId,
    string IdempotencyKey,
    string PayloadHash,
    string EchoToken,
    int PayloadVersion = 1,
    string? DeviceId = null);

public sealed record DevOfflineProbeClientResponse(
    bool IsReplay,
    bool IsConflict,
    string OutcomeCode,
    string? ServerReference,
    string? OutcomeBodyJson);

public sealed class PosOfflineProbeClient(HttpClient httpClient, IConnectivityService? connectivity = null)
    : IPosOfflineProbeClient
{
    public async Task<ApiResult<DevOfflineProbeClientResponse>> SubmitAsync(
        DevOfflineProbeClientRequest request,
        CancellationToken ct = default)
    {
        if (connectivity is not null && !await connectivity.IsConnectedAsync(ct).ConfigureAwait(false))
        {
            return ApiResult<DevOfflineProbeClientResponse>.Offline();
        }

        try
        {
            using var response = await httpClient
                .PostAsJsonAsync("/api/v1/pos/dev/offline-probe", request, ct)
                .ConfigureAwait(false);

            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                var conflict = await response.Content
                    .ReadFromJsonAsync<DevOfflineProbeClientResponse>(cancellationToken: ct)
                    .ConfigureAwait(false);
                return new ApiResult<DevOfflineProbeClientResponse>
                {
                    Status = ApiCallStatus.Conflict,
                    Data = conflict,
                    Error = new ApiError("Conflict", "Idempotency payload mismatch", "conflict_payload_mismatch", null, 409)
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                var apiStatus = status switch
                {
                    400 => ApiCallStatus.Validation,
                    401 => ApiCallStatus.Unauthorized,
                    403 => ApiCallStatus.Forbidden,
                    404 => ApiCallStatus.NotFound,
                    >= 500 => ApiCallStatus.Unavailable,
                    _ => ApiCallStatus.Failed
                };
                return ApiResult<DevOfflineProbeClientResponse>.Failure(
                    apiStatus,
                    new ApiError(response.ReasonPhrase, null, null, null, status));
            }

            var body = await response.Content
                .ReadFromJsonAsync<DevOfflineProbeClientResponse>(cancellationToken: ct)
                .ConfigureAwait(false);
            return body is null
                ? ApiResult<DevOfflineProbeClientResponse>.Failure(ApiCallStatus.Failed)
                : ApiResult<DevOfflineProbeClientResponse>.Success(body);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ApiResult<DevOfflineProbeClientResponse>.Timeout();
        }
        catch (HttpRequestException)
        {
            return ApiResult<DevOfflineProbeClientResponse>.Offline();
        }
    }
}

/// <summary>Dispatches Development/Testing offline probe operations to the POS API.</summary>
public sealed class DevOfflineProbeDispatcher(IPosOfflineProbeClient client) : IOfflineOperationDispatcher
{
    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.DevOfflineProbe, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        string echoToken;
        try
        {
            echoToken = Encoding.UTF8.GetString(plaintextPayload.Span);
            if (string.IsNullOrWhiteSpace(echoToken))
            {
                return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "empty_payload", null, null);
            }
        }
        catch
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_encoding", null, null);
        }

        var result = await client.SubmitAsync(
                new DevOfflineProbeClientRequest(
                    envelope.OperationId,
                    envelope.IdempotencyKey,
                    envelope.PayloadHash,
                    echoToken,
                    envelope.PayloadVersion,
                    envelope.DeviceId),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            if (result.Data.IsConflict)
            {
                return new OfflineDispatchResult(
                    false,
                    OfflineFailureClass.Conflict,
                    result.Data.OutcomeCode,
                    null,
                    result.Data.ServerReference,
                    409);
            }

            return new OfflineDispatchResult(
                true,
                OfflineFailureClass.None,
                null,
                null,
                result.Data.ServerReference);
        }

        var failure = MapFailure(result.Status, result.Error?.StatusCode);
        return new OfflineDispatchResult(
            false,
            failure,
            result.Status.ToString(),
            result.Error?.Title,
            null,
            result.Error?.StatusCode);
    }

    private static OfflineFailureClass MapFailure(ApiCallStatus status, int? http)
    {
        if (http is >= 500 and <= 599)
        {
            return OfflineFailureClass.Transient;
        }

        return status switch
        {
            ApiCallStatus.Offline or ApiCallStatus.Timeout or ApiCallStatus.Unavailable or ApiCallStatus.Cancelled
                => OfflineFailureClass.Transient,
            ApiCallStatus.Unauthorized or ApiCallStatus.Forbidden => OfflineFailureClass.AccessBlocked,
            ApiCallStatus.Conflict => OfflineFailureClass.Conflict,
            _ => OfflineFailureClass.Permanent
        };
    }
}
