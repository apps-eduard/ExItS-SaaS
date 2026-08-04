using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.Abstractions;
using ExItS.PinoyBusinessPOS.Application.Auth;
using ExItS.PinoyBusinessPOS.Application.Common;

namespace ExItS.PinoyBusinessPOS.Api.Common;

internal static class PosIdempotencyEndpointHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    public static async Task<IResult> ExecuteMutationAsync<TDomain, TDto>(
        HttpRequest request,
        Guid organizationId,
        string operationType,
        IPosIdempotencyService idempotency,
        Func<CancellationToken, Task<ApplicationResult<TDomain>>> executeUseCase,
        Func<TDomain, TDto> map,
        Func<TDto, IResult> onCreated,
        CancellationToken ct)
    {
        if (!PosIdempotencyRequestHeaders.TryRead(request, out var headers, out var validationProblem))
        {
            return validationProblem!;
        }

        if (!headers.IsPresent)
        {
            var direct = await executeUseCase(ct).ConfigureAwait(false);
            return PosApiResults.FromResult(direct, d => onCreated(map(d)));
        }

        try
        {
            var outcome = await idempotency.ExecuteAsync(
                    new PosIdempotencyRequest(
                        organizationId,
                        PosProductCodes.PinoyBusinessPos,
                        operationType,
                        headers.IdempotencyKey!,
                        headers.PayloadHash!,
                        headers.OperationId),
                    async ct2 =>
                    {
                        var result = await executeUseCase(ct2).ConfigureAwait(false);
                        if (!result.IsSuccess)
                        {
                            throw new PosIdempotencyAbortException(result.ErrorCode!, result.ErrorMessage!);
                        }

                        var dto = map(result.Value!);
                        var reference = ExtractServerReference(dto);
                        return new PosIdempotencyExecutionResult(
                            "succeeded",
                            JsonSerializer.Serialize(dto, JsonOptions),
                            reference);
                    },
                    ct)
                .ConfigureAwait(false);

            if (outcome.IsConflict)
            {
                return Results.Conflict(new
                {
                    outcome.IsReplay,
                    outcome.IsConflict,
                    outcome.OutcomeCode,
                    outcome.ServerReference,
                    outcome.OutcomeBodyJson
                });
            }

            if (outcome.IsReplay)
            {
                var replay = DeserializeOutcome<TDto>(outcome.OutcomeBodyJson);
                return replay is null
                    ? PosApiResults.Problem(
                        ApplicationErrorCodes.DomainViolation,
                        "Stored idempotency outcome could not be deserialized.",
                        StatusCodes.Status500InternalServerError)
                    : Results.Ok(replay);
            }

            var created = DeserializeOutcome<TDto>(outcome.OutcomeBodyJson);
            return created is null
                ? PosApiResults.Problem(
                    ApplicationErrorCodes.DomainViolation,
                    "Idempotency outcome could not be deserialized.",
                    StatusCodes.Status500InternalServerError)
                : onCreated(created);
        }
        catch (PosIdempotencyAbortException ex)
        {
            return PosApiResults.Problem(ex.ErrorCode, ex.Message, PosApiResults.MapStatusCode(ex.ErrorCode));
        }
    }

    private static TDto? DeserializeOutcome<TDto>(string? json) =>
        string.IsNullOrWhiteSpace(json) ? default : JsonSerializer.Deserialize<TDto>(json, JsonOptions);

    private static string? ExtractServerReference<TDto>(TDto dto) => dto switch
    {
        Application.Customers.POSCustomerDto c => c.CustomerId.ToString("D"),
        Application.Credit.CreditEntryDto e => e.CreditEntryId.ToString("D"),
        Application.Payments.RepaymentDto r => r.RepaymentId.ToString("D"),
        Application.Payments.PosRepaymentDto p => p.RepaymentId.ToString("D"),
        Application.Sales.PosSaleDto s => s.SaleId.ToString("D"),
        Application.Expenses.PosExpenseDto e => e.ExpenseId.ToString("D"),
        Application.Purchasing.PosPurchaseOrderDto p => p.PurchaseOrderId.ToString("D"),
        Application.Purchasing.PosGoodsReceiptDto g => g.GoodsReceiptId.ToString("D"),
        Application.CashierShifts.PosCashierShiftMovementDto m => m.MovementId.ToString("D"),
        Application.Returns.PosSaleReturnDto r => r.ReturnId.ToString("D"),
        Application.Permissions.PosRoleAssignmentDto a => a.AssignmentId.ToString("D"),
        Application.Catalog.PosCatalogImportJobDto j => j.JobId.ToString("D"),
        _ => null
    };
}
