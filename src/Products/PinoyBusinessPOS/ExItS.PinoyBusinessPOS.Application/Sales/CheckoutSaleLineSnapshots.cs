using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>
/// Offline sync snapshot validation: accept trusted line economics without live-catalog re-pricing.
/// When <see cref="CheckoutSaleLineRequest.SellingUnitId"/> is present, conversion is recomputed server-side.
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
        CatalogProduct product,
        CatalogProductUnit? sellingUnit = null)
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

            var name = string.IsNullOrWhiteSpace(line.NameSnapshot)
                ? product.Name
                : line.NameSnapshot.Trim();
            var sku = string.IsNullOrWhiteSpace(line.SkuSnapshot) ? product.Sku : line.SkuSnapshot.Trim();
            var barcode = string.IsNullOrWhiteSpace(line.BarcodeSnapshot)
                ? product.Barcode
                : line.BarcodeSnapshot.Trim();

            if (line.SellingUnitId is not null || sellingUnit is not null)
            {
                var unit = sellingUnit;
                if (unit is null)
                {
                    return ApplicationResult<SaleLineDraft>.Failure(
                        DomainErrorCodes.InvalidProductUnitId,
                        "Selling unit was not found for this sale line.");
                }

                var unitValidation = ValidateSellUnit(unit, product);
                if (!unitValidation.IsSuccess)
                {
                    return ApplicationResult<SaleLineDraft>.Failure(
                        unitValidation.ErrorCode!,
                        unitValidation.ErrorMessage!);
                }

                var entered = line.EnteredQuantity ?? line.Quantity;
                var multiplier = unit.MultiplierToBase;
                var baseQty = ProductUnitConversion.ToBaseQuantity(entered, multiplier);
                var unitPrice = SaleLine.NormalizeUnitPrice(line.UnitPriceSnapshot.Value);
                var expectedLineTotal = SaleMoney.RoundMoney(unitPrice * entered);
                if (line.LineTotal.Value != expectedLineTotal)
                {
                    return ApplicationResult<SaleLineDraft>.Failure(
                        ApplicationErrorCodes.SaleSnapshotLineTotalMismatch,
                        $"Line total must equal RoundMoney(UnitPriceSnapshot × EnteredQuantity) ({expectedLineTotal}).");
                }

                // Ignore client Quantity when conversion applies — recompute base.
                _ = line.Quantity;

                return ApplicationResult<SaleLineDraft>.Success(
                    new SaleLineDraft(
                        product.Id,
                        name,
                        sku,
                        barcode,
                        uom,
                        unitPrice,
                        baseQty,
                        sellingMode,
                        unit.Id,
                        unit.DisplayName,
                        entered,
                        multiplier));
            }

            var quantity = SaleLine.NormalizeQuantity(line.Quantity, uom, sellingMode);
            var simpleUnitPrice = SaleLine.NormalizeUnitPrice(line.UnitPriceSnapshot.Value);
            var simpleExpected = SaleMoney.RoundMoney(simpleUnitPrice * quantity);
            if (line.LineTotal.Value != simpleExpected)
            {
                return ApplicationResult<SaleLineDraft>.Failure(
                    ApplicationErrorCodes.SaleSnapshotLineTotalMismatch,
                    $"Line total must equal RoundMoney(UnitPriceSnapshot × Quantity) ({simpleExpected}).");
            }

            return ApplicationResult<SaleLineDraft>.Success(
                new SaleLineDraft(
                    product.Id,
                    name,
                    sku,
                    barcode,
                    uom,
                    simpleUnitPrice,
                    quantity,
                    sellingMode));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaleLineDraft>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    public static ApplicationResult<SaleLineDraft> TryCreateOnlineDraft(
        CheckoutSaleLineRequest line,
        CatalogProduct product,
        CatalogProductUnit? sellingUnit,
        decimal? effectiveBasePrice = null,
        decimal? effectiveUnitPrice = null)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(product);

        try
        {
            if (sellingUnit is not null || line.SellingUnitId is not null)
            {
                if (sellingUnit is null)
                {
                    return ApplicationResult<SaleLineDraft>.Failure(
                        DomainErrorCodes.InvalidProductUnitId,
                        "Selling unit was not found for this sale line.");
                }

                var unitValidation = ValidateSellUnit(sellingUnit, product);
                if (!unitValidation.IsSuccess)
                {
                    return ApplicationResult<SaleLineDraft>.Failure(
                        unitValidation.ErrorCode!,
                        unitValidation.ErrorMessage!);
                }

                var entered = line.EnteredQuantity ?? line.Quantity;
                var multiplier = sellingUnit.MultiplierToBase;
                var baseQty = ProductUnitConversion.ToBaseQuantity(entered, multiplier);
                var unitPrice = effectiveUnitPrice ?? sellingUnit.SellingPrice ?? effectiveBasePrice ?? product.SellingPrice;

                return ApplicationResult<SaleLineDraft>.Success(
                    new SaleLineDraft(
                        product.Id,
                        product.Name,
                        product.Sku,
                        product.Barcode,
                        product.UnitOfMeasure,
                        unitPrice,
                        baseQty,
                        product.SellingMode,
                        sellingUnit.Id,
                        sellingUnit.DisplayName,
                        entered,
                        multiplier));
            }

            return ApplicationResult<SaleLineDraft>.Success(
                new SaleLineDraft(
                    product.Id,
                    product.Name,
                    product.Sku,
                    product.Barcode,
                    product.UnitOfMeasure,
                    effectiveBasePrice ?? product.SellingPrice,
                    line.Quantity,
                    product.SellingMode));
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaleLineDraft>.Failure(ex.ErrorCode, ex.Message);
        }
    }

    public static ApplicationResult ValidateSellUnit(CatalogProductUnit unit, CatalogProduct product)
    {
        if (unit.OrganizationId != product.OrganizationId)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.InvalidProductUnitId,
                "Selling unit does not belong to this organization.");
        }

        if (unit.ProductId != product.Id)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.InvalidProductUnitId,
                "Selling unit does not belong to this product.");
        }

        if (!unit.IsActive)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.ProductUnitNotActive,
                "Selling unit is not active.");
        }

        if (unit.Kind != ProductUnitKind.Sell)
        {
            return ApplicationResult.Failure(
                DomainErrorCodes.InvalidProductUnitKind,
                "Only sell units can be used on sale lines.");
        }

        return ApplicationResult.Success();
    }
}
