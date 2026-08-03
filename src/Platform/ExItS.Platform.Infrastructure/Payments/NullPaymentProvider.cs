using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Payments;

namespace ExItS.Platform.Infrastructure.Payments;

internal sealed class NullPaymentProvider : IPaymentProvider
{
    public string ProviderName => PaymentProviderNames.Manual;
    public bool IsTestProvider => false;

    public Task<PaymentProviderResult> ChargeAsync(PaymentChargeRequest request, CancellationToken ct = default) =>
        throw new NotSupportedException("Automated payment charging is not configured. Use manual SaaS payment recording.");

    public Task<PaymentProviderResult> SimulateAsync(string simulation, PaymentChargeRequest request, CancellationToken ct = default) =>
        throw new NotSupportedException("Payment simulation is only available with LocalValidation provider.");
}
