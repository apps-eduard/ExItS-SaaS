using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class CatalogProductGovernancePersistenceTests(PosPostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-08-31T15:00:00Z");

    [Fact]
    public async Task PGDF_DB_01_OrganizationStandard_persists_and_reloads()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var orgId = PosOrganizationId.From(Guid.NewGuid());
        var product = CatalogProduct.Create(orgId, "Standard Coke", UnitOfMeasure.Piece, 50m, Now);

        await using (var db = new PosDbContext(options))
        {
            var repo = new CatalogProductRepository(db);
            await repo.AddAsync(product);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var repo = new CatalogProductRepository(db);
            var loaded = await repo.GetByIdAsync(orgId, product.Id);
            Assert.NotNull(loaded);
            Assert.Equal(CatalogProductScope.OrganizationStandard, loaded.Scope);
            Assert.Null(loaded.OriginBranchId);
            Assert.Equal(50m, loaded.SellingPrice);
        }
    }

    [Fact]
    public async Task PGDF_DB_02_BranchLocal_with_origin_persists_and_reloads()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var orgId = PosOrganizationId.From(Guid.NewGuid());
        var branchId = PosBranchId.From(Guid.NewGuid());
        var product = CatalogProduct.Create(
            orgId,
            "Local Bangus",
            UnitOfMeasure.Kilogram,
            180m,
            Now,
            scope: CatalogProductScope.BranchLocal,
            originBranchId: branchId);

        await using (var db = new PosDbContext(options))
        {
            var repo = new CatalogProductRepository(db);
            await repo.AddAsync(product);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var repo = new CatalogProductRepository(db);
            var loaded = await repo.GetByIdAsync(orgId, product.Id);
            Assert.NotNull(loaded);
            Assert.Equal(CatalogProductScope.BranchLocal, loaded.Scope);
            Assert.Equal(branchId, loaded.OriginBranchId);
        }
    }

    [Fact]
    public async Task PGDF_DB_03_to_06_BranchProductAvailability_key_and_multiplicity()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var orgId = PosOrganizationId.From(Guid.NewGuid());
        var branchA = PosBranchId.From(Guid.NewGuid());
        var branchB = PosBranchId.From(Guid.NewGuid());
        var p1 = CatalogProduct.Create(orgId, "Coke", UnitOfMeasure.Piece, 50m, Now);
        var p2 = CatalogProduct.Create(orgId, "Rice", UnitOfMeasure.Kilogram, 340m, Now);

        await using (var db = new PosDbContext(options))
        {
            var products = new CatalogProductRepository(db);
            await products.AddAsync(p1);
            await products.AddAsync(p2);
            await db.SaveChangesAsync();
        }

        var a1 = BranchProductAvailability.Create(orgId, branchA, p1.Id, isOffered: false, Now);
        var a2 = BranchProductAvailability.Create(orgId, branchB, p1.Id, isOffered: true, Now);
        var a3 = BranchProductAvailability.Create(orgId, branchA, p2.Id, isOffered: false, Now);

        await using (var db = new PosDbContext(options))
        {
            var repo = new BranchProductAvailabilityRepository(db);
            await repo.AddAsync(a1);
            await repo.AddAsync(a2);
            await repo.AddAsync(a3);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            var repo = new BranchProductAvailabilityRepository(db);
            var byBranch = await repo.ListByBranchAsync(orgId, branchA);
            Assert.Equal(2, byBranch.Count);

            var byProducts = await repo.ListByProductIdsAsync(orgId, branchA, [p1.Id, p2.Id]);
            Assert.Equal(2, byProducts.Count);

            var one = await repo.GetAsync(orgId, branchB, p1.Id);
            Assert.NotNull(one);
            Assert.True(one.IsOffered);
        }

        // PGDF-DB-04 duplicate key rejected
        await using (var db = new PosDbContext(options))
        {
            var repo = new BranchProductAvailabilityRepository(db);
            var dup = BranchProductAvailability.Create(orgId, branchA, p1.Id, isOffered: true, Now.AddMinutes(1));
            await repo.AddAsync(dup);
            await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task PGDF_DB_07_availability_FK_rejects_unknown_product()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var orgId = PosOrganizationId.From(Guid.NewGuid());
        var branchId = PosBranchId.From(Guid.NewGuid());
        var missingProduct = CatalogProductId.New();

        await using var db = new PosDbContext(options);
        var repo = new BranchProductAvailabilityRepository(db);
        await repo.AddAsync(
            BranchProductAvailability.Create(orgId, branchId, missingProduct, false, Now));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Fact]
    public async Task PGDF_DB_08_availability_uses_xmin_concurrency()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var orgId = PosOrganizationId.From(Guid.NewGuid());
        var branchId = PosBranchId.From(Guid.NewGuid());
        var product = CatalogProduct.Create(orgId, "Concurrency Product", UnitOfMeasure.Piece, 1m, Now);

        await using (var db = new PosDbContext(options))
        {
            await new CatalogProductRepository(db).AddAsync(product);
            await db.SaveChangesAsync();
        }

        await using (var db = new PosDbContext(options))
        {
            await new BranchProductAvailabilityRepository(db).AddAsync(
                BranchProductAvailability.Create(orgId, branchId, product.Id, true, Now));
            await db.SaveChangesAsync();
        }

        await using var db1 = new PosDbContext(options);
        await using var db2 = new PosDbContext(options);
        var r1 = await db1.BranchProductAvailabilities
            .FirstAsync(a => a.OrganizationId == orgId.Value && a.ProductId == product.Id.Value);
        var r2 = await db2.BranchProductAvailabilities
            .FirstAsync(a => a.OrganizationId == orgId.Value && a.ProductId == product.Id.Value);

        r1.IsOffered = false;
        r1.UpdatedAtUtc = Now.AddMinutes(1);
        await db1.SaveChangesAsync();

        r2.IsOffered = false;
        r2.UpdatedAtUtc = Now.AddMinutes(2);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
    }

    [Fact]
    public async Task PGDF_DB_BranchLocal_null_origin_rejected_at_database()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.products (
                id, organization_id, name, normalized_name, unit_of_measure, selling_mode, selling_price, status,
                catalog_source, scope, origin_branch_id, created_at_utc, updated_at_utc)
            VALUES (
                @id, @org, 'Bad Local', 'BAD LOCAL', 'Piece', 'PerItem', 1.00, 'Active',
                'Manual', 'BranchLocal', NULL, @now, @now);
            """,
            connection);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("org", Guid.NewGuid());
        cmd.Parameters.AddWithValue("now", Now);
        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
    }

    [Fact]
    public async Task PGDF_DB_invalid_scope_rejected_at_database()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            """
            INSERT INTO pos.products (
                id, organization_id, name, normalized_name, unit_of_measure, selling_mode, selling_price, status,
                catalog_source, scope, created_at_utc, updated_at_utc)
            VALUES (
                @id, @org, 'Bad Scope', 'BAD SCOPE', 'Piece', 'PerItem', 1.00, 'Active',
                'Manual', 'Global', @now, @now);
            """,
            connection);
        cmd.Parameters.AddWithValue("id", Guid.NewGuid());
        cmd.Parameters.AddWithValue("org", Guid.NewGuid());
        cmd.Parameters.AddWithValue("now", Now);
        var ex = await Assert.ThrowsAsync<PostgresException>(() => cmd.ExecuteNonQueryAsync());
        Assert.Equal("23514", ex.SqlState);
    }

    private DbContextOptions<PosDbContext> CreateOptions() =>
        new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;

    private static async Task MigrateAsync(DbContextOptions<PosDbContext> options)
    {
        await using var db = new PosDbContext(options);
        await db.Database.MigrateAsync();
    }
}
