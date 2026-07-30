using System.Globalization;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Payments;

namespace ExItS.PinoyBusinessPOS.ApiClient;

internal static class PaymentOfflinePayloads
{
    internal sealed record RepaymentCreatePayload(
        Guid RepaymentId,
        Guid CustomerId,
        string Amount,
        string? Remarks);

    internal sealed record RepaymentReversePayload(
        Guid RepaymentId,
        Guid CustomerId,
        string Reason);

    internal sealed record CreditReversePayload(
        Guid CreditEntryId,
        Guid CustomerId,
        string Reason);

    internal sealed record CreditDueDatePayload(
        Guid CreditEntryId,
        Guid CustomerId,
        string? NewDueDate,
        string Reason,
        bool IsClear,
        string? ExpectedCurrentDueDate,
        bool CheckExpectedDueDate);
}

/// <summary>Dispatches offline repayment create operations to the POS API.</summary>
public sealed class RepaymentCreateOfflineDispatcher(
    IPosCustomerClient client,
    ILocalCustomerCreditStore? localStore = null) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.RepaymentCreate, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        PaymentOfflinePayloads.RepaymentCreatePayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaymentOfflinePayloads.RepaymentCreatePayload>(
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

        var result = await client.CreateRepaymentAsync(
                payload.CustomerId,
                new CreatePosRepaymentRequest(amount, payload.Remarks, payload.RepaymentId),
                new PosMutationIdempotencyHeaders(
                    envelope.IdempotencyKey,
                    envelope.PayloadHash,
                    envelope.OperationId,
                    OfflineOperationTypes.RepaymentCreate),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore.UpsertServerRepaymentAsync(MapRepayment(result.Data), ct).ConfigureAwait(false);
                await RefreshCustomerFinancialsAsync(client, localStore, payload.CustomerId, ct).ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true,
                OfflineFailureClass.None,
                null,
                null,
                result.Data.RepaymentId.ToString("D"));
        }

        return await HandleRepaymentFailureAsync(localStore, payload.RepaymentId, result, ct).ConfigureAwait(false);
    }

    internal static LocalRepaymentProjection MapRepayment(PosRepaymentDto dto) =>
        new(
            dto.RepaymentId,
            dto.CustomerId,
            dto.OrganizationId,
            dto.Amount,
            dto.Remarks,
            dto.Status,
            dto.RecordedAtUtc,
            LocalEntitySyncState.ServerConfirmed,
            null,
            null,
            null);

    internal static async Task RefreshCustomerFinancialsAsync(
        IPosCustomerClient client,
        ILocalCustomerCreditStore localStore,
        Guid customerId,
        CancellationToken ct)
    {
        var summary = await client.GetUtangSummaryAsync(customerId, ct).ConfigureAwait(false);
        if (summary.IsSuccess && summary.Data is not null)
        {
            await localStore
                .SetConfirmedOutstandingAsync(customerId, summary.Data.OutstandingAmount, ct)
                .ConfigureAwait(false);
        }

        await localStore.RebuildOptimisticBalancesAsync(customerId, ct).ConfigureAwait(false);
    }

    private static async Task<OfflineDispatchResult> HandleRepaymentFailureAsync(
        ILocalCustomerCreditStore? localStore,
        Guid repaymentId,
        ApiResult<PosRepaymentDto> result,
        CancellationToken ct)
    {
        if (result.Status is ApiCallStatus.Validation or ApiCallStatus.NotFound)
        {
            if (localStore is not null)
            {
                await localStore.MarkRepaymentStateAsync(
                        repaymentId,
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

        return PaymentOfflineDispatchHelpers.MapTransient(result);
    }
}

/// <summary>Dispatches offline repayment reverse operations to the POS API.</summary>
public sealed class RepaymentReverseOfflineDispatcher(
    IPosCustomerClient client,
    ILocalCustomerCreditStore? localStore = null) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.RepaymentReverse, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        PaymentOfflinePayloads.RepaymentReversePayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaymentOfflinePayloads.RepaymentReversePayload>(
                          plaintextPayload.Span,
                          JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        var result = await client.ReverseRepaymentAsync(
                payload.RepaymentId,
                new ReversePosRepaymentRequest(payload.Reason),
                new PosMutationIdempotencyHeaders(
                    envelope.IdempotencyKey,
                    envelope.PayloadHash,
                    envelope.OperationId,
                    OfflineOperationTypes.RepaymentReverse),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore.UpsertServerRepaymentAsync(RepaymentCreateOfflineDispatcher.MapRepayment(result.Data), ct)
                    .ConfigureAwait(false);
                await RepaymentCreateOfflineDispatcher
                    .RefreshCustomerFinancialsAsync(client, localStore, payload.CustomerId, ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true,
                OfflineFailureClass.None,
                null,
                null,
                result.Data.RepaymentId.ToString("D"));
        }

        if (result.Status is ApiCallStatus.Validation or ApiCallStatus.NotFound)
        {
            if (localStore is not null)
            {
                await localStore.MarkRepaymentStateAsync(
                        payload.RepaymentId,
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

        return PaymentOfflineDispatchHelpers.MapTransient(result);
    }
}

/// <summary>Dispatches offline credit reverse operations to the POS API.</summary>
public sealed class CreditReverseOfflineDispatcher(
    IPosCustomerClient client,
    ILocalCustomerCreditStore? localStore = null) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.CreditReverse, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        PaymentOfflinePayloads.CreditReversePayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaymentOfflinePayloads.CreditReversePayload>(
                          plaintextPayload.Span,
                          JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        var result = await client.ReverseCreditEntryAsync(
                payload.CustomerId,
                payload.CreditEntryId,
                new ReversePosCreditEntryRequest(payload.Reason),
                new PosMutationIdempotencyHeaders(
                    envelope.IdempotencyKey,
                    envelope.PayloadHash,
                    envelope.OperationId,
                    OfflineOperationTypes.CreditReverse),
                ct)
            .ConfigureAwait(false);

        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore.UpsertServerCreditAsync(PaymentOfflineDispatchHelpers.MapCredit(result.Data), ct)
                    .ConfigureAwait(false);
                await RepaymentCreateOfflineDispatcher
                    .RefreshCustomerFinancialsAsync(client, localStore, payload.CustomerId, ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true,
                OfflineFailureClass.None,
                null,
                null,
                result.Data.CreditEntryId.ToString("D"));
        }

        return await PaymentOfflineDispatchHelpers.HandleCreditFailureAsync(
                localStore,
                payload.CreditEntryId,
                result,
                ct)
            .ConfigureAwait(false);
    }
}

/// <summary>Dispatches offline credit due-date set/clear operations to the POS API.</summary>
public sealed class CreditDueDateSetOfflineDispatcher(
    IPosCustomerClient client,
    ILocalCustomerCreditStore? localStore = null) : IOfflineOperationDispatcher
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public bool CanHandle(string operationType) =>
        string.Equals(operationType, OfflineOperationTypes.CreditDueDateSet, StringComparison.Ordinal)
        || string.Equals(operationType, OfflineOperationTypes.CreditDueDateClear, StringComparison.Ordinal);

    public async Task<OfflineDispatchResult> DispatchAsync(
        OfflineOperationEnvelope envelope,
        ReadOnlyMemory<byte> plaintextPayload,
        CancellationToken ct = default)
    {
        PaymentOfflinePayloads.CreditDueDatePayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<PaymentOfflinePayloads.CreditDueDatePayload>(
                          plaintextPayload.Span,
                          JsonOptions)
                      ?? throw new JsonException("Null payload.");
        }
        catch (JsonException)
        {
            return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "payload_invalid", null, null);
        }

        DateOnly? expectedDueDate = null;
        if (payload.CheckExpectedDueDate && !string.IsNullOrEmpty(payload.ExpectedCurrentDueDate))
        {
            if (!DateOnly.TryParse(
                    payload.ExpectedCurrentDueDate,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var parsedExpected))
            {
                return new OfflineDispatchResult(
                    false,
                    OfflineFailureClass.Permanent,
                    "concurrency_token_invalid",
                    null,
                    null);
            }

            expectedDueDate = parsedExpected;
        }

        DateOnly? newDueDate = null;
        if (!string.IsNullOrEmpty(payload.NewDueDate))
        {
            if (!DateOnly.TryParse(payload.NewDueDate, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDueDate))
            {
                return new OfflineDispatchResult(false, OfflineFailureClass.Permanent, "due_date_invalid", null, null);
            }

            newDueDate = parsedDueDate;
        }

        ApiResult<PosCreditEntryDto> result;
        if (payload.IsClear)
        {
            result = await client.ClearCreditDueDateAsync(
                    payload.CreditEntryId,
                    new ClearPosCreditDueDateRequest(
                        payload.Reason,
                        expectedDueDate,
                        payload.CheckExpectedDueDate),
                    new PosMutationIdempotencyHeaders(
                        envelope.IdempotencyKey,
                        envelope.PayloadHash,
                        envelope.OperationId,
                        OfflineOperationTypes.CreditDueDateClear),
                    ct)
                .ConfigureAwait(false);
        }
        else
        {
            result = await client.SetCreditDueDateAsync(
                    payload.CreditEntryId,
                    new SetPosCreditDueDateRequest(
                        newDueDate,
                        payload.Reason,
                        expectedDueDate,
                        payload.CheckExpectedDueDate),
                    new PosMutationIdempotencyHeaders(
                        envelope.IdempotencyKey,
                        envelope.PayloadHash,
                        envelope.OperationId,
                        OfflineOperationTypes.CreditDueDateSet),
                    ct)
                .ConfigureAwait(false);
        }

        if (result.IsSuccess && result.Data is not null)
        {
            if (localStore is not null)
            {
                await localStore.UpsertServerCreditAsync(PaymentOfflineDispatchHelpers.MapCredit(result.Data), ct)
                    .ConfigureAwait(false);
                await RepaymentCreateOfflineDispatcher
                    .RefreshCustomerFinancialsAsync(client, localStore, payload.CustomerId, ct)
                    .ConfigureAwait(false);
            }

            return new OfflineDispatchResult(
                true,
                OfflineFailureClass.None,
                null,
                null,
                result.Data.CreditEntryId.ToString("D"));
        }

        return await PaymentOfflineDispatchHelpers.HandleCreditFailureAsync(
                localStore,
                payload.CreditEntryId,
                result,
                ct)
            .ConfigureAwait(false);
    }
}

internal static class PaymentOfflineDispatchHelpers
{
    internal static LocalCreditProjection MapCredit(PosCreditEntryDto dto) =>
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

    internal static async Task<OfflineDispatchResult> HandleCreditFailureAsync(
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
}
