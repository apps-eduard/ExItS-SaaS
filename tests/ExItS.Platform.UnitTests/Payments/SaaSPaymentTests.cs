using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Payments;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;

namespace ExItS.Platform.UnitTests.Payments;

public sealed class SaaSPaymentTests
{
    private static readonly DateTimeOffset T0 = new(2026, 7, 29, 12, 0, 0, TimeSpan.Zero);

    private static SaaSPayment CreateValid(string reference = "REF-001", decimal amount = 999m) =>
        SaaSPayment.CreateManual(
            PlatformOrganizationId.New(),
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            amount,
            CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.GCash,
            reference,
            T0,
            T0);

    [Fact]
    public void CreateManual_creates_pending_confirmation_payment_with_no_linked_subscription()
    {
        var payment = CreateValid();

        Assert.Equal(SaaSPaymentStatus.PendingConfirmation, payment.Status);
        Assert.Null(payment.SubscriptionId);
        Assert.Equal(1, payment.Version);
        Assert.Equal(T0, payment.CreatedAtUtc);
        Assert.Equal(T0, payment.UpdatedAtUtc);
        Assert.Equal("REF-001", payment.ExternalReference);
    }

    [Fact]
    public void CreateConfirmedLinkedFromProvider_is_confirmed_and_linked()
    {
        var orgId = PlatformOrganizationId.New();
        var subId = SubscriptionId.New();
        var payment = SaaSPayment.CreateConfirmedLinkedFromProvider(
            orgId,
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            subId,
            299m,
            CurrencyCode.Create(CurrencyCode.PHP),
            "lvp_pay_000001",
            "provider:LocalValidation",
            T0,
            T0);

        Assert.Equal(SaaSPaymentStatus.Confirmed, payment.Status);
        Assert.Equal(SaaSPaymentMethod.Online, payment.Method);
        Assert.Equal(subId, payment.SubscriptionId);
        Assert.Equal("provider:LocalValidation", payment.ConfirmedBy);
    }

    [Fact]
    public void CreateManual_rejects_Online_method()
    {
        var ex = Assert.Throws<DomainException>(() =>
            SaaSPayment.CreateManual(
                PlatformOrganizationId.New(),
                ProductCode.Create(ProductCode.PinoyBusinessPos),
                100m,
                CurrencyCode.Create(CurrencyCode.PHP),
                SaaSPaymentMethod.Online,
                "REF-ONLINE",
                T0,
                T0));
        Assert.Equal(DomainErrorCodes.InvalidSaaSPaymentTransition, ex.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(-0.01)]
    public void CreateManual_rejects_non_positive_amount(decimal amount)
    {
        var ex = Assert.Throws<DomainException>(() => CreateValid(amount: amount));
        Assert.Equal(DomainErrorCodes.PaymentAmountInvalid, ex.ErrorCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("123")]
    public void CurrencyCode_rejects_invalid_values(string value)
    {
        var ex = Assert.Throws<DomainException>(() => CurrencyCode.Create(value));
        Assert.Equal(DomainErrorCodes.PaymentCurrencyInvalid, ex.ErrorCode);
    }

    [Fact]
    public void CurrencyCode_normalizes_lowercase_input_to_uppercase()
    {
        var code = CurrencyCode.Create("php");
        Assert.Equal("PHP", code.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateManual_rejects_blank_reference(string reference)
    {
        var ex = Assert.Throws<DomainException>(() => CreateValid(reference: reference));
        Assert.Equal(DomainErrorCodes.PaymentReferenceRequired, ex.ErrorCode);
    }

    [Fact]
    public void NormalizeReference_trims_uppercases_and_collapses_internal_whitespace()
    {
        Assert.Equal("REF 001", SaaSPayment.NormalizeReference("  ref   001  "));
        Assert.Equal("ABC-123", SaaSPayment.NormalizeReference("abc-123"));
    }

    [Fact]
    public void Confirm_from_PendingConfirmation_transitions_to_Confirmed()
    {
        var payment = CreateValid();
        payment.Confirm("staff-1", T0.AddMinutes(1));

        Assert.Equal(SaaSPaymentStatus.Confirmed, payment.Status);
        Assert.Equal("staff-1", payment.ConfirmedBy);
        Assert.Equal(T0.AddMinutes(1), payment.ConfirmedAtUtc);
        Assert.Equal(T0.AddMinutes(1), payment.UpdatedAtUtc);
        Assert.Equal(2, payment.Version);
    }

    [Fact]
    public void Confirm_when_already_Confirmed_throws_PaymentAlreadyConfirmed_without_mutating_state()
    {
        var payment = CreateValid();
        payment.Confirm("staff-1", T0.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() => payment.Confirm("staff-2", T0.AddMinutes(2)));

        Assert.Equal(DomainErrorCodes.PaymentAlreadyConfirmed, ex.ErrorCode);
        Assert.Equal("staff-1", payment.ConfirmedBy);
        Assert.Equal(T0.AddMinutes(1), payment.ConfirmedAtUtc);
        Assert.Equal(2, payment.Version);
    }

    [Fact]
    public void Reject_from_PendingConfirmation_transitions_to_Rejected()
    {
        var payment = CreateValid();
        payment.Reject("staff-1", "Invalid reference", T0.AddMinutes(1));

        Assert.Equal(SaaSPaymentStatus.Rejected, payment.Status);
        Assert.Equal("staff-1", payment.RejectedBy);
        Assert.Equal("Invalid reference", payment.RejectionReason);
        Assert.Equal(T0.AddMinutes(1), payment.RejectedAtUtc);
        Assert.Equal(2, payment.Version);
    }

    [Fact]
    public void Reject_requires_a_non_blank_reason_and_does_not_mutate_state_on_failure()
    {
        var payment = CreateValid();
        var ex = Assert.Throws<DomainException>(() => payment.Reject("staff-1", "   ", T0.AddMinutes(1)));

        Assert.Equal(DomainErrorCodes.PaymentReasonRequired, ex.ErrorCode);
        Assert.Equal(SaaSPaymentStatus.PendingConfirmation, payment.Status);
        Assert.Null(payment.RejectedBy);
        Assert.Equal(1, payment.Version);
    }

    [Fact]
    public void Void_from_Confirmed_transitions_to_Voided()
    {
        var payment = CreateValid();
        payment.Confirm("staff-1", T0.AddMinutes(1));
        payment.Void("staff-2", "Refunded", T0.AddMinutes(2));

        Assert.Equal(SaaSPaymentStatus.Voided, payment.Status);
        Assert.Equal("staff-2", payment.VoidedBy);
        Assert.Equal("Refunded", payment.VoidReason);
        Assert.Equal(T0.AddMinutes(2), payment.VoidedAtUtc);
        Assert.Equal(3, payment.Version);
    }

    [Fact]
    public void Void_requires_a_non_blank_reason()
    {
        var payment = CreateValid();
        payment.Confirm("staff-1", T0.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() => payment.Void("staff-2", "", T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.PaymentReasonRequired, ex.ErrorCode);
        Assert.Equal(SaaSPaymentStatus.Confirmed, payment.Status);
    }

    [Fact]
    public void Void_from_PendingConfirmation_throws_invalid_transition()
    {
        var payment = CreateValid();
        var ex = Assert.Throws<DomainException>(() => payment.Void("staff-1", "reason", T0.AddMinutes(1)));

        Assert.Equal(DomainErrorCodes.InvalidSaaSPaymentTransition, ex.ErrorCode);
        Assert.Equal(SaaSPaymentStatus.PendingConfirmation, payment.Status);
    }

    [Fact]
    public void Confirm_from_Rejected_throws_invalid_transition()
    {
        var payment = CreateValid();
        payment.Reject("staff-1", "bad ref", T0.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() => payment.Confirm("staff-2", T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidSaaSPaymentTransition, ex.ErrorCode);
    }

    [Fact]
    public void Reject_from_Confirmed_throws_invalid_transition()
    {
        var payment = CreateValid();
        payment.Confirm("staff-1", T0.AddMinutes(1));

        var ex = Assert.Throws<DomainException>(() => payment.Reject("staff-2", "reason", T0.AddMinutes(2)));
        Assert.Equal(DomainErrorCodes.InvalidSaaSPaymentTransition, ex.ErrorCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Terminal_states_reject_every_further_transition(bool rejectedFirst)
    {
        var payment = CreateValid();
        if (rejectedFirst)
        {
            payment.Reject("staff-1", "bad ref", T0.AddMinutes(1));
        }
        else
        {
            payment.Confirm("staff-1", T0.AddMinutes(1));
            payment.Void("staff-2", "refund", T0.AddMinutes(2));
        }

        var versionBefore = payment.Version;

        Assert.Throws<DomainException>(() => payment.Confirm("staff-3", T0.AddMinutes(3)));
        Assert.Throws<DomainException>(() => payment.Reject("staff-3", "reason", T0.AddMinutes(3)));
        Assert.Throws<DomainException>(() => payment.Void("staff-3", "reason", T0.AddMinutes(3)));
        Assert.Throws<DomainException>(() => payment.LinkSubscription(SubscriptionId.New(), T0.AddMinutes(3)));

        Assert.Equal(versionBefore, payment.Version);
    }

    [Fact]
    public void LinkSubscription_requires_Confirmed_status()
    {
        var payment = CreateValid();
        var ex = Assert.Throws<DomainException>(() => payment.LinkSubscription(SubscriptionId.New(), T0.AddMinutes(1)));

        Assert.Equal(DomainErrorCodes.PaymentAlreadyUsed, ex.ErrorCode);
        Assert.Null(payment.SubscriptionId);
    }

    [Fact]
    public void LinkSubscription_succeeds_once_when_Confirmed()
    {
        var payment = CreateValid();
        payment.Confirm("staff-1", T0.AddMinutes(1));
        var subscriptionId = SubscriptionId.New();

        payment.LinkSubscription(subscriptionId, T0.AddMinutes(2));

        Assert.Equal(subscriptionId, payment.SubscriptionId);
        Assert.Equal(3, payment.Version);
    }

    [Fact]
    public void LinkSubscription_twice_throws_PaymentAlreadyUsed_and_keeps_original_link()
    {
        var payment = CreateValid();
        payment.Confirm("staff-1", T0.AddMinutes(1));
        var firstSubscriptionId = SubscriptionId.New();
        payment.LinkSubscription(firstSubscriptionId, T0.AddMinutes(2));

        var ex = Assert.Throws<DomainException>(
            () => payment.LinkSubscription(SubscriptionId.New(), T0.AddMinutes(3)));

        Assert.Equal(DomainErrorCodes.PaymentAlreadyUsed, ex.ErrorCode);
        Assert.Equal(firstSubscriptionId, payment.SubscriptionId);
    }

    [Fact]
    public void Rehydrate_restores_full_persisted_state()
    {
        var id = SaaSPaymentId.New();
        var orgId = PlatformOrganizationId.New();
        var subscriptionId = SubscriptionId.New();

        var payment = SaaSPayment.Rehydrate(
            id,
            orgId,
            ProductCode.Create(ProductCode.PinoyBusinessPos),
            subscriptionId,
            500m,
            CurrencyCode.Create(CurrencyCode.PHP),
            SaaSPaymentMethod.Cash,
            "Ref 001",
            "REF 001",
            SaaSPaymentStatus.Confirmed,
            T0,
            T0.AddMinutes(1),
            "staff-1",
            null,
            null,
            null,
            null,
            null,
            null,
            T0,
            T0.AddMinutes(1),
            2);

        Assert.Equal(id, payment.Id);
        Assert.Equal(orgId, payment.OrganizationId);
        Assert.Equal(subscriptionId, payment.SubscriptionId);
        Assert.Equal(SaaSPaymentStatus.Confirmed, payment.Status);
        Assert.Equal("staff-1", payment.ConfirmedBy);
        Assert.Equal(T0.AddMinutes(1), payment.ConfirmedAtUtc);
        Assert.Equal(2, payment.Version);
    }
}
