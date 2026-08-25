using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Sales;

namespace ExItS.PinoyBusinessPOS.Application.Sales;

/// <summary>
/// Builds sale line drafts from server-signed offline price leases (RMAP-21 Review Repair 01).
///
/// The lease is the only price input on this path. Any <c>UnitPriceSnapshot</c> or <c>LineTotal</c>
/// the device also sent is treated as a claim to check, never as a source of truth: if it disagrees
/// with the lease the sale is refused rather than silently repriced in either direction.
/// </summary>
public static class CheckoutSaleLineAuthorities
{
    public static bool HasAuthority(CheckoutSaleLineRequest? line) =>
        line?.OfflinePriceAuthority is not null;

    public static bool RequestUsesOfflinePriceAuthorities(IReadOnlyList<CheckoutSaleLineRequest>? lines) =>
        lines is not null && lines.Any(HasAuthority);

    public static ApplicationResult<SaleLineDraft> TryCreateDraftFromAuthority(
        CheckoutSaleLineRequest line,
        CatalogProduct product,
        CatalogProductUnit? sellingUnit,
        IOfflinePriceAuthorityService authorities,
        Guid organizationId,
        Guid? branchId)
    {
        ArgumentNullException.ThrowIfNull(line);
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(authorities);

        var token = line.OfflinePriceAuthority;
        if (token is null)
        {
            return ApplicationResult<SaleLineDraft>.Failure(
                ApplicationErrorCodes.OfflinePriceAuthorityRequiredOnEveryLine,
                "Every line of an offline authority sale must carry its own price authority.");
        }

        var verification = authorities.Verify(
            new OfflinePriceAuthority(
                token.AuthorityId,
                token.OrganizationId,
                token.BranchId,
                token.ProductId,
                token.SellingUnitId,
                token.UnitPrice,
                token.UnitOfMeasure,
                token.SellingMode,
                token.IssuedAtUtc,
                token.ExpiresAtUtc,
                token.Signature),
            organizationId,
            branchId,
            line.ProductId,
            line.SellingUnitId);

        if (!verification.IsValid)
        {
            var (code, message) = Describe(verification.Failure);
            return ApplicationResult<SaleLineDraft>.Failure(code, message);
        }

        if (!UnitOfMeasures.TryParse(verification.UnitOfMeasure, out var uom))
        {
            return ApplicationResult<SaleLineDraft>.Failure(
                ApplicationErrorCodes.SaleSnapshotInvalid,
                "Offline price authority carries an unrecognized unit of measure.");
        }

        SellingMode sellingMode;
        try
        {
            sellingMode = SellingModes.Parse(verification.SellingMode);
        }
        catch (DomainException ex)
        {
            return ApplicationResult<SaleLineDraft>.Failure(ApplicationErrorCodes.SaleSnapshotInvalid, ex.Message);
        }

        try
        {
            var unitPrice = SaleLine.NormalizeUnitPrice(verification.UnitPrice);

            if (line.SellingUnitId is not null || sellingUnit is not null)
            {
                if (sellingUnit is null)
                {
                    return ApplicationResult<SaleLineDraft>.Failure(
                        DomainErrorCodes.InvalidProductUnitId,
                        "Selling unit was not found for this sale line.");
                }

                var unitValidation = CheckoutSaleLineSnapshots.ValidateSellUnit(sellingUnit, product);
                if (!unitValidation.IsSuccess)
                {
                    return ApplicationResult<SaleLineDraft>.Failure(
                        unitValidation.ErrorCode!,
                        unitValidation.ErrorMessage!);
                }

                var entered = line.EnteredQuantity ?? line.Quantity;
                var mismatch = EnsureClientClaimsAgree(line, unitPrice, entered);
                if (mismatch is not null)
                {
                    return mismatch;
                }

                var multiplier = sellingUnit.MultiplierToBase;
                return ApplicationResult<SaleLineDraft>.Success(new SaleLineDraft(
                    product.Id,
                    product.Name,
                    product.Sku,
                    product.Barcode,
                    uom,
                    unitPrice,
                    ProductUnitConversion.ToBaseQuantity(entered, multiplier),
                    sellingMode,
                    sellingUnit.Id,
                    sellingUnit.DisplayName,
                    entered,
                    multiplier));
            }

            if (sellingMode == SellingMode.ByWeight)
            {
                SellingModes.EnsureCompatible(sellingMode, uom);
            }

            var quantity = SaleLine.NormalizeQuantity(line.Quantity, uom, sellingMode);
            var simpleMismatch = EnsureClientClaimsAgree(line, unitPrice, quantity);
            if (simpleMismatch is not null)
            {
                return simpleMismatch;
            }

            return ApplicationResult<SaleLineDraft>.Success(new SaleLineDraft(
                product.Id,
                product.Name,
                product.Sku,
                product.Barcode,
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

    /// <summary>
    /// The device may echo the amounts it printed on the customer's receipt. They must match the
    /// lease exactly; a disagreement means the receipt and the recorded sale would differ.
    /// </summary>
    private static ApplicationResult<SaleLineDraft>? EnsureClientClaimsAgree(
        CheckoutSaleLineRequest line,
        decimal authorityUnitPrice,
        decimal billedQuantity)
    {
        if (line.UnitPriceSnapshot is not null && line.UnitPriceSnapshot.Value != authorityUnitPrice)
        {
            return ApplicationResult<SaleLineDraft>.Failure(
                ApplicationErrorCodes.OfflinePriceAuthorityLineMismatch,
                "Line unit price does not match the price authority the server issued.");
        }

        if (line.LineTotal is not null)
        {
            var expected = SaleMoney.RoundMoney(authorityUnitPrice * billedQuantity);
            if (line.LineTotal.Value != expected)
            {
                return ApplicationResult<SaleLineDraft>.Failure(
                    ApplicationErrorCodes.OfflinePriceAuthorityLineMismatch,
                    $"Line total must equal RoundMoney(authority unit price × quantity) ({expected}).");
            }
        }

        return null;
    }

    private static (string Code, string Message) Describe(OfflinePriceAuthorityFailure failure) =>
        failure switch
        {
            OfflinePriceAuthorityFailure.Expired => (
                ApplicationErrorCodes.OfflinePriceAuthorityExpired,
                "This offline price authority has expired. Reconnect to refresh prices before selling."),
            OfflinePriceAuthorityFailure.WrongOrganization => (
                ApplicationErrorCodes.OfflinePriceAuthorityWrongOrganization,
                "This offline price authority was issued for a different organization."),
            OfflinePriceAuthorityFailure.WrongBranch => (
                ApplicationErrorCodes.OfflinePriceAuthorityWrongBranch,
                "This offline price authority was issued for a different branch."),
            OfflinePriceAuthorityFailure.WrongProductBinding => (
                ApplicationErrorCodes.OfflinePriceAuthorityWrongProduct,
                "This offline price authority was issued for a different product or selling unit."),
            _ => (
                ApplicationErrorCodes.OfflinePriceAuthorityTampered,
                "This offline price authority could not be verified.")
        };
}
