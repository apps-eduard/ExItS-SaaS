using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

/// <summary>
/// MB2-01D: real PostgreSQL proof that catalog membership filters apply before Count/Skip/Take.
/// Closes deferred PGA-HARD-PAGE P2 from MB2-01B-H1.
/// </summary>
[Collection(PosPostgreSqlCollection.Name)]
public sealed class CatalogProductListPaginationPersistenceTests(PosPostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T06:00:00Z");

    [Fact]
    public async Task PGA_HARD_PAGE_01_commercial_first_page_returns_pageSize_when_enough_valid()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var org = PosOrganizationId.From(Guid.NewGuid());
        var branchA = PosBranchId.From(Guid.NewGuid());
        var branchB = PosBranchId.From(Guid.NewGuid());

        // 12 Standards offered at A by default + 3 noise rows that must not count toward commercial A.
        for (var i = 1; i <= 12; i++)
        {
            await SaveProductAsync(options, CatalogProduct.Create(org, $"Std{i:D2}", UnitOfMeasure.Piece, 10m + i, Now));
        }

        var notOffered = CatalogProduct.Create(org, "StdNotOffered", UnitOfMeasure.Piece, 99m, Now);
        await SaveProductAsync(options, notOffered);
        await SaveAvailabilityAsync(options, BranchProductAvailability.Create(org, branchA, notOffered.Id, isOffered: false, Now));

        await SaveProductAsync(
            options,
            CatalogProduct.Create(
                org, "LocalBOnly", UnitOfMeasure.Piece, 5m, Now,
                scope: CatalogProductScope.BranchLocal, originBranchId: branchB));

        await SaveProductAsync(
            options,
            CatalogProduct.Create(
                org, "LocalA", UnitOfMeasure.Piece, 5m, Now,
                scope: CatalogProductScope.BranchLocal, originBranchId: branchA));

        var filter = CommercialFilter(branchA);
        await using var db = new PosDbContext(options);
        var repo = new CatalogProductRepository(db);
        var (page1, total) = await repo.ListAsync(org, filter, skip: 0, take: 10);

        Assert.Equal(13, total); // 12 Std + LocalA; not StdNotOffered / LocalB
        Assert.Equal(10, page1.Count);
        Assert.DoesNotContain(page1, p => p.Name == "StdNotOffered");
        Assert.DoesNotContain(page1, p => p.Name == "LocalBOnly");
    }

    [Fact]
    public async Task PGA_HARD_PAGE_02_TotalCount_is_full_filtered_membership_across_pages()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var org = PosOrganizationId.From(Guid.NewGuid());
        var branchA = PosBranchId.From(Guid.NewGuid());

        for (var i = 1; i <= 37; i++)
        {
            await SaveProductAsync(options, CatalogProduct.Create(org, $"Match{i:D2}", UnitOfMeasure.Piece, i, Now));
        }

        // Noise outside commercial filter
        await SaveProductAsync(
            options,
            CatalogProduct.Create(
                org, "ForeignLocal", UnitOfMeasure.Piece, 1m, Now,
                scope: CatalogProductScope.BranchLocal,
                originBranchId: PosBranchId.From(Guid.NewGuid())));

        var filter = CommercialFilter(branchA);
        await using var db = new PosDbContext(options);
        var repo = new CatalogProductRepository(db);

        var (page1, total1) = await repo.ListAsync(org, filter, skip: 0, take: 20);
        var (page2, total2) = await repo.ListAsync(org, filter, skip: 20, take: 20);

        Assert.Equal(37, total1);
        Assert.Equal(37, total2);
        Assert.Equal(20, page1.Count);
        Assert.Equal(17, page2.Count);
    }

    [Fact]
    public async Task PGA_HARD_PAGE_03_no_skip_or_duplicate_between_pages()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var org = PosOrganizationId.From(Guid.NewGuid());
        var branchA = PosBranchId.From(Guid.NewGuid());

        for (var i = 1; i <= 25; i++)
        {
            await SaveProductAsync(options, CatalogProduct.Create(org, $"Item{i:D2}", UnitOfMeasure.Piece, i, Now));
        }

        var filter = CommercialFilter(branchA);
        await using var db = new PosDbContext(options);
        var repo = new CatalogProductRepository(db);

        var (page1, total) = await repo.ListAsync(org, filter, skip: 0, take: 10);
        var (page2, _) = await repo.ListAsync(org, filter, skip: 10, take: 10);
        var (page3, _) = await repo.ListAsync(org, filter, skip: 20, take: 10);

        var ids = page1.Concat(page2).Concat(page3).Select(p => p.Id.Value).ToList();
        Assert.Equal(25, total);
        Assert.Equal(25, ids.Count);
        Assert.Equal(25, ids.Distinct().Count());
    }

    [Fact]
    public async Task PGA_HARD_PAGE_04_branch_management_excludes_foreign_Local_before_Count_Skip_Take()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var org = PosOrganizationId.From(Guid.NewGuid());
        var branchA = PosBranchId.From(Guid.NewGuid());
        var branchB = PosBranchId.From(Guid.NewGuid());

        for (var i = 1; i <= 5; i++)
        {
            await SaveProductAsync(options, CatalogProduct.Create(org, $"Std{i:D2}", UnitOfMeasure.Piece, i, Now));
        }

        for (var i = 1; i <= 8; i++)
        {
            await SaveProductAsync(
                options,
                CatalogProduct.Create(
                    org, $"LocalA{i:D2}", UnitOfMeasure.Piece, i, Now,
                    scope: CatalogProductScope.BranchLocal, originBranchId: branchA));
        }

        for (var i = 1; i <= 20; i++)
        {
            await SaveProductAsync(
                options,
                CatalogProduct.Create(
                    org, $"LocalB{i:D2}", UnitOfMeasure.Piece, i, Now,
                    scope: CatalogProductScope.BranchLocal, originBranchId: branchB));
        }

        var filter = new CatalogProductFilter(
            RestrictBranchLocalToActingBranch: true,
            ActingBranchId: branchA.Value);

        await using var db = new PosDbContext(options);
        var repo = new CatalogProductRepository(db);
        var (page1, total) = await repo.ListAsync(org, filter, skip: 0, take: 10);

        Assert.Equal(13, total); // 5 Std + 8 LocalA; LocalB excluded before count
        Assert.Equal(10, page1.Count);
        Assert.DoesNotContain(page1, p => p.Name.StartsWith("LocalB", StringComparison.Ordinal));
        Assert.All(
            page1.Where(p => p.Scope == CatalogProductScope.BranchLocal),
            p => Assert.Equal(branchA, p.OriginBranchId));
    }

    [Fact]
    public async Task PGA_HARD_PAGE_05_Owner_management_includes_all_BranchLocal_with_correct_count()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var org = PosOrganizationId.From(Guid.NewGuid());
        var branchA = PosBranchId.From(Guid.NewGuid());
        var branchB = PosBranchId.From(Guid.NewGuid());

        await SaveProductAsync(options, CatalogProduct.Create(org, "Std01", UnitOfMeasure.Piece, 1m, Now));
        await SaveProductAsync(
            options,
            CatalogProduct.Create(
                org, "LocalA01", UnitOfMeasure.Piece, 1m, Now,
                scope: CatalogProductScope.BranchLocal, originBranchId: branchA));
        await SaveProductAsync(
            options,
            CatalogProduct.Create(
                org, "LocalB01", UnitOfMeasure.Piece, 1m, Now,
                scope: CatalogProductScope.BranchLocal, originBranchId: branchB));

        // Owner/Admin: RestrictBranchLocalToActingBranch = false
        var filter = new CatalogProductFilter();
        await using var db = new PosDbContext(options);
        var repo = new CatalogProductRepository(db);
        var (page, total) = await repo.ListAsync(org, filter, skip: 0, take: 10);

        Assert.Equal(3, total);
        Assert.Equal(3, page.Count);
        Assert.Contains(page, p => p.Name == "LocalA01");
        Assert.Contains(page, p => p.Name == "LocalB01");
    }

    [Fact]
    public async Task PGA_HARD_PAGE_06_scope_and_status_filters_compose_before_pagination()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var org = PosOrganizationId.From(Guid.NewGuid());
        var branchA = PosBranchId.From(Guid.NewGuid());

        for (var i = 1; i <= 15; i++)
        {
            await SaveProductAsync(options, CatalogProduct.Create(org, $"StdActive{i:D2}", UnitOfMeasure.Piece, i, Now));
        }

        var inactive = CatalogProduct.Create(org, "StdInactive01", UnitOfMeasure.Piece, 1m, Now);
        inactive.Deactivate(Now);
        await SaveProductAsync(options, inactive);

        for (var i = 1; i <= 7; i++)
        {
            await SaveProductAsync(
                options,
                CatalogProduct.Create(
                    org, $"LocalActive{i:D2}", UnitOfMeasure.Piece, i, Now,
                    scope: CatalogProductScope.BranchLocal, originBranchId: branchA));
        }

        var filter = new CatalogProductFilter(
            Status: CatalogProductStatus.Active,
            Scope: CatalogProductScope.OrganizationStandard);

        await using var db = new PosDbContext(options);
        var repo = new CatalogProductRepository(db);
        var (page1, total) = await repo.ListAsync(org, filter, skip: 0, take: 10);
        var (page2, total2) = await repo.ListAsync(org, filter, skip: 10, take: 10);

        Assert.Equal(15, total);
        Assert.Equal(15, total2);
        Assert.Equal(10, page1.Count);
        Assert.Equal(5, page2.Count);
        Assert.All(page1.Concat(page2), p =>
        {
            Assert.Equal(CatalogProductScope.OrganizationStandard, p.Scope);
            Assert.Equal(CatalogProductStatus.Active, p.Status);
        });
        Assert.DoesNotContain(page1.Concat(page2), p => p.Name.StartsWith("Local", StringComparison.Ordinal));
        Assert.DoesNotContain(page1.Concat(page2), p => p.Name.StartsWith("StdInactive", StringComparison.Ordinal));
    }

    private static CatalogProductFilter CommercialFilter(PosBranchId branch) =>
        new(
            CommerciallyOfferedAtBranch: true,
            ActingBranchId: branch.Value);

    private DbContextOptions<PosDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

    private static async Task MigrateAsync(DbContextOptions<PosDbContext> options)
    {
        await using var db = new PosDbContext(options);
        await db.Database.MigrateAsync();
    }

    private static async Task SaveProductAsync(DbContextOptions<PosDbContext> options, CatalogProduct product)
    {
        await using var db = new PosDbContext(options);
        await new CatalogProductRepository(db).AddAsync(product);
        await db.SaveChangesAsync();
    }

    private static async Task SaveAvailabilityAsync(
        DbContextOptions<PosDbContext> options,
        BranchProductAvailability availability)
    {
        await using var db = new PosDbContext(options);
        await new BranchProductAvailabilityRepository(db).AddAsync(availability);
        await db.SaveChangesAsync();
    }
}
