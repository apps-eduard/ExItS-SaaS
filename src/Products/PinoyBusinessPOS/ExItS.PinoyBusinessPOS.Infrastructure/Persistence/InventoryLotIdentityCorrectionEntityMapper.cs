using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class InventoryLotIdentityCorrectionEntityMapper
{
    public static InventoryLotIdentityCorrection ToDomain(InventoryLotIdentityCorrectionRecord record) =>
        InventoryLotIdentityCorrection.Rehydrate(
            InventoryLotIdentityCorrectionId.From(record.Id),
            PosOrganizationId.From(record.OrganizationId),
            PosBranchId.From(record.BranchId),
            CatalogProductId.From(record.ProductId),
            InventoryLotId.From(record.InventoryLotId),
            record.OldExpirationDate,
            record.NewExpirationDate,
            record.OldLotNumber,
            record.NewLotNumber,
            record.Reason,
            record.CorrectedBy,
            record.CorrectedAtUtc);

    public static InventoryLotIdentityCorrectionRecord ToRecord(InventoryLotIdentityCorrection correction) =>
        new()
        {
            Id = correction.Id.Value,
            OrganizationId = correction.OrganizationId.Value,
            BranchId = correction.BranchId.Value,
            ProductId = correction.ProductId.Value,
            InventoryLotId = correction.InventoryLotId.Value,
            OldExpirationDate = correction.OldExpirationDate,
            NewExpirationDate = correction.NewExpirationDate,
            OldLotNumber = correction.OldLotNumber,
            NewLotNumber = correction.NewLotNumber,
            Reason = correction.Reason,
            CorrectedBy = correction.CorrectedBy,
            CorrectedAtUtc = correction.CorrectedAtUtc
        };
}
