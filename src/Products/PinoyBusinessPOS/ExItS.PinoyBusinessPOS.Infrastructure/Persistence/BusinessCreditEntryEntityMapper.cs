using ExItS.PinoyBusinessPOS.Domain.Credit;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Credit;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class BusinessCreditEntryEntityMapper
{
    public static BusinessCreditEntry ToDomain(BusinessCreditEntryRecord record) =>
        BusinessCreditEntry.Rehydrate(
            BusinessCreditEntryId.From(record.Id),
            PosOrganizationId.From(record.SellerOrganizationId),
            PosOrganizationId.From(record.BuyerOrganizationId),
            record.ConnectionId,
            record.Amount,
            record.Remarks,
            Enum.Parse<CreditEntryStatus>(record.Status, ignoreCase: false),
            record.CreatedAtUtc,
            record.ReversedAtUtc,
            record.ReversalReason,
            record.CurrentDueDate,
            record.SourceSaleId is null ? null : SaleId.From(record.SourceSaleId.Value));

    public static BusinessCreditEntryRecord ToRecord(BusinessCreditEntry entry) =>
        new()
        {
            Id = entry.Id.Value,
            SellerOrganizationId = entry.SellerOrganizationId.Value,
            BuyerOrganizationId = entry.BuyerOrganizationId.Value,
            ConnectionId = entry.ConnectionId,
            Amount = entry.Amount,
            Remarks = entry.Remarks,
            Status = entry.Status.ToString(),
            CreatedAtUtc = entry.CreatedAtUtc,
            ReversedAtUtc = entry.ReversedAtUtc,
            ReversalReason = entry.ReversalReason,
            CurrentDueDate = entry.CurrentDueDate,
            SourceSaleId = entry.SourceSaleId?.Value
        };

    public static void ApplyToRecord(BusinessCreditEntry entry, BusinessCreditEntryRecord record)
    {
        record.Amount = entry.Amount;
        record.Status = entry.Status.ToString();
        record.ReversedAtUtc = entry.ReversedAtUtc;
        record.ReversalReason = entry.ReversalReason;
        record.CurrentDueDate = entry.CurrentDueDate;
    }
}
