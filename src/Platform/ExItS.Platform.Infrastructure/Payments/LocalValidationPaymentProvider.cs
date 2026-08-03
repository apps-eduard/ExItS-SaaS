using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.Infrastructure.Payments;

internal sealed class LocalValidationPaymentProvider : IPaymentProvider
{
    public const string ProviderKey = PaymentProviderNames.LocalValidation;

    private readonly IProviderPaymentRepository _payments;
    private readonly IClock _clock;

    public LocalValidationPaymentProvider(IProviderPaymentRepository payments, IClock clock)
    {
        _payments = payments;
        _clock = clock;
    }

    public string ProviderName => ProviderKey;
    public bool IsTestProvider => true;

    public Task<PaymentProviderResult> ChargeAsync(PaymentChargeRequest request, CancellationToken ct = default) =>
        SimulateAsync("succeed", request, ct);

    public async Task<PaymentProviderResult> SimulateAsync(
        string simulation,
        PaymentChargeRequest request,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var existing = await _payments.GetByIdempotencyKeyAsync(request.IdempotencyKey, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return ToResult(existing);
        }

        var normalized = NormalizeSimulation(simulation);
        var status = MapSimulation(normalized);
        var sequence = await _payments.GetNextSequenceAsync(ct).ConfigureAwait(false);
        var reference = $"lvp_pay_{sequence:D6}";

        var (failureCode, failureMessage) = status switch
        {
            PaymentProviderResultStatus.Declined => ("declined", "Simulated decline."),
            PaymentProviderResultStatus.Failed => ("failed", "Simulated failure."),
            PaymentProviderResultStatus.RenewalFailed => ("renewal_failed", "Simulated renewal failure."),
            _ => ((string?)null, (string?)null)
        };

        var result = new PaymentProviderResult(
            status,
            ProviderKey,
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

    private static string NormalizeSimulation(string simulation) =>
        simulation.Trim().ToLowerInvariant().Replace('_', '-');

    private static PaymentProviderResultStatus MapSimulation(string simulation) =>
        simulation switch
        {
            "succeed" or "success" => PaymentProviderResultStatus.Succeeded,
            "decline" or "declined" => PaymentProviderResultStatus.Declined,
            "pending" => PaymentProviderResultStatus.Pending,
            "fail" or "failed" => PaymentProviderResultStatus.Failed,
            "refund" or "refunded" => PaymentProviderResultStatus.Refunded,
            "renewal-succeed" or "renewal-succeeded" => PaymentProviderResultStatus.RenewalSucceeded,
            "renewal-fail" or "renewal-failed" => PaymentProviderResultStatus.RenewalFailed,
            _ => PaymentProviderResultStatus.Succeeded
        };
}
