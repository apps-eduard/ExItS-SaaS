using ExItS.Platform.Application.Payments;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Identity;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.UnitTests.Payments;

public sealed class SubscriptionPaymentTransactionTests
{
    private static readonly PlatformUserId UserId = PlatformUserId.From(Guid.Parse("11111111-1111-1111-1111-111111111111"));
    private static readonly PlatformOrganizationId OrgId =
        PlatformOrganizationId.From(Guid.Parse("22222222-2222-2222-2222-222222222222"));

    private static SubscriptionPriceQuote Quote(
        decimal final = 4272m,
        decimal baseAmount = 4497m,
        decimal discountPercent = 5m,
        decimal discountAmount = 225m) =>
        new(
            PlanKey: "pro",
            BillingCycle: BillingCycle.Quarterly,
            PeriodMonths: 3,
            MonthlyListPrice: 1499m,
            BaseAmount: baseAmount,
            DiscountPercent: discountPercent,
            DiscountAmount: discountAmount,
            FinalAmount: final,
            EquivalentMonthlyAmount: Math.Round(final / 3m, 2),
            CurrencyCode: "PHP");

    [Fact]
    public void Paid_activates_once_and_rejects_paid_to_pending()
    {
        var utc = DateTimeOffset.Parse("2026-09-13T21:14:00Z");
        var payment = SubscriptionPaymentTransaction.CreatePending(
            "PAY-20260913-000001",
            UserId,
            "pro",
            BillingCycle.Quarterly,
            Quote(),
            utc,
            OrgId);

        Assert.Equal(SubscriptionPaymentStatus.Pending, payment.Status);
        Assert.False(payment.SubscriptionActivated);

        payment.BeginProcessing(SubscriptionPaymentChannel.GCash, "SIM-GC-260913-X7K29P", utc);
        Assert.Equal(SubscriptionPaymentStatus.Processing, payment.Status);

        var periodStart = utc;
        var periodEnd = utc.AddMonths(3);
        payment.MarkPaid(utc.AddMinutes(1), periodStart, periodEnd);
        Assert.Equal(SubscriptionPaymentStatus.Paid, payment.Status);

        var subId = SubscriptionId.From(Guid.Parse("33333333-3333-3333-3333-333333333333"));
        payment.MarkSubscriptionActivated(subId, utc.AddMinutes(1));
        Assert.True(payment.SubscriptionActivated);

        // Idempotent re-activation
        payment.MarkSubscriptionActivated(subId, utc.AddMinutes(2));
        Assert.True(payment.SubscriptionActivated);
        Assert.Equal(1, payment.Activities.Count(a => a.EventType == "SubscriptionActivated"));

        var ex = Assert.Throws<DomainException>(() =>
            payment.BeginProcessing(SubscriptionPaymentChannel.Maya, "SIM-MY-260913-AAAAAA", utc.AddMinutes(3)));
        Assert.Equal(DomainErrorCodes.InvalidPaymentStatusTransition, ex.ErrorCode);
    }

    [Fact]
    public void Failed_cancelled_expired_do_not_activate()
    {
        var utc = DateTimeOffset.Parse("2026-09-13T21:14:00Z");
        var monthly = Quote(final: 1499m, baseAmount: 1499m, discountPercent: 0m, discountAmount: 0m);

        var failed = SubscriptionPaymentTransaction.CreatePending(
            "PAY-20260913-000002", UserId, "pro", BillingCycle.Monthly, monthly, utc, OrgId);
        failed.BeginProcessing(SubscriptionPaymentChannel.Card, "SIM-CC-260913-H8N31K", utc, "Visa", "0002");
        failed.MarkFailed("card_declined", "Simulated card decline", utc);
        Assert.Equal(SubscriptionPaymentStatus.Failed, failed.Status);
        Assert.False(failed.SubscriptionActivated);

        var cancelled = SubscriptionPaymentTransaction.CreatePending(
            "PAY-20260913-000003", UserId, "pro", BillingCycle.Monthly, monthly, utc, OrgId);
        cancelled.Cancel(utc);
        Assert.Equal(SubscriptionPaymentStatus.Cancelled, cancelled.Status);
        Assert.False(cancelled.SubscriptionActivated);

        var expired = SubscriptionPaymentTransaction.CreatePending(
            "PAY-20260913-000004", UserId, "pro", BillingCycle.Monthly, monthly, utc, OrgId);
        expired.Expire(utc);
        Assert.Equal(SubscriptionPaymentStatus.Expired, expired.Status);
        Assert.False(expired.SubscriptionActivated);
    }

    [Fact]
    public void Card_simulator_rules_and_safe_metadata()
    {
        Assert.True(SubscriptionCardSimulator.Evaluate(SubscriptionCardSimulator.SuccessPan).Success);
        Assert.False(SubscriptionCardSimulator.Evaluate(SubscriptionCardSimulator.DeclinePan).Success);
        Assert.True(SubscriptionCardSimulator.Evaluate(SubscriptionCardSimulator.PendingPan).LeaveProcessing);
        Assert.Equal("Visa", SubscriptionCardSimulator.DetectBrand("4242 4242 4242 4242"));
        Assert.Equal("4242", SubscriptionCardSimulator.Last4("4242 4242 4242 4242"));
    }

    [Fact]
    public void Reference_formats_match_task()
    {
        var utc = DateTimeOffset.Parse("2026-09-13T21:14:00Z");
        Assert.Equal("PAY-20260913-000123", SubscriptionPaymentReferences.FormatInternalReference(utc, 123));
        var gc = SubscriptionPaymentReferences.FormatProviderReference(SubscriptionPaymentChannel.GCash, utc);
        var my = SubscriptionPaymentReferences.FormatProviderReference(SubscriptionPaymentChannel.Maya, utc);
        var cc = SubscriptionPaymentReferences.FormatProviderReference(SubscriptionPaymentChannel.Card, utc);
        Assert.StartsWith("SIM-GC-260913-", gc);
        Assert.StartsWith("SIM-MY-260913-", my);
        Assert.StartsWith("SIM-CC-260913-", cc);
    }
}
