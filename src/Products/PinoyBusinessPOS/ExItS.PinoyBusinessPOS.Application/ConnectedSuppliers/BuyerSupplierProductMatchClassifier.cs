using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;

namespace ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;

/// <summary>
/// Buyer-perspective classification of a shared supplier product against the buyer catalog.
/// Pure / side-effect free — never links products.
/// </summary>
public enum BuyerSupplierProductMatchStatus
{
    /// <summary>Already linked, or safe exact Name+SKU+Barcode+UOM unique match (auto-link eligible).</summary>
    Ready = 0,

    /// <summary>No credible buyer product match.</summary>
    New = 1,

    /// <summary>Likely or partial match needing explicit confirmation.</summary>
    Review = 2,

    /// <summary>Identifiers point at different buyer products or are ambiguous.</summary>
    Conflict = 3,

    /// <summary>Supplier product already has an active buyer link.</summary>
    AlreadyLinked = 4
}

public sealed record BuyerSupplierProductMatchEvidence(
    bool NameMatched,
    bool SkuMatched,
    bool BarcodeMatched,
    bool UnitCompatible);

public sealed record BuyerSupplierProductMatchClassification(
    BuyerSupplierProductMatchStatus Status,
    bool CanAutoLink,
    Guid? CandidateBuyerProductId,
    BuyerSupplierProductMatchEvidence Evidence,
    string MatchDetails);

/// <summary>
/// Pure match engine for buyer ↔ shared supplier product readiness.
/// Safe exact auto-link is signaled only via <see cref="BuyerSupplierProductMatchClassification.CanAutoLink"/>.
/// </summary>
public static class BuyerSupplierProductMatchClassifier
{
    public static BuyerSupplierProductMatchClassification Classify(
        string supplierName,
        string? supplierSku,
        string? supplierBarcode,
        string supplierUnitOfMeasureCode,
        IEnumerable<CatalogProduct> activeBuyerProducts,
        Guid? existingLinkedBuyerProductId = null)
    {
        if (existingLinkedBuyerProductId is Guid linkedId && linkedId != Guid.Empty)
        {
            var linked = activeBuyerProducts.FirstOrDefault(p => p.Id.Value == linkedId);
            var evidence = linked is null
                ? new BuyerSupplierProductMatchEvidence(false, false, false, false)
                : BuildEvidence(
                    linked,
                    NormalizeName(supplierName),
                    NormalizeSku(supplierSku),
                    NormalizeBarcode(supplierBarcode),
                    TryParseUom(supplierUnitOfMeasureCode));

            return new BuyerSupplierProductMatchClassification(
                BuyerSupplierProductMatchStatus.AlreadyLinked,
                CanAutoLink: false,
                linkedId,
                evidence,
                "Already linked to a buyer catalog product.");
        }

        var nameNorm = NormalizeName(supplierName);
        var skuNorm = NormalizeSku(supplierSku);
        var barcodeNorm = NormalizeBarcode(supplierBarcode);
        var supplierUom = TryParseUom(supplierUnitOfMeasureCode);

        var evaluated = activeBuyerProducts
            .Where(p => p.Status == CatalogProductStatus.Active)
            .Select(p => (Product: p, Evidence: BuildEvidence(p, nameNorm, skuNorm, barcodeNorm, supplierUom)))
            .ToList();

        var skuHits = evaluated.Where(x => x.Evidence.SkuMatched).Select(x => x.Product.Id.Value).Distinct().ToList();
        var barcodeHits = evaluated.Where(x => x.Evidence.BarcodeMatched).Select(x => x.Product.Id.Value).Distinct().ToList();
        var nameHits = evaluated.Where(x => x.Evidence.NameMatched).Select(x => x.Product.Id.Value).Distinct().ToList();

        if (skuHits.Count > 1 || barcodeHits.Count > 1)
        {
            return Conflict(
                PickEvidence(evaluated, null),
                "Multiple buyer products match the same SKU or barcode.");
        }

        var claimed = new HashSet<Guid>();
        if (skuHits.Count == 1)
        {
            claimed.Add(skuHits[0]);
        }

        if (barcodeHits.Count == 1)
        {
            claimed.Add(barcodeHits[0]);
        }

        if (nameHits.Count == 1
            && (skuHits.Count == 1 || barcodeHits.Count == 1))
        {
            claimed.Add(nameHits[0]);
        }

        if (claimed.Count > 1)
        {
            return Conflict(
                PickEvidence(evaluated, null),
                "Identifiers point to different buyer products.");
        }

        var allIdentifiersPresent = !string.IsNullOrEmpty(nameNorm)
            && skuNorm is not null
            && barcodeNorm is not null;

        var exact = evaluated
            .Where(x =>
                allIdentifiersPresent
                && x.Evidence.NameMatched
                && x.Evidence.SkuMatched
                && x.Evidence.BarcodeMatched
                && x.Evidence.UnitCompatible)
            .ToList();

        if (exact.Count > 1)
        {
            return Conflict(
                exact[0].Evidence,
                "Multiple buyer products satisfy exact Name+SKU+Barcode+UOM.");
        }

        if (exact.Count == 1)
        {
            var hit = exact[0];
            return new BuyerSupplierProductMatchClassification(
                BuyerSupplierProductMatchStatus.Ready,
                CanAutoLink: true,
                hit.Product.Id.Value,
                hit.Evidence,
                "Exact Name+SKU+Barcode with compatible UOM.");
        }

        // Identifiers match one product but UOM incompatible → never auto-link.
        var exactIdsIncompatibleUom = evaluated
            .Where(x =>
                allIdentifiersPresent
                && x.Evidence.NameMatched
                && x.Evidence.SkuMatched
                && x.Evidence.BarcodeMatched
                && !x.Evidence.UnitCompatible)
            .ToList();
        if (exactIdsIncompatibleUom.Count == 1)
        {
            var hit = exactIdsIncompatibleUom[0];
            return new BuyerSupplierProductMatchClassification(
                BuyerSupplierProductMatchStatus.Review,
                CanAutoLink: false,
                hit.Product.Id.Value,
                hit.Evidence,
                "Exact identifiers match but unit of measure is incompatible.");
        }

        if (exactIdsIncompatibleUom.Count > 1)
        {
            return Conflict(
                exactIdsIncompatibleUom[0].Evidence,
                "Multiple buyer products match identifiers with incompatible UOM.");
        }

        // Name + SKU (barcode missing or different on the same product) → REVIEW
        var nameSku = evaluated
            .Where(x => x.Evidence.NameMatched && x.Evidence.SkuMatched && x.Evidence.UnitCompatible)
            .ToList();
        if (nameSku.Count == 1 && !nameSku[0].Evidence.BarcodeMatched)
        {
            var hit = nameSku[0];
            return new BuyerSupplierProductMatchClassification(
                BuyerSupplierProductMatchStatus.Review,
                CanAutoLink: false,
                hit.Product.Id.Value,
                hit.Evidence,
                "Name and SKU match; barcode missing or different — confirm before linking.");
        }

        if (nameSku.Count > 1)
        {
            return Conflict(
                nameSku[0].Evidence,
                "Multiple buyer products match Name+SKU.");
        }

        // Name + Barcode (SKU missing or different) → REVIEW
        var nameBarcode = evaluated
            .Where(x => x.Evidence.NameMatched && x.Evidence.BarcodeMatched && x.Evidence.UnitCompatible)
            .ToList();
        if (nameBarcode.Count == 1 && !nameBarcode[0].Evidence.SkuMatched)
        {
            var hit = nameBarcode[0];
            return new BuyerSupplierProductMatchClassification(
                BuyerSupplierProductMatchStatus.Review,
                CanAutoLink: false,
                hit.Product.Id.Value,
                hit.Evidence,
                "Name and barcode match; SKU missing or different — confirm before linking.");
        }

        if (nameBarcode.Count > 1)
        {
            return Conflict(
                nameBarcode[0].Evidence,
                "Multiple buyer products match Name+Barcode.");
        }

        // SKU only → REVIEW if unique + UOM compatible
        if (skuHits.Count == 1)
        {
            var hit = evaluated.First(x => x.Product.Id.Value == skuHits[0]);
            if (hit.Evidence.UnitCompatible && !hit.Evidence.NameMatched && !hit.Evidence.BarcodeMatched)
            {
                return new BuyerSupplierProductMatchClassification(
                    BuyerSupplierProductMatchStatus.Review,
                    CanAutoLink: false,
                    hit.Product.Id.Value,
                    hit.Evidence,
                    "SKU-only match with compatible UOM — confirm before linking.");
            }
        }

        // Barcode only → REVIEW if unique + UOM compatible
        if (barcodeHits.Count == 1)
        {
            var hit = evaluated.First(x => x.Product.Id.Value == barcodeHits[0]);
            if (hit.Evidence.UnitCompatible && !hit.Evidence.NameMatched && !hit.Evidence.SkuMatched)
            {
                return new BuyerSupplierProductMatchClassification(
                    BuyerSupplierProductMatchStatus.Review,
                    CanAutoLink: false,
                    hit.Product.Id.Value,
                    hit.Evidence,
                    "Barcode-only match with compatible UOM — confirm before linking.");
            }
        }

        // Name only → weak REVIEW (never auto-link); may mark UOM incompatible
        if (nameHits.Count == 1)
        {
            var hit = evaluated.First(x => x.Product.Id.Value == nameHits[0]);
            if (!hit.Evidence.SkuMatched && !hit.Evidence.BarcodeMatched)
            {
                return new BuyerSupplierProductMatchClassification(
                    BuyerSupplierProductMatchStatus.Review,
                    CanAutoLink: false,
                    hit.Product.Id.Value,
                    hit.Evidence,
                    hit.Evidence.UnitCompatible
                        ? "Name-only match — confirm before linking."
                        : "Name-only match with incompatible unit of measure — confirm before linking.");
            }
        }

        if (nameHits.Count > 1
            && skuHits.Count == 0
            && barcodeHits.Count == 0)
        {
            var best = evaluated
                .Where(x => x.Evidence.NameMatched)
                .OrderByDescending(x => x.Evidence.UnitCompatible)
                .ThenBy(x => x.Product.Name, StringComparer.OrdinalIgnoreCase)
                .First();
            return new BuyerSupplierProductMatchClassification(
                BuyerSupplierProductMatchStatus.Review,
                CanAutoLink: false,
                best.Product.Id.Value,
                best.Evidence,
                "Multiple name-only matches — confirm before linking.");
        }

        // Partial identifier hits that did not resolve above (e.g. SKU match with wrong UOM)
        if (skuHits.Count == 1 || barcodeHits.Count == 1 || nameHits.Count >= 1)
        {
            Guid? candidate = skuHits.Count == 1
                ? skuHits[0]
                : barcodeHits.Count == 1
                    ? barcodeHits[0]
                    : nameHits.Count == 1 ? nameHits[0] : null;
            var evidence = PickEvidence(evaluated, candidate);
            return new BuyerSupplierProductMatchClassification(
                BuyerSupplierProductMatchStatus.Review,
                CanAutoLink: false,
                candidate,
                evidence,
                "Partial match needs confirmation.");
        }

        return new BuyerSupplierProductMatchClassification(
            BuyerSupplierProductMatchStatus.New,
            CanAutoLink: false,
            null,
            new BuyerSupplierProductMatchEvidence(false, false, false, false),
            "No credible buyer product match.");
    }

    /// <summary>
    /// Maps classification status used by auto-link counters (AlreadyLinked counts as Ready).
    /// </summary>
    public static BuyerSupplierProductMatchStatus ToReadinessBucket(
        BuyerSupplierProductMatchStatus status) =>
        status == BuyerSupplierProductMatchStatus.AlreadyLinked
            ? BuyerSupplierProductMatchStatus.Ready
            : status;

    private static BuyerSupplierProductMatchClassification Conflict(
        BuyerSupplierProductMatchEvidence evidence,
        string details) =>
        new(
            BuyerSupplierProductMatchStatus.Conflict,
            CanAutoLink: false,
            null,
            evidence,
            details);

    private static BuyerSupplierProductMatchEvidence PickEvidence(
        IReadOnlyList<(CatalogProduct Product, BuyerSupplierProductMatchEvidence Evidence)> evaluated,
        Guid? candidateId)
    {
        if (candidateId is Guid id)
        {
            var hit = evaluated.FirstOrDefault(x => x.Product.Id.Value == id);
            if (hit.Product is not null)
            {
                return hit.Evidence;
            }
        }

        return evaluated.Count > 0
            ? evaluated[0].Evidence
            : new BuyerSupplierProductMatchEvidence(false, false, false, false);
    }

    private static BuyerSupplierProductMatchEvidence BuildEvidence(
        CatalogProduct product,
        string supplierNameNorm,
        string? supplierSkuNorm,
        string? supplierBarcodeNorm,
        UnitOfMeasure? supplierUom)
    {
        var nameMatched = !string.IsNullOrEmpty(supplierNameNorm)
            && string.Equals(NormalizeName(product.Name), supplierNameNorm, StringComparison.Ordinal);
        var skuMatched = supplierSkuNorm is not null
            && product.NormalizedSku is not null
            && string.Equals(product.NormalizedSku, supplierSkuNorm, StringComparison.Ordinal);
        var barcodeMatched = supplierBarcodeNorm is not null
            && product.Barcode is not null
            && string.Equals(product.Barcode, supplierBarcodeNorm, StringComparison.Ordinal);
        var unitCompatible = supplierUom is UnitOfMeasure uom && product.UnitOfMeasure == uom;
        return new BuyerSupplierProductMatchEvidence(nameMatched, skuMatched, barcodeMatched, unitCompatible);
    }

    internal static string NormalizeName(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToUpperInvariant();

    internal static string? NormalizeSku(string? value)
    {
        try
        {
            var (_, normalized) = CatalogProduct.NormalizeOptionalSku(value);
            return normalized;
        }
        catch (DomainException)
        {
            return null;
        }
    }

    internal static string? NormalizeBarcode(string? value)
    {
        try
        {
            return CatalogProduct.NormalizeOptionalBarcode(value);
        }
        catch (DomainException)
        {
            return null;
        }
    }

    private static UnitOfMeasure? TryParseUom(string? code) =>
        UnitOfMeasures.TryParse(code, out var uom) ? uom : null;
}
