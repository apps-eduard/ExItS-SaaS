using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Quotations;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Quotations;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class QuotationEntityMapper
{
    public static Quotation ToDomain(QuotationRecord record, IEnumerable<QuotationLineRecord> lineRecords)
    {
        var quotationId = QuotationId.From(record.Id);
        var organizationId = PosOrganizationId.From(record.OrganizationId);
        var lines = lineRecords
            .OrderBy(l => l.LineNumber)
            .Select(l => QuotationLine.Rehydrate(
                QuotationLineId.From(l.Id),
                quotationId,
                organizationId,
                CatalogProductId.From(l.ProductId),
                l.LineNumber,
                l.NameSnapshot,
                l.SkuSnapshot,
                string.IsNullOrWhiteSpace(l.UomSnapshot) ? null : UnitOfMeasures.Parse(l.UomSnapshot),
                l.Quantity,
                l.UnitPrice,
                l.DiscountAmount,
                l.LineTotal))
            .ToList();

        return Quotation.Rehydrate(
            quotationId,
            organizationId,
            record.QuotationNumber,
            Enum.Parse<QuotationStatus>(record.Status, ignoreCase: true),
            POSCustomerId.From(record.CustomerId),
            record.CustomerDisplayNameSnapshot,
            record.CustomerMobileNumberSnapshot,
            record.CustomerAddressSnapshot,
            record.CustomerNotesSnapshot,
            PosBranchId.From(record.BranchId),
            record.PreparedBy,
            record.ValidUntil,
            record.Reference,
            record.Notes,
            record.Terms,
            record.ConvertedSaleId,
            record.IsCustomerVisible,
            record.IssuedAtUtc,
            record.CreatedAtUtc,
            record.UpdatedAtUtc,
            lines);
    }

    public static QuotationRecord ToRecord(Quotation quotation) =>
        new()
        {
            Id = quotation.Id.Value,
            OrganizationId = quotation.OrganizationId.Value,
            QuotationNumber = quotation.QuotationNumber,
            Status = quotation.Status.ToString(),
            CustomerId = quotation.CustomerId.Value,
            CustomerDisplayNameSnapshot = quotation.CustomerDisplayNameSnapshot,
            CustomerMobileNumberSnapshot = quotation.CustomerMobileNumberSnapshot,
            CustomerAddressSnapshot = quotation.CustomerAddressSnapshot,
            CustomerNotesSnapshot = quotation.CustomerNotesSnapshot,
            BranchId = quotation.BranchId.Value,
            PreparedBy = quotation.PreparedBy,
            ValidUntil = quotation.ValidUntil,
            Reference = quotation.Reference,
            Notes = quotation.Notes,
            Terms = quotation.Terms,
            ConvertedSaleId = quotation.ConvertedSaleId,
            IsCustomerVisible = quotation.IsCustomerVisible,
            IssuedAtUtc = quotation.IssuedAtUtc,
            CreatedAtUtc = quotation.CreatedAtUtc,
            UpdatedAtUtc = quotation.UpdatedAtUtc
        };

    public static QuotationLineRecord ToRecord(QuotationLine line) =>
        new()
        {
            Id = line.Id.Value,
            QuotationId = line.QuotationId.Value,
            OrganizationId = line.OrganizationId.Value,
            ProductId = line.ProductId.Value,
            LineNumber = line.LineNumber,
            NameSnapshot = line.NameSnapshot,
            SkuSnapshot = line.SkuSnapshot,
            UomSnapshot = line.UomSnapshot is null ? null : UnitOfMeasures.ToCode(line.UomSnapshot.Value),
            Quantity = line.Quantity,
            UnitPrice = line.UnitPrice,
            DiscountAmount = line.DiscountAmount,
            LineTotal = line.LineTotal
        };

    public static void ApplyToRecord(Quotation quotation, QuotationRecord record)
    {
        record.QuotationNumber = quotation.QuotationNumber;
        record.Status = quotation.Status.ToString();
        record.CustomerId = quotation.CustomerId.Value;
        record.CustomerDisplayNameSnapshot = quotation.CustomerDisplayNameSnapshot;
        record.CustomerMobileNumberSnapshot = quotation.CustomerMobileNumberSnapshot;
        record.CustomerAddressSnapshot = quotation.CustomerAddressSnapshot;
        record.CustomerNotesSnapshot = quotation.CustomerNotesSnapshot;
        record.BranchId = quotation.BranchId.Value;
        record.PreparedBy = quotation.PreparedBy;
        record.ValidUntil = quotation.ValidUntil;
        record.Reference = quotation.Reference;
        record.Notes = quotation.Notes;
        record.Terms = quotation.Terms;
        record.ConvertedSaleId = quotation.ConvertedSaleId;
        record.IsCustomerVisible = quotation.IsCustomerVisible;
        record.IssuedAtUtc = quotation.IssuedAtUtc;
        record.UpdatedAtUtc = quotation.UpdatedAtUtc;
    }
}
