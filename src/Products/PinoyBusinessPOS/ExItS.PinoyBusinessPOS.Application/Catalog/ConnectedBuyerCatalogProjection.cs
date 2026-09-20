using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.ConnectedSuppliers;

namespace ExItS.PinoyBusinessPOS.Application.Catalog;

/// <summary>
/// Canonical seller-side eligibility and effective buyer visibility for Connected Buyer catalogs.
/// Must stay aligned with buyer catalog SQL (tracked + sharing mode + exclusion).
/// </summary>
public static class ConnectedBuyerCatalogProjection
{
    public static bool IsEligible(CatalogProduct product, bool isInventoryTracked) =>
        product.Scope != CatalogProductScope.BranchLocal
        && !product.IsBlockedFromConnectedBuyers
        && product.CanBeSold
        && isInventoryTracked;

    public static bool IsExplicitlyExcluded(ConnectedBuyerProductShare? share) =>
        share is { IsShared: false };

    /// <summary>
    /// Preference from the share row only (ignores eligibility).
    /// AllEligible + no row ⇒ preferred shared; SelectedOnly + no row ⇒ not preferred.
    /// </summary>
    public static bool IsPreferredShared(CatalogSharingMode mode, ConnectedBuyerProductShare? share) =>
        ConnectedPoPricing.IsProductShared(mode, share);

    /// <summary>
    /// Buyer-visible right now: eligible AND preferred shared under the relationship mode.
    /// </summary>
    public static bool IsEffectivelyShared(
        CatalogSharingMode mode,
        CatalogProduct product,
        bool isInventoryTracked,
        ConnectedBuyerProductShare? share) =>
        IsEligible(product, isInventoryTracked) && IsPreferredShared(mode, share);

    public static string SharingStatus(
        CatalogSharingMode mode,
        CatalogProduct product,
        bool isInventoryTracked,
        ConnectedBuyerProductShare? share)
    {
        if (!IsEligible(product, isInventoryTracked))
        {
            return "Ineligible";
        }

        if (IsExplicitlyExcluded(share))
        {
            return mode == CatalogSharingMode.AllEligible ? "Excluded" : "NotShared";
        }

        return IsPreferredShared(mode, share) ? "Shared" : "NotShared";
    }

    /// <summary>
    /// Canonical order baseline for sharing / readiness:
    /// Default PO (or exposed supplier order price) when &gt; 0, else SellingPrice when &gt; 0.
    /// </summary>
    public static decimal? ResolvePoPrice(
        CatalogProduct product,
        SupplierProductExposure? exposure = null)
    {
        if (product.DefaultConnectedPoPrice is > 0m)
        {
            return product.DefaultConnectedPoPrice;
        }

        if (exposure is { IsExposed: true, SupplierOrderPrice: > 0m })
        {
            return exposure.SupplierOrderPrice;
        }

        if (product.SellingPrice is > 0m)
        {
            return product.SellingPrice;
        }

        return null;
    }

    /// <summary>
    /// True when <see cref="ResolvePoPrice"/> yields a usable order price.
    /// </summary>
    public static bool HasValidPoPrice(
        CatalogProduct product,
        SupplierProductExposure? exposure = null) =>
        ResolvePoPrice(product, exposure) is > 0m;

    /// <summary>
    /// Eligible, not currently buyer-visible, and has a usable order price for Share.
    /// </summary>
    public static bool CanShare(
        CatalogSharingMode mode,
        CatalogProduct product,
        bool isInventoryTracked,
        ConnectedBuyerProductShare? share,
        SupplierProductExposure? exposure = null) =>
        IsEligible(product, isInventoryTracked)
        && !IsEffectivelyShared(mode, product, isInventoryTracked, share)
        && HasValidPoPrice(product, exposure);

    /// <summary>
    /// Currently buyer-visible — Stop sharing remains available even if pricing later regresses.
    /// </summary>
    public static bool CanStopSharing(
        CatalogSharingMode mode,
        CatalogProduct product,
        bool isInventoryTracked,
        ConnectedBuyerProductShare? share) =>
        IsEffectivelyShared(mode, product, isInventoryTracked, share);

    public static bool IsSelectableForBulk(
        CatalogSharingMode mode,
        CatalogProduct product,
        bool isInventoryTracked,
        ConnectedBuyerProductShare? share,
        SupplierProductExposure? exposure = null) =>
        CanShare(mode, product, isInventoryTracked, share, exposure)
        || CanStopSharing(mode, product, isInventoryTracked, share);
}
