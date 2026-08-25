using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Offline;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Sales;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.UnitTests.Offline;

/// <summary>
/// RMAP-21 Review Repair 01: an offline Cash sale is priced by a lease the server signed, not by a
/// number the device reports. These tests pin the two halves of that claim — that a genuine lease
/// survives a live catalog price change, and that anything the device edits is refused.
/// </summary>
public sealed class OfflinePriceAuthorityTests
{
    private static readonly Guid OrgId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherOrgId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid BranchId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OtherBranchId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly DateTimeOffset Now = new(2026, 8, 21, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Issued_authority_verifies_against_the_same_scope()
    {
        var product = Coke(100m);
        var (service, _) = Build(Now, [product]);

        var issued = await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)]);

        Assert.True(issued.IsSuccess);
        var authority = Assert.Single(issued.Value!);
        Assert.Equal(100m, authority.UnitPrice);
        Assert.Equal(OrgId, authority.OrganizationId);
        Assert.Equal(BranchId, authority.BranchId);
        Assert.Equal(product.Id.Value, authority.ProductId);
        Assert.Equal(Now.AddHours(8), authority.ExpiresAtUtc);
        Assert.Matches("^[0-9a-f]{64}$", authority.Signature);

        var verified = service.Verify(authority, OrgId, BranchId, product.Id.Value, null);
        Assert.True(verified.IsValid);
        Assert.Equal(100m, verified.UnitPrice);
    }

    [Fact]
    public async Task Validity_window_is_configurable_and_defaults_to_eight_hours()
    {
        var product = Coke(100m);
        var (defaults, _) = Build(Now, [product]);
        var eightHours = Assert.Single((await defaults.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);
        Assert.Equal(TimeSpan.FromHours(8), eightHours.ExpiresAtUtc - eightHours.IssuedAtUtc);

        var (twoHours, _) = Build(Now, [product], validityHours: 2);
        var shorter = Assert.Single((await twoHours.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);
        Assert.Equal(TimeSpan.FromHours(2), shorter.ExpiresAtUtc - shorter.IssuedAtUtc);
    }

    [Fact]
    public async Task Tampered_unit_price_is_rejected()
    {
        var product = Coke(100m);
        var (service, _) = Build(Now, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        var forged = authority with { UnitPrice = 1m };

        var verified = service.Verify(forged, OrgId, BranchId, product.Id.Value, null);
        Assert.False(verified.IsValid);
        Assert.Equal(OfflinePriceAuthorityFailure.Tampered, verified.Failure);
    }

    [Fact]
    public async Task Tampered_expiry_is_rejected()
    {
        var product = Coke(100m);
        var (service, _) = Build(Now, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        var extended = authority with { ExpiresAtUtc = authority.ExpiresAtUtc.AddDays(30) };

        Assert.Equal(
            OfflinePriceAuthorityFailure.Tampered,
            service.Verify(extended, OrgId, BranchId, product.Id.Value, null).Failure);
    }

    [Fact]
    public async Task A_key_from_another_deployment_cannot_mint_authorities()
    {
        var product = Coke(100m);
        var (attacker, _) = Build(Now, [product], signingKey: "some-other-deployment-key");
        var forged = Assert.Single((await attacker.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        var (server, _) = Build(Now, [product]);
        Assert.Equal(
            OfflinePriceAuthorityFailure.Tampered,
            server.Verify(forged, OrgId, BranchId, product.Id.Value, null).Failure);
    }

    [Fact]
    public async Task Authority_from_another_organization_is_rejected()
    {
        var product = Coke(100m);
        var (service, _) = Build(Now, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        var verified = service.Verify(authority, OtherOrgId, BranchId, product.Id.Value, null);
        Assert.False(verified.IsValid);
        Assert.Equal(OfflinePriceAuthorityFailure.WrongOrganization, verified.Failure);
    }

    [Fact]
    public async Task Authority_from_another_branch_is_rejected()
    {
        var product = Coke(100m);
        var (service, _) = Build(Now, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, OtherBranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        var verified = service.Verify(authority, OrgId, BranchId, product.Id.Value, null);
        Assert.False(verified.IsValid);
        Assert.Equal(OfflinePriceAuthorityFailure.WrongBranch, verified.Failure);
    }

    [Fact]
    public async Task Authority_for_another_product_is_rejected()
    {
        var coke = Coke(100m);
        var water = CatalogProduct.Create(PosOrganizationId.From(OrgId), "Water", UnitOfMeasure.Bottle, 20m, Now);
        var (service, _) = Build(Now, [coke, water]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(water.Id.Value)])).Value!);

        var verified = service.Verify(authority, OrgId, BranchId, coke.Id.Value, null);
        Assert.False(verified.IsValid);
        Assert.Equal(OfflinePriceAuthorityFailure.WrongProductBinding, verified.Failure);
    }

    [Fact]
    public async Task Expired_authority_is_rejected()
    {
        var product = Coke(100m);
        var clock = new MutableClock(Now);
        var (service, _) = Build(clock, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        clock.UtcNow = Now.AddHours(8).AddSeconds(1);

        var verified = service.Verify(authority, OrgId, BranchId, product.Id.Value, null);
        Assert.False(verified.IsValid);
        Assert.Equal(OfflinePriceAuthorityFailure.Expired, verified.Failure);
    }

    [Fact]
    public async Task Authority_still_valid_one_second_before_expiry()
    {
        var product = Coke(100m);
        var clock = new MutableClock(Now);
        var (service, _) = Build(clock, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        clock.UtcNow = Now.AddHours(8).AddSeconds(-1);

        Assert.True(service.Verify(authority, OrgId, BranchId, product.Id.Value, null).IsValid);
    }

    [Fact]
    public async Task Inactive_product_cannot_be_leased()
    {
        var product = Coke(100m);
        product.Deactivate(Now);
        var (service, _) = Build(Now, [product]);

        var issued = await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)]);

        Assert.False(issued.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.SaleProductNotActive, issued.ErrorCode);
    }

    [Fact]
    public async Task Sell_unit_authority_leases_the_unit_price()
    {
        var product = CatalogProduct.Create(PosOrganizationId.From(OrgId), "Soda", UnitOfMeasure.Piece, 10m, Now);
        var pack = CatalogProductUnit.Create(
            PosOrganizationId.From(OrgId),
            product.Id,
            ProductUnitKind.Sell,
            "6-pack",
            "6pk",
            6m,
            Now,
            sellingPrice: 55m);
        var (service, _) = Build(Now, [product], [pack]);

        var authority = Assert.Single(
            (await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value, pack.Id.Value)])).Value!);

        Assert.Equal(55m, authority.UnitPrice);
        Assert.Equal(pack.Id.Value, authority.SellingUnitId);
        Assert.True(service.Verify(authority, OrgId, BranchId, product.Id.Value, pack.Id.Value).IsValid);
        // The same lease may not be re-pointed at the base unit.
        Assert.Equal(
            OfflinePriceAuthorityFailure.WrongProductBinding,
            service.Verify(authority, OrgId, BranchId, product.Id.Value, null).Failure);
    }

    [Fact]
    public async Task Draft_keeps_the_leased_price_after_the_catalog_price_rises()
    {
        var product = Coke(100m);
        var (service, _) = Build(Now, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        Assert.True(product.UpdateSellingPrice(120m, Now.AddMinutes(30)));

        var draft = CheckoutSaleLineAuthorities.TryCreateDraftFromAuthority(
            LineFor(authority, product, quantity: 1m),
            product,
            null,
            service,
            OrgId,
            BranchId);

        Assert.True(draft.IsSuccess);
        Assert.Equal(100m, draft.Value!.UnitPrice);
        Assert.Equal(100m, SaleMoney.RoundMoney(draft.Value.UnitPrice * draft.Value.Quantity));
        Assert.NotEqual(product.SellingPrice, draft.Value.UnitPrice);
    }

    [Fact]
    public async Task Draft_keeps_the_leased_price_after_the_catalog_price_falls()
    {
        var product = Coke(100m);
        var (service, _) = Build(Now, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        Assert.True(product.UpdateSellingPrice(80m, Now.AddMinutes(30)));

        var draft = CheckoutSaleLineAuthorities.TryCreateDraftFromAuthority(
            LineFor(authority, product, quantity: 1m),
            product,
            null,
            service,
            OrgId,
            BranchId);

        Assert.True(draft.IsSuccess);
        Assert.Equal(100m, draft.Value!.UnitPrice);
    }

    [Fact]
    public async Task Weighted_half_kilo_at_120_is_60()
    {
        var tomato = CatalogProduct.Create(
            PosOrganizationId.From(OrgId),
            "Tomato",
            UnitOfMeasure.Kilogram,
            120m,
            Now,
            sellingMode: SellingMode.ByWeight);
        var (service, _) = Build(Now, [tomato]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(tomato.Id.Value)])).Value!);

        Assert.True(tomato.UpdateSellingPrice(150m, Now.AddMinutes(10)));

        var draft = CheckoutSaleLineAuthorities.TryCreateDraftFromAuthority(
            LineFor(authority, tomato, quantity: 0.5m, lineTotal: 60.00m),
            tomato,
            null,
            service,
            OrgId,
            BranchId);

        Assert.True(draft.IsSuccess);
        Assert.Equal(0.5m, draft.Value!.Quantity);
        Assert.Equal(60.00m, SaleMoney.RoundMoney(draft.Value.UnitPrice * draft.Value.Quantity));
    }

    [Fact]
    public async Task Client_line_total_that_disagrees_with_the_lease_is_rejected()
    {
        var product = Coke(100m);
        var (service, _) = Build(Now, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        var draft = CheckoutSaleLineAuthorities.TryCreateDraftFromAuthority(
            LineFor(authority, product, quantity: 1m, lineTotal: 1.00m),
            product,
            null,
            service,
            OrgId,
            BranchId);

        Assert.False(draft.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OfflinePriceAuthorityLineMismatch, draft.ErrorCode);
    }

    [Fact]
    public async Task Client_unit_price_snapshot_that_disagrees_with_the_lease_is_rejected()
    {
        var product = Coke(100m);
        var (service, _) = Build(Now, [product]);
        var authority = Assert.Single((await service.IssueAsync(OrgId, BranchId, [new OfflinePriceAuthorityRequestItem(product.Id.Value)])).Value!);

        var line = LineFor(authority, product, quantity: 1m) with { UnitPriceSnapshot = 5m };

        var draft = CheckoutSaleLineAuthorities.TryCreateDraftFromAuthority(
            line,
            product,
            null,
            service,
            OrgId,
            BranchId);

        Assert.False(draft.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.OfflinePriceAuthorityLineMismatch, draft.ErrorCode);
    }

    [Fact]
    public void Request_detection_only_fires_on_lines_that_carry_an_authority()
    {
        Assert.False(CheckoutSaleLineAuthorities.RequestUsesOfflinePriceAuthorities(
            [new CheckoutSaleLineRequest(Guid.NewGuid(), 1m)]));
        Assert.False(CheckoutSaleLineAuthorities.RequestUsesOfflinePriceAuthorities(
            [new CheckoutSaleLineRequest(Guid.NewGuid(), 1m, UnitPriceSnapshot: 10m)]));
        Assert.True(CheckoutSaleLineAuthorities.RequestUsesOfflinePriceAuthorities(
        [
            new CheckoutSaleLineRequest(
                Guid.NewGuid(),
                1m,
                OfflinePriceAuthority: new OfflinePriceAuthorityToken(
                    Guid.NewGuid(),
                    OrgId,
                    Guid.NewGuid(),
                    "sig",
                    Now,
                    Now.AddHours(8),
                    10m,
                    "Bottle",
                    "PerItem"))
        ]));
    }

    private static CheckoutSaleLineRequest LineFor(
        OfflinePriceAuthority authority,
        CatalogProduct product,
        decimal quantity,
        decimal? lineTotal = null) =>
        new(
            product.Id.Value,
            quantity,
            UnitPriceSnapshot: authority.UnitPrice,
            UnitOfMeasure: authority.UnitOfMeasure,
            SellingMode: authority.SellingMode,
            LineTotal: lineTotal ?? SaleMoney.RoundMoney(authority.UnitPrice * quantity),
            SellingUnitId: authority.SellingUnitId,
            OfflinePriceAuthority: new OfflinePriceAuthorityToken(
                authority.AuthorityId,
                authority.OrganizationId,
                authority.ProductId,
                authority.Signature,
                authority.IssuedAtUtc,
                authority.ExpiresAtUtc,
                authority.UnitPrice,
                authority.UnitOfMeasure,
                authority.SellingMode,
                authority.BranchId,
                authority.SellingUnitId));

    private static CatalogProduct Coke(decimal price) =>
        CatalogProduct.Create(PosOrganizationId.From(OrgId), "Coca-Cola 330ml", UnitOfMeasure.Bottle, price, Now);

    private static (OfflinePriceAuthorityService Service, MutableClock Clock) Build(
        DateTimeOffset now,
        IReadOnlyList<CatalogProduct> products,
        IReadOnlyList<CatalogProductUnit>? units = null,
        int validityHours = 8,
        string? signingKey = null) =>
        Build(new MutableClock(now), products, units, validityHours, signingKey);

    private static (OfflinePriceAuthorityService Service, MutableClock Clock) Build(
        MutableClock clock,
        IReadOnlyList<CatalogProduct> products,
        IReadOnlyList<CatalogProductUnit>? units = null,
        int validityHours = 8,
        string? signingKey = null)
    {
        var options = Options.Create(new OfflinePriceAuthorityOptions
        {
            PriceAuthoritySigningKey = signingKey ?? OfflinePriceAuthorityOptions.DevelopmentSigningKey,
            PriceAuthorityValidityHours = validityHours
        });
        var service = new OfflinePriceAuthorityService(
            new FakeProducts(products),
            new FakeUnits(units ?? []),
            clock,
            options);
        return (service, clock);
    }

    private sealed class MutableClock(DateTimeOffset now) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = now;
    }

    private sealed class FakeProducts(IReadOnlyList<CatalogProduct> products) : ICatalogProductRepository
    {
        public Task<CatalogProduct?> GetByIdAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult(products.FirstOrDefault(p => p.OrganizationId == organizationId && p.Id == productId));

        public Task<IReadOnlyList<CatalogProduct>> ListByIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProduct>>(
                products.Where(p => p.OrganizationId == organizationId && productIds.Contains(p.Id)).ToList());

        public Task<CatalogProduct?> FindByNormalizedSkuAsync(PosOrganizationId organizationId, string normalizedSku, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProduct?>(null);
        public Task<CatalogProduct?> FindByBarcodeAsync(PosOrganizationId organizationId, string barcode, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProduct?>(null);
        public Task<(IReadOnlyList<CatalogProduct> Items, int TotalCount)> ListAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<(IReadOnlyList<CatalogProduct>, int)>(([], 0));
        public Task<IReadOnlyList<Guid>> ListIdsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, int skip, int take, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
        public Task<(int TotalCount, int AvailableCount, int NotAvailableCount)> CountConnectedBuyerAvailabilityAsync(PosOrganizationId organizationId, CancellationToken cancellationToken = default) => Task.FromResult((0, 0, 0));
        public Task<IReadOnlyList<(Guid? CategoryId, int Count)>> ListConnectedBuyerAvailabilityCategoryFacetsAsync(PosOrganizationId organizationId, CatalogProductFilter filter, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<(Guid?, int)>>([]);
        public Task<CatalogProduct?> FindByPlatformGlobalProductIdAsync(PosOrganizationId organizationId, Guid platformGlobalProductId, CancellationToken cancellationToken = default) => Task.FromResult<CatalogProduct?>(null);
        public Task<IReadOnlySet<Guid>> ListPlatformGlobalProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<Guid> platformGlobalProductIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlySet<Guid>>(new HashSet<Guid>());
        public Task AddAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(CatalogProduct product, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeUnits(IReadOnlyList<CatalogProductUnit> units) : ICatalogProductUnitRepository
    {
        public Task<CatalogProductUnit?> GetByIdAsync(PosOrganizationId organizationId, ProductUnitId unitId, CancellationToken cancellationToken = default) =>
            Task.FromResult(units.FirstOrDefault(u => u.OrganizationId == organizationId && u.Id == unitId));

        public Task<IReadOnlyList<CatalogProductUnit>> ListByProductAsync(PosOrganizationId organizationId, CatalogProductId productId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CatalogProductUnit>>(units.Where(u => u.ProductId == productId).ToList());

        public Task<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>> ListByProductIdsAsync(PosOrganizationId organizationId, IReadOnlyCollection<CatalogProductId> productIds, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyDictionary<Guid, IReadOnlyList<CatalogProductUnit>>>(new Dictionary<Guid, IReadOnlyList<CatalogProductUnit>>());

        public Task AddAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task UpdateAsync(CatalogProductUnit unit, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task ReplaceActiveUnitsAsync(PosOrganizationId organizationId, CatalogProductId productId, ProductUnitKind kind, IReadOnlyList<CatalogProductUnit> units, DateTimeOffset utcNow, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
