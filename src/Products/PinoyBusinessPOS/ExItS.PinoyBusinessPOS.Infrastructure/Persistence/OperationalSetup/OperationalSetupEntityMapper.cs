using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence.OperationalSetup;

internal static class OperationalSetupEntityMapper
{
    public static PosOperationalSetup ToDomain(OperationalSetupRecord record) =>
        PosOperationalSetup.Rehydrate(
            PosOrganizationId.From(record.OrganizationId),
            record.StoreDisplayName,
            record.CurrencyCode,
            Enum.Parse<TaxPricingMode>(record.TaxPricingMode, ignoreCase: true),
            record.TaxRatePercent,
            record.ReceiptHeader,
            record.ReceiptFooter,
            record.BusinessAddress,
            record.ContactPhone,
            record.DefaultRegisterId is null ? null : RegisterId.From(record.DefaultRegisterId.Value),
            record.IsCompleted,
            record.CompletedAtUtc,
            record.CreatedAtUtc,
            record.CreatedBy,
            record.UpdatedAtUtc,
            record.UpdatedBy,
            Enum.Parse<CashCountMode>(record.CashCountMode, ignoreCase: true));

    public static OperationalSetupRecord ToRecord(PosOperationalSetup setup) =>
        new()
        {
            OrganizationId = setup.OrganizationId.Value,
            StoreDisplayName = setup.StoreDisplayName,
            CurrencyCode = setup.CurrencyCode,
            TaxPricingMode = setup.TaxPricingMode.ToString(),
            TaxRatePercent = setup.TaxRatePercent,
            ReceiptHeader = setup.ReceiptHeader,
            ReceiptFooter = setup.ReceiptFooter,
            BusinessAddress = setup.BusinessAddress,
            ContactPhone = setup.ContactPhone,
            DefaultRegisterId = setup.DefaultRegisterId?.Value,
            CashCountMode = setup.CashCountMode.ToString(),
            IsCompleted = setup.IsCompleted,
            CompletedAtUtc = setup.CompletedAtUtc,
            CreatedAtUtc = setup.CreatedAtUtc,
            CreatedBy = setup.CreatedBy,
            UpdatedAtUtc = setup.UpdatedAtUtc,
            UpdatedBy = setup.UpdatedBy
        };

    public static void ApplyToRecord(PosOperationalSetup setup, OperationalSetupRecord record)
    {
        record.StoreDisplayName = setup.StoreDisplayName;
        record.CurrencyCode = setup.CurrencyCode;
        record.TaxPricingMode = setup.TaxPricingMode.ToString();
        record.TaxRatePercent = setup.TaxRatePercent;
        record.ReceiptHeader = setup.ReceiptHeader;
        record.ReceiptFooter = setup.ReceiptFooter;
        record.BusinessAddress = setup.BusinessAddress;
        record.ContactPhone = setup.ContactPhone;
        record.DefaultRegisterId = setup.DefaultRegisterId?.Value;
        record.CashCountMode = setup.CashCountMode.ToString();
        record.IsCompleted = setup.IsCompleted;
        record.CompletedAtUtc = setup.CompletedAtUtc;
        record.UpdatedAtUtc = setup.UpdatedAtUtc;
        record.UpdatedBy = setup.UpdatedBy;
    }
}
