using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>
/// Offline sync snapshot validation: accept trusted line economics without live-catalog re-pricing.
/// </summary>
public static class CheckoutSaleLineSnapshots
{
    public static bool HasAnySnapshotField(CheckoutSaleLineRequest line) =>
        line.UnitPriceSnapshot is not null
        || line.LineTotal is not null
        || !string.IsNullOrWhiteSpace(line.UnitOfMeasure)
        || !string.IsNullOrWhiteSpace(line.SellingMode)
        || !string.IsNullOrWhiteSpace(line.NameSnapshot)
        || !string.IsNullOrWhiteSpace(line.SkuSnapshot)
        || !string.IsNullOrWhiteSpace(line.BarcodeSnapshot);

    public static bool RequestUsesTrustedSnapshots(IReadOnlyList<CheckoutSaleLineRequest>? lines) =>
        lines is not null && lines.Any(l => l is not null && HasAnySnapshotField(l));

    /// <summary>
    /// Builds a <see cref="SaleLineDraft"/> from immutable offline snapshots.
    /// Product must already be known/Active; live SellingPrice is never used as the unit price.
    /// </summary>
    public static ApplicationResult<SaleLineDraft> TryCreateDraftFromSnapshot(
        CheckoutSaleLineRequest line,
        CatalogProduct product)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(product);

        if (line.UnitPriceSnapshot is null
            || line.LineTotal is null
            || string.IsNullOrWhiteSpace(line.UnitOfMeasure)
            || string.IsNullOrWhiteSpace(line.SellingMode))
        {
            return ApplicationResult<SaleLineDraft>.Failure(
                ApplicationErrorCodes.SaleSnapshotIncomplete,
                "Offline sale lines must include UnitPriceSnapshot, UnitOfMeasure, SellingMode, and LineTotal.");
        }

        if (!UnitOfMeasures.TryParse(line.UnitOfMeasure, out var uom))
        {
            return ApplicationResult<SaleLineDraft>.Failure(
                ApplicationErrorCodes.SaleSnapshotInvalid,
                "Sale line UnitOfMeasure snapshot is not recognized.");
        }

        SellingMode sellingMode;
        try
        {
            sellingMode = SellingModes.Parse(line.SellingMode);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaleLineDraft>.Failure(
                ApplicationErrorCodes.SaleSnapshotInvalid,
                ex.Message);
        }

        try
        {
            if (sellingMode == SellingMode.ByWeight)
            {
                SellingModes.EnsureCompatible(sellingMode, uom);
            }

            var quantity = SaleLine.NormalizeQuantity(line.Quantity, uom, sellingMode);
            var unitPrice = SaleLine.NormalizeUnitPrice(line.UnitPriceSnapshot.Value);
            var expectedLineTotal = SaleMoney.RoundMoney(unitPrice * quantity);
            if (line.LineTotal.Value != expectedLineTotal)
            {
                return ApplicationResult<SaleLineDraft>.Failure(
                    ApplicationErrorCodes.SaleSnapshotLineTotalMismatch,
                    $"Line total must equal RoundMoney(UnitPriceSnapshot × Quantity) ({expectedLineTotal}).");
            }

            var name = string.IsNullOrWhiteSpace(line.NameSnapshot)
                ? product.Name
                : line.NameSnapshot.Trim();
            var sku = string.IsNullOrWhiteSpace(line.SkuSnapshot) ? product.Sku : line.SkuSnapshot.Trim();
            var barcode = string.IsNullOrWhiteSpace(line.BarcodeSnapshot)
                ? product.Barcode
                : line.BarcodeSnapshot.Trim();

            return ApplicationResult<SaleLineDraft>.Success(
                new SaleLineDraft(
                    product.Id,
                    name,
                    sku,
                    barcode,
                    uom,
                    unitPrice,
                    quantity,
                    sellingMode));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaleLineDraft>.Failure(ex.ErrorCode, ex.Message);
        }
    }
}
