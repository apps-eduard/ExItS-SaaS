using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Payments;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.UnitTests.Payments;

public sealed class FakePaymentGatewayTests
{
    [Fact]
    public async Task CreateSession_is_idempotent_for_same_key_and_payload()
    {
        var gateway = new FakePaymentGateway();
        var attemptId = Guid.NewGuid();
        var request = new PaymentGatewayCreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attemptId,
            "Card",
            100m,
            "PHP",
            "idem-1");

        var first = await gateway.CreateSessionAsync(request);
        var second = await gateway.CreateSessionAsync(request);
        Assert.Equal(first.ProviderReference, second.ProviderReference);
        Assert.Equal($"fake_{attemptId:N}", first.ProviderReference);

        var loaded = await gateway.GetSessionAsync(first.ProviderReference);
        Assert.NotNull(loaded);
        Assert.Equal(first.CheckoutUrl, loaded!.CheckoutUrl);
    }

    [Fact]
    public async Task CreateSession_rejects_idempotency_payload_conflict()
    {
        var gateway = new FakePaymentGateway();
        var key = "idem-conflict";
        var org = Guid.NewGuid();
        var sale = Guid.NewGuid();
        var attempt = Guid.NewGuid();
        await gateway.CreateSessionAsync(
            new PaymentGatewayCreateRequest(org, sale, attempt, "Card", 50m, "PHP", key));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            gateway.CreateSessionAsync(
                new PaymentGatewayCreateRequest(org, sale, attempt, "Card", 99m, "PHP", key)));
        Assert.Equal(DomainErrorCodes.PaymentGatewayIdempotencyConflict, ex.ErrorCode);
    }

    [Fact]
    public async Task Behavior_timeout_after_create_stores_session_then_throws()
    {
        var gateway = new FakePaymentGateway();
        gateway.SetBehavior(FakePaymentGatewayBehavior.TimeoutAfterCreate);
        var attemptId = Guid.NewGuid();
        var request = new PaymentGatewayCreateRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            attemptId,
            "GCash",
            25m,
            "PHP",
            "timeout-after");

        var ex = await Assert.ThrowsAsync<PaymentGatewayException>(() => gateway.CreateSessionAsync(request));
        Assert.True(ex.SessionMayExist);
        Assert.Equal(DomainErrorCodes.PaymentGatewayTimeout, ex.ErrorCode);

        var recovered = await gateway.GetSessionAsync($"fake_{attemptId:N}");
        Assert.NotNull(recovered);
        Assert.False(string.IsNullOrWhiteSpace(recovered!.QrPayload));

        gateway.ResetBehavior();
        gateway.ClearSessions();
    }

    [Fact]
    public async Task Behavior_definite_failure_and_timeout_before_create()
    {
        var gateway = new FakePaymentGateway();
        gateway.SetBehavior(FakePaymentGatewayBehavior.DefiniteFailure);
        var fail = await Assert.ThrowsAsync<PaymentGatewayException>(() =>
            gateway.CreateSessionAsync(
                new PaymentGatewayCreateRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Card", 1m, "PHP", "fail")));
        Assert.False(fail.SessionMayExist);

        gateway.SetBehavior(FakePaymentGatewayBehavior.TimeoutBeforeCreate);
        var timeout = await Assert.ThrowsAsync<PaymentGatewayException>(() =>
            gateway.CreateSessionAsync(
                new PaymentGatewayCreateRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Card", 1m, "PHP", "before")));
        Assert.False(timeout.SessionMayExist);
        Assert.Null(await gateway.GetSessionAsync($"fake_{Guid.NewGuid():N}"));
        gateway.ResetBehavior();
    }

    [Fact]
    public void MarkPaidFromProvider_can_override_failed_with_newer_sequence()
    {
        var attempt = PaymentAttempt.CreateElectronic(
            PosOrganizationId.From(Guid.NewGuid()),
            SaleId.From(Guid.NewGuid()),
            PaymentAttemptMethod.Card,
            10m,
            "PHP",
            "key-1",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);
        attempt.AttachProviderSession("fake_ref", "https://x", null, null, DateTimeOffset.UtcNow);
        attempt.MarkFailedFromProvider(10, "declined", "no", DateTimeOffset.UtcNow);
        Assert.Equal(PaymentAttemptStatus.Failed, attempt.Status);

        attempt.MarkPaidFromProvider(20, DateTimeOffset.UtcNow, "Visa", "4242");
        Assert.Equal(PaymentAttemptStatus.Paid, attempt.Status);
        Assert.Equal(20, attempt.ProviderEventSequence);
    }
}
