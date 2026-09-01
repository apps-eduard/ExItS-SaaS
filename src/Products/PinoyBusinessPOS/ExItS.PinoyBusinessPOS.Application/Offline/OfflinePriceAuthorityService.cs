using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Application.Offline;

/// <summary>One requested lease: a product, optionally at one of its sell units.</summary>
public sealed record OfflinePriceAuthorityRequestItem(Guid ProductId, Guid? SellingUnitId = null);

/// <summary>
/// Issues and verifies offline price leases.
///
/// Issuing reads the live catalog; verification reads only the signature. Nothing in between can
/// change the price, which is the whole point: the price a cashier saw while offline is the price
/// the server already committed to, not a number the device asks the server to believe.
/// </summary>
public interface IOfflinePriceAuthorityService
{
    Task<ApplicationResult<IReadOnlyList<OfflinePriceAuthority>>> IssueAsync(
        Guid organizationId,
        Guid? branchId,
        IReadOnlyList<OfflinePriceAuthorityRequestItem> items,
        CancellationToken cancellationToken = default);

    OfflinePriceAuthorityVerification Verify(
        OfflinePriceAuthority authority,
        Guid expectedOrganizationId,
        Guid? expectedBranchId,
        Guid expectedProductId,
        Guid? expectedSellingUnitId);
}

public sealed class OfflinePriceAuthorityService : IOfflinePriceAuthorityService
{
    /// <summary>A single sell-floor load should not be able to mint an unbounded price book. </summary>
    public const int MaxItemsPerRequest = 500;

    private readonly ICatalogProductRepository _products;
    private readonly ICatalogProductUnitRepository _units;
    private readonly IClock _clock;
    private readonly OfflinePriceAuthorityOptions _options;
    private readonly IEffectivePriceResolver? _effectivePrices;

    public OfflinePriceAuthorityService(
        ICatalogProductRepository products,
        ICatalogProductUnitRepository units,
        IClock clock,
        IOptions<OfflinePriceAuthorityOptions> options,
        IEffectivePriceResolver? effectivePrices = null)
    {
        _products = products;
        _units = units;
        _clock = clock;
        _options = options.Value;
        _effectivePrices = effectivePrices;
    }

    public async Task<ApplicationResult<IReadOnlyList<OfflinePriceAuthority>>> IssueAsync(
        Guid organizationId,
        Guid? branchId,
        IReadOnlyList<OfflinePriceAuthorityRequestItem> items,
        CancellationToken cancellationToken = default)
    {
        if (items is null || items.Count == 0)
        {
            return ApplicationResult<IReadOnlyList<OfflinePriceAuthority>>.Failure(
                ApplicationErrorCodes.OfflinePriceAuthorityRequestInvalid,
                "At least one product is required to issue offline price authorities.");
        }

        if (items.Count > MaxItemsPerRequest)
        {
            return ApplicationResult<IReadOnlyList<OfflinePriceAuthority>>.Failure(
                ApplicationErrorCodes.OfflinePriceAuthorityRequestInvalid,
                $"At most {MaxItemsPerRequest} offline price authorities can be issued in one request.");
        }

        var orgId = PosOrganizationId.From(organizationId);
        var productIds = items
            .Select(i => CatalogProductId.From(i.ProductId))
            .Distinct()
            .ToList();
        var products = await _products
            .ListByIdsAsync(orgId, productIds, cancellationToken)
            .ConfigureAwait(false);
        var byId = products.ToDictionary(p => p.Id.Value);

        IReadOnlyDictionary<EffectivePriceKey, EffectivePriceResult>? effectivePrices = null;
        if (branchId is Guid bid && bid != Guid.Empty && _effectivePrices is not null)
        {
            var unitIds = items
                .Where(i => i.SellingUnitId is not null)
                .Select(i => ProductUnitId.From(i.SellingUnitId!.Value))
                .Distinct()
                .ToList();
            var unitsByProduct = new Dictionary<CatalogProductId, List<CatalogProductUnit>>();
            foreach (var unitId in unitIds)
            {
                var unit = await _units.GetByIdAsync(orgId, unitId, cancellationToken).ConfigureAwait(false);
                if (unit is not null)
                {
                    if (!unitsByProduct.TryGetValue(unit.ProductId, out var list))
                    {
                        list = [];
                        unitsByProduct[unit.ProductId] = list;
                    }

                    list.Add(unit);
                }
            }

            effectivePrices = await _effectivePrices
                .ResolveAsync(
                    orgId,
                    PosBranchId.From(bid),
                    products,
                    unitsByProduct.ToDictionary(
                        kvp => kvp.Key,
                        kvp => (IReadOnlyList<CatalogProductUnit>)kvp.Value),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        var issuedAt = Truncate(_clock.UtcNow);
        var expiresAt = issuedAt.AddHours(ValidityHours);
        var issued = new List<OfflinePriceAuthority>(items.Count);

        foreach (var item in items)
        {
            if (!byId.TryGetValue(item.ProductId, out var product))
            {
                // Silently skipping would let the sell floor believe it can sell an unknown product.
                return ApplicationResult<IReadOnlyList<OfflinePriceAuthority>>.Failure(
                    ApplicationErrorCodes.SaleProductNotFound,
                    "One or more products were not found in this organization.");
            }

            if (product.Status != CatalogProductStatus.Active)
            {
                return ApplicationResult<IReadOnlyList<OfflinePriceAuthority>>.Failure(
                    ApplicationErrorCodes.SaleProductNotActive,
                    $"'{product.Name}' is inactive and cannot be leased for offline selling.");
            }

            var unitPrice = effectivePrices?.TryGetValue(
                    EffectivePriceKeys.ForBaseProduct(item.ProductId),
                    out var baseEffective) == true
                ? baseEffective.EffectivePrice
                : product.SellingPrice;
            if (item.SellingUnitId is not null)
            {
                var unit = await _units
                    .GetByIdAsync(orgId, ProductUnitId.From(item.SellingUnitId.Value), cancellationToken)
                    .ConfigureAwait(false);
                if (unit is null)
                {
                    return ApplicationResult<IReadOnlyList<OfflinePriceAuthority>>.Failure(
                        DomainErrorCodes.InvalidProductUnitId,
                        "Selling unit was not found for this product.");
                }

                var validation = CheckoutSaleLineSnapshots.ValidateSellUnit(unit, product);
                if (!validation.IsSuccess)
                {
                    return ApplicationResult<IReadOnlyList<OfflinePriceAuthority>>.Failure(
                        validation.ErrorCode!,
                        validation.ErrorMessage!);
                }

                unitPrice = effectivePrices?.TryGetValue(
                        EffectivePriceKeys.ForSellUnit(item.ProductId, unit.Id.Value),
                        out var unitEffective) == true
                    ? unitEffective.EffectivePrice
                    : unit.SellingPrice ?? unitPrice;
            }

            issued.Add(Create(
                organizationId,
                branchId,
                product,
                item.SellingUnitId,
                unitPrice,
                issuedAt,
                expiresAt));
        }

        return ApplicationResult<IReadOnlyList<OfflinePriceAuthority>>.Success(issued);
    }

    public OfflinePriceAuthorityVerification Verify(
        OfflinePriceAuthority authority,
        Guid expectedOrganizationId,
        Guid? expectedBranchId,
        Guid expectedProductId,
        Guid? expectedSellingUnitId)
    {
        ArgumentNullException.ThrowIfNull(authority);

        if (authority.AuthorityId == Guid.Empty
            || string.IsNullOrWhiteSpace(authority.Signature)
            || string.IsNullOrWhiteSpace(authority.UnitOfMeasure)
            || string.IsNullOrWhiteSpace(authority.SellingMode))
        {
            return OfflinePriceAuthorityVerification.Rejected(OfflinePriceAuthorityFailure.Malformed);
        }

        // Signature first: an unsigned lease has no trustworthy organization or product to compare.
        var canonical = OfflinePriceAuthoritySigning.Canonicalize(
            authority.AuthorityId,
            authority.OrganizationId,
            authority.BranchId,
            authority.ProductId,
            authority.SellingUnitId,
            authority.UnitPrice,
            authority.UnitOfMeasure,
            authority.SellingMode,
            authority.IssuedAtUtc,
            authority.ExpiresAtUtc);
        var expected = OfflinePriceAuthoritySigning.Sign(SigningKey, canonical);
        if (!OfflinePriceAuthoritySigning.SignatureMatches(expected, authority.Signature))
        {
            return OfflinePriceAuthorityVerification.Rejected(OfflinePriceAuthorityFailure.Tampered);
        }

        if (authority.OrganizationId != expectedOrganizationId)
        {
            return OfflinePriceAuthorityVerification.Rejected(OfflinePriceAuthorityFailure.WrongOrganization);
        }

        if (authority.BranchId != expectedBranchId)
        {
            return OfflinePriceAuthorityVerification.Rejected(OfflinePriceAuthorityFailure.WrongBranch);
        }

        if (authority.ProductId != expectedProductId || authority.SellingUnitId != expectedSellingUnitId)
        {
            return OfflinePriceAuthorityVerification.Rejected(OfflinePriceAuthorityFailure.WrongProductBinding);
        }

        if (authority.ExpiresAtUtc <= authority.IssuedAtUtc || _clock.UtcNow > authority.ExpiresAtUtc)
        {
            return OfflinePriceAuthorityVerification.Rejected(OfflinePriceAuthorityFailure.Expired);
        }

        return OfflinePriceAuthorityVerification.Success(authority);
    }

    private int ValidityHours =>
        _options.PriceAuthorityValidityHours > 0 ? _options.PriceAuthorityValidityHours : 8;

    private string SigningKey =>
        string.IsNullOrWhiteSpace(_options.PriceAuthoritySigningKey)
            ? OfflinePriceAuthorityOptions.DevelopmentSigningKey
            : _options.PriceAuthoritySigningKey;

    private OfflinePriceAuthority Create(
        Guid organizationId,
        Guid? branchId,
        CatalogProduct product,
        Guid? sellingUnitId,
        decimal unitPrice,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        var authorityId = Guid.NewGuid();
        var unitOfMeasure = UnitOfMeasures.ToCode(product.UnitOfMeasure);
        var sellingMode = SellingModes.ToCode(product.SellingMode);
        var canonical = OfflinePriceAuthoritySigning.Canonicalize(
            authorityId,
            organizationId,
            branchId,
            product.Id.Value,
            sellingUnitId,
            unitPrice,
            unitOfMeasure,
            sellingMode,
            issuedAtUtc,
            expiresAtUtc);

        return new OfflinePriceAuthority(
            authorityId,
            organizationId,
            branchId,
            product.Id.Value,
            sellingUnitId,
            unitPrice,
            unitOfMeasure,
            sellingMode,
            issuedAtUtc,
            expiresAtUtc,
            OfflinePriceAuthoritySigning.Sign(SigningKey, canonical));
    }

    /// <summary>Whole seconds only — the canonical form signs Unix seconds. </summary>
    private static DateTimeOffset Truncate(DateTimeOffset value) =>
        DateTimeOffset.FromUnixTimeSeconds(value.ToUnixTimeSeconds());
}
