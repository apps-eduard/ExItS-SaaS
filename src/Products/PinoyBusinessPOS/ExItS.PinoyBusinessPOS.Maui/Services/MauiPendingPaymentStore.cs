using Microsoft.Maui.Storage;

namespace ExItS.PinoyBusinessPOS.Maui.Services;

/// <summary>
/// Persists an in-flight electronic payment attempt so checkout can resume after app restart or back navigation.
/// Stores identifiers only — never card data or secrets.
/// </summary>
public sealed class MauiPendingPaymentStore
{
    public const string SaleIdKey = "exits-pos-pending-payment-sale-id";
    public const string AttemptIdKey = "exits-pos-pending-payment-attempt-id";
    public const string IdempotencyKeyKey = "exits-pos-pending-payment-idempotency-key";
    public const string OrganizationIdKey = "exits-pos-pending-payment-org-id";
    public const string PaymentMethodKey = "exits-pos-pending-payment-method";

    public PendingPaymentState? Get()
    {
        var saleRaw = Preferences.Default.Get(SaleIdKey, string.Empty);
        if (!Guid.TryParse(saleRaw, out var saleId))
        {
            return null;
        }

        Guid? attemptId = Guid.TryParse(Preferences.Default.Get(AttemptIdKey, string.Empty), out var parsedAttempt)
            && parsedAttempt != Guid.Empty
            ? parsedAttempt
            : null;

        Guid? organizationId = Guid.TryParse(Preferences.Default.Get(OrganizationIdKey, string.Empty), out var orgId)
            ? orgId
            : null;

        return new PendingPaymentState(
            saleId,
            attemptId,
            Preferences.Default.Get(IdempotencyKeyKey, string.Empty),
            organizationId,
            Preferences.Default.Get(PaymentMethodKey, string.Empty));
    }

    public void Save(PendingPaymentState state)
    {
        Preferences.Default.Set(SaleIdKey, state.SaleId.ToString("D"));
        if (state.AttemptId is Guid attemptId && attemptId != Guid.Empty)
        {
            Preferences.Default.Set(AttemptIdKey, attemptId.ToString("D"));
        }
        else
        {
            Preferences.Default.Remove(AttemptIdKey);
        }
        Preferences.Default.Set(IdempotencyKeyKey, state.IdempotencyKey);
        Preferences.Default.Set(PaymentMethodKey, state.PaymentMethod);
        if (state.OrganizationId is Guid orgId)
        {
            Preferences.Default.Set(OrganizationIdKey, orgId.ToString("D"));
        }
        else
        {
            Preferences.Default.Remove(OrganizationIdKey);
        }
    }

    public void Clear()
    {
        Preferences.Default.Remove(SaleIdKey);
        Preferences.Default.Remove(AttemptIdKey);
        Preferences.Default.Remove(IdempotencyKeyKey);
        Preferences.Default.Remove(OrganizationIdKey);
        Preferences.Default.Remove(PaymentMethodKey);
    }
}

public sealed record PendingPaymentState(
    Guid SaleId,
    Guid? AttemptId,
    string IdempotencyKey,
    Guid? OrganizationId,
    string PaymentMethod);
