using System.Globalization;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Offline;

namespace ExItS.PinoyBusinessPOS.ApiClient;

internal static class CustomerCreditOfflinePayloads
{
    internal sealed record CustomerCreatePayload(
        Guid CustomerId,
        string DisplayName,
        string? MobileNumber,
        string? Address,
        string? Notes);

    internal sealed record CustomerUpdatePayload(
        Guid CustomerId,
        string DisplayName,
        string? MobileNumber,
        string? Address,
        string? Notes,
        string ExpectedUpdatedAtUtc);

    internal sealed record CreditCreatePayload(
        Guid CreditEntryId,
        Guid CustomerId,
        string Amount,
        string Remarks);
}

/// <summary>Dispatches offline customer create operations to the POS API.</summary>
public sealed class CustomerCreateOfflineDispatcher(
    IPosCustomerClient client,
    ILocalCustomerCreditStore? localStore = null) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.CustomerCreate, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        CustomerCreditOfflinePayloads.CustomerCreatePayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<CustomerCreditOfflinePayloads.CustomerCreatePayload>(
                          plaintextPayload.Span,
                          JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        var result = await client.CreateAsync(
                new CreatePosCustomerRequest(
                    payload.DisplayName,
                    payload.MobileNumber,
                    payload.Address,
                    payload.Notes,
                    payload.CustomerId),
                new PosMutationIdempotencyHeaders(
                    envelope.IdempotencyKey,
                    envelope.PayloadHash,
                    envelope.OperationId,
                    OfflineOperationTypes.CustomerCreate),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore.UpsertServerCustomerAsync(MapCustomer(result.Data), ct).ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true,
                OfflineFailureClass.None,
                null,
                null,
                result.Data.CustomerId.ToString("D"));
        }

        return await HandleFailureAsync(localStore, payload.CustomerId, result, ct).ConfigureAwait(false);
    }

    internal static LocalCustomerProjection MapCustomer(PosCustomerDetailDto dto) =>
        new(
            dto.CustomerId,
            dto.OrganizationId,
            dto.DisplayName,
            dto.MobileNumber,
            dto.Address,
            dto.Notes,
            dto.Status,
            dto.CreatedAtUtc,
            dto.UpdatedAtUtc,
            LocalEntitySyncState.ServerConfirmed,
            null,
            dto.UpdatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
            null,
            null);

    internal static async Task<OfflineDispatchResult> HandleFailureAsync(
        ILocalCustomerCreditStore? localStore,
        Guid customerId,
        ApiResult<PosCustomerDetailDto> result,
        CancellationToken ct)
    {
        if (result.Status == ApiCallStatus.Conflict)
        {
            var conflictJson = SerializeConflict(result.Error);
            if (localStore is not null)
            {
                await localStore.MarkCustomerStateAsync(
                        customerId,
                        LocalEntitySyncState.Conflict,
                        conflictServerJson: conflictJson,
                        ct: ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                false,
                OfflineFailureClass.Conflict,
                result.Error?.ErrorCode ?? result.Status.ToString(),
                result.Error?.Detail,
                null,
                result.Error?.StatusCode ?? 409);
        }

        if (result.Status is ApiCallStatus.Validation or ApiCallStatus.NotFound
            || string.Equals(result.Error?.ErrorCode, ApplicationErrorCodes.MobileConflict, StringComparison.Ordinal))
        {
            if (localStore is not null)
            {
                await localStore.MarkCustomerStateAsync(
                        customerId,
                        LocalEntitySyncState.Rejected,
                        safeFailureCode: result.Error?.ErrorCode ?? result.Status.ToString(),
                        ct: ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                false,
                OfflineFailureClass.Permanent,
                result.Error?.ErrorCode ?? result.Status.ToString(),
                result.Error?.Detail,
                null,
                result.Error?.StatusCode);
        }

        return MapTransient(result);
    }

    internal static OfflineDispatchResult MapTransient<T>(ApiResult<T> result)
    {
        var failure = result.Status switch
        {
            ApiCallStatus.Offline or ApiCallStatus.Timeout or ApiCallStatus.Unavailable or ApiCallStatus.Cancelled
                => OfflineFailureClass.Transient,
            ApiCallStatus.Unauthorized or ApiCallStatus.Forbidden => OfflineFailureClass.AccessBlocked,
            ApiCallStatus.Conflict => OfflineFailureClass.Conflict,
            _ => OfflineFailureClass.Permanent
        };

        return new OfflineDispatchResult(
            false,
            failure,
            result.Error?.ErrorCode ?? result.Status.ToString(),
            result.Error?.Detail,
            null,
            result.Error?.StatusCode);
    }

    private static string? SerializeConflict(ApiError? error) =>
        error is null
            ? null
            : JsonSerializer.Serialize(new { statusCode = error.StatusCode, errorCode = error.ErrorCode }, JsonOptions);
}

/// <summary>Dispatches offline customer update operations to the POS API.</summary>
public sealed class CustomerUpdateOfflineDispatcher(
    IPosCustomerClient client,
    ILocalCustomerCreditStore? localStore = null) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.CustomerUpdate, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        CustomerCreditOfflinePayloads.CustomerUpdatePayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<CustomerCreditOfflinePayloads.CustomerUpdatePayload>(
                          plaintextPayload.Span,
                          JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        if (!DateTimeOffset.TryParse(
                payload.ExpectedUpdatedAtUtc,
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind,
                out var expectedUpdatedAtUtc))
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "concurrency_token_invalid", null, null);
        }

        var result = await client.UpdateAsync(
                payload.CustomerId,
                new UpdatePosCustomerRequest(
                    payload.DisplayName,
                    payload.MobileNumber,
                    payload.Address,
                    payload.Notes,
                    expectedUpdatedAtUtc),
                new PosMutationIdempotencyHeaders(
                    envelope.IdempotencyKey,
                    envelope.PayloadHash,
                    envelope.OperationId,
                    OfflineOperationTypes.CustomerUpdate),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore.UpsertServerCustomerAsync(CustomerCreateOfflineDispatcher.MapCustomer(result.Data), ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true,
                OfflineFailureClass.None,
                null,
                null,
                result.Data.CustomerId.ToString("D"));
        }

        return await CustomerCreateOfflineDispatcher.HandleFailureAsync(
                localStore,
                payload.CustomerId,
                result,
                ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Dispatches offline credit create operations to the POS API.</summary>
public sealed class CreditCreateOfflineDispatcher(
    IPosCustomerClient client,
    ILocalCustomerCreditStore? localStore = null) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.CreditCreate, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        CustomerCreditOfflinePayloads.CreditCreatePayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<CustomerCreditOfflinePayloads.CreditCreatePayload>(
                          plaintextPayload.Span,
                          JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        if (!decimal.TryParse(payload.Amount, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "amount_invalid", null, null);
        }

        var result = await client.CreateCreditEntryAsync(
                payload.CustomerId,
                new CreatePosCreditEntryRequest(amount, payload.Remarks, payload.CreditEntryId),
                new PosMutationIdempotencyHeaders(
                    envelope.IdempotencyKey,
                    envelope.PayloadHash,
                    envelope.OperationId,
                    OfflineOperationTypes.CreditCreate),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore.UpsertServerCreditAsync(MapCredit(result.Data), ct).ConfigureAwait(false);
                var summary = await client.GetCreditSummaryAsync(payload.CustomerId, ct).ConfigureAwait(false);
                if (summary.IsSuccess && summary.Data is not null)
                {
                    await localStore
                        .SetConfirmedOutstandingAsync(payload.CustomerId, summary.Data.OutstandingAmount, ct)
                        .ConfigureAwait(false);
                }

                await localStore.RebuildOptimisticBalancesAsync(payload.CustomerId, ct).ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true,
                OfflineFailureClass.None,
                null,
                null,
                result.Data.CreditEntryId.ToString("D"));
        }

        return await HandleCreditFailureAsync(localStore, payload.CreditEntryId, result, ct).ConfigureAwait(false);
    }

    private static LocalCreditProjection MapCredit(PosCreditEntryDto dto) =>
        new(
            dto.CreditEntryId,
            dto.CustomerId,
            dto.OrganizationId,
            dto.Amount,
            dto.Remarks,
            dto.Status,
            dto.CreatedAtUtc,
            LocalEntitySyncState.ServerConfirmed,
            null,
            null,
            null,
            dto.CurrentDueDate);

    private static async Task<OfflineDispatchResult> HandleCreditFailureAsync(
        ILocalCustomerCreditStore? localStore,
        Guid creditEntryId,
        ApiResult<PosCreditEntryDto> result,
        CancellationToken ct)
    {
        if (result.Status is ApiCallStatus.Validation or ApiCallStatus.NotFound)
        {
            if (localStore is not null)
            {
                await localStore.MarkCreditStateAsync(
                        creditEntryId,
                        LocalEntitySyncState.Rejected,
                        safeFailureCode: result.Error?.ErrorCode ?? result.Status.ToString(),
                        ct: ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                false,
                OfflineFailureClass.Permanent,
                result.Error?.ErrorCode ?? result.Status.ToString(),
                result.Error?.Detail,
                null,
                result.Error?.StatusCode);
        }

        if (result.Status == ApiCallStatus.Conflict)
        {
            return new OfflineDispatchResult(
                false,
                OfflineFailureClass.Conflict,
                result.Error?.ErrorCode ?? result.Status.ToString(),
                result.Error?.Detail,
                null,
                result.Error?.StatusCode ?? 409);
        }

        return CustomerCreateOfflineDispatcher.MapTransient(result);
    }
}
