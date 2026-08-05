using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Payments;

namespace ExItS.PinoyBusinessPOS.Application.Abstractions;

/// <summary>
/// Typed POS payment-attempt client. Online-only — offline calls fail fast; clients never mark Paid.
/// </summary>
public interface IPosPaymentAttemptClient
{
    Task<ApiResult<PaymentAttemptDto>> CreateAsync(
        Guid saleId,
        CreatePaymentAttemptRequest request,
        CancellationToken ct = default);

    Task<ApiResult<PaymentAttemptDto>> GetAsync(Guid attemptId, CancellationToken ct = default);

    Task<ApiResult<PaymentAttemptDto>> CancelAsync(Guid attemptId, CancellationToken ct = default);

    Task<ApiResult<PaymentAttemptDto>> SimulateAsync(
        Guid attemptId,
        SimulatePaymentRequest request,
        CancellationToken ct = default);
}
