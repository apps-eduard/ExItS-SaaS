using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.UnitTests.Support;

internal sealed class FakeLocalValidationPaymentProvider : IPaymentProvider
{
    private readonly InMemoryProviderPaymentRepository _payments;
    private readonly IClock _clock;

    public FakeLocalValidationPaymentProvider(InMemoryProviderPaymentRepository payments, IClock clock)
    {
        _payments = payments;
        _clock = clock;
    }

    public string ProviderName => "LocalValidation";
    public bool IsTestProvider => true;

    public Task<PaymentProviderResult> ChargeAsync(PaymentChargeRequest request, CancellationToken ct = default) =>
        SimulateAsync("Succeeded", request, ct);

    public async Task<PaymentProviderResult> SimulateAsync(
        string simulation,
        PaymentChargeRequest request,
        CancellationToken ct = default)
    {
        var existing = await _payments.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return ToResult(existing);
        }

        var status = MapSimulation(simulation);
        var sequence = await _payments.GetNextSequenceAsync(ct).ConfigureAwait(false);
        var reference = $"test_pay_{sequence:D6}";
        var (failureCode, failureMessage) = status switch
        {
            PaymentProviderResultStatus.Declined => ("declined", "Simulated decline."),
            PaymentProviderResultStatus.Failed => ("failed", "Simulated failure."),
            PaymentProviderResultStatus.RenewalFailed => ("renewal_failed", "Simulated renewal failure."),
            _ => ((string?)null, (string?)null)
        };

        var result = new PaymentProviderResult(
            status,
            ProviderName,
            reference,
            request.Amount,
            request.CurrencyCode,
            IsTest: true,
            failureCode,
            failureMessage,
            request.IdempotencyKey);

        var record = ProviderPayment.FromResult(
            PlatformOrganizationId.From(request.OrganizationId),
            SubscriptionId.From(request.SubscriptionId),
            result,
            request.Purpose,
            _clock.UtcNow);

        await _payments.AddAsync(record, ct).ConfigureAwait(false);
        return result;
    }

    private static PaymentProviderResult ToResult(ProviderPayment payment) =>
        new(
            payment.Status,
            payment.Provider,
            payment.ProviderReference,
            payment.Amount,
            payment.CurrencyCode,
            payment.IsTest,
            payment.FailureCode,
            payment.FailureMessage,
            payment.IdempotencyKey);

    private static PaymentProviderResultStatus MapSimulation(string simulation) =>
        simulation.Trim().ToLowerInvariant().Replace('_', '-') switch
        {
            "succeed" or "success" or "succeeded" => PaymentProviderResultStatus.Succeeded,
            "decline" or "declined" => PaymentProviderResultStatus.Declined,
            "pending" => PaymentProviderResultStatus.Pending,
            "fail" or "failed" => PaymentProviderResultStatus.Failed,
            "refund" or "refunded" => PaymentProviderResultStatus.Refunded,
            "renewal-succeed" or "renewal-succeeded" or "renewalsucceeded" => PaymentProviderResultStatus.RenewalSucceeded,
            "renewal-fail" or "renewal-failed" or "renewalfailed" => PaymentProviderResultStatus.RenewalFailed,
            _ => PaymentProviderResultStatus.Succeeded
        };
}
