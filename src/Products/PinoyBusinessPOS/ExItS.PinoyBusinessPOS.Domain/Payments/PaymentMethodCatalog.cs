using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Domain.Payments;

/// <summary>Central catalog of POS payment channels (sale codes + future online foundations).</summary>
public sealed record PaymentMethodDefinition(
    string MethodCode,
    PaymentCapability RequiredCapability,
    PaymentIntegrationMode IntegrationMode,
    PaymentSettlementMode SettlementMode,
    PaymentMethodAvailability Availability,
    bool IsCheckoutSaleMethod,
    string? MapsToSalePaymentMethod = null);

public static class PaymentMethodCatalog
{
    public const string Cash = nameof(SalePaymentMethod.Cash);
    public const string ManualGCash = nameof(SalePaymentMethod.ManualGCash);
    public const string Utang = nameof(SalePaymentMethod.Utang);
    public const string BankTransfer = nameof(SalePaymentMethod.BankTransfer);
    public const string Check = nameof(SalePaymentMethod.Check);
    public const string ManualMaya = nameof(SalePaymentMethod.ManualMaya);
    public const string OnlineGCash = "OnlineGCash";
    public const string OnlineMaya = "OnlineMaya";
    public const string QrPh = "QrPh";
    public const string Card = nameof(SalePaymentMethod.Card);
    public const string OnlineBanking = "OnlineBanking";

    public static IReadOnlyList<PaymentMethodDefinition> All { get; } =
    [
        new(Cash, PaymentCapability.BasicPayments, PaymentIntegrationMode.None, PaymentSettlementMode.Immediate, PaymentMethodAvailability.BuiltIn, true, Cash),
        new(ManualGCash, PaymentCapability.BasicPayments, PaymentIntegrationMode.Manual, PaymentSettlementMode.Immediate, PaymentMethodAvailability.BuiltIn, true, ManualGCash),
        // Utang completes immediately as recorded debt (SalePaymentMethods.CreatesReceivable) — not deferred settlement.
        new(Utang, PaymentCapability.BasicPayments, PaymentIntegrationMode.None, PaymentSettlementMode.Immediate, PaymentMethodAvailability.BuiltIn, true, Utang),
        new(BankTransfer, PaymentCapability.PaymentManagement, PaymentIntegrationMode.Manual, PaymentSettlementMode.Immediate, PaymentMethodAvailability.Configurable, true, BankTransfer),
        new(Check, PaymentCapability.PaymentManagement, PaymentIntegrationMode.Manual, PaymentSettlementMode.Deferred, PaymentMethodAvailability.Configurable, true, Check),
        new(ManualMaya, PaymentCapability.PaymentManagement, PaymentIntegrationMode.Manual, PaymentSettlementMode.Immediate, PaymentMethodAvailability.Configurable, true, ManualMaya),
        new(OnlineGCash, PaymentCapability.OnlinePayments, PaymentIntegrationMode.Online, PaymentSettlementMode.ExternalConfirmation, PaymentMethodAvailability.ComingSoon, false, nameof(SalePaymentMethod.GCash)),
        new(OnlineMaya, PaymentCapability.OnlinePayments, PaymentIntegrationMode.Online, PaymentSettlementMode.ExternalConfirmation, PaymentMethodAvailability.ComingSoon, false),
        new(QrPh, PaymentCapability.OnlinePayments, PaymentIntegrationMode.Online, PaymentSettlementMode.ExternalConfirmation, PaymentMethodAvailability.ComingSoon, false),
        new(Card, PaymentCapability.OnlinePayments, PaymentIntegrationMode.Online, PaymentSettlementMode.ExternalConfirmation, PaymentMethodAvailability.ComingSoon, false, nameof(SalePaymentMethod.Card)),
        new(OnlineBanking, PaymentCapability.OnlinePayments, PaymentIntegrationMode.Online, PaymentSettlementMode.ExternalConfirmation, PaymentMethodAvailability.ComingSoon, false)
    ];

    public static PaymentMethodDefinition? Find(string? methodCode)
    {
        if (string.IsNullOrWhiteSpace(methodCode))
        {
            return null;
        }

        return All.FirstOrDefault(d =>
            string.Equals(d.MethodCode, methodCode.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    public static bool TryGetSalePaymentMethod(string methodCode, out SalePaymentMethod method)
    {
        method = SalePaymentMethod.Cash;
        var def = Find(methodCode);
        if (def?.MapsToSalePaymentMethod is null || !def.IsCheckoutSaleMethod)
        {
            return false;
        }

        return SalePaymentMethods.TryParse(def.MapsToSalePaymentMethod, out method);
    }
}
