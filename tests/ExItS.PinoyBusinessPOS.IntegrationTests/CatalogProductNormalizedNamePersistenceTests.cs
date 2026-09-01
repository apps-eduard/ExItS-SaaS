using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class CatalogProductNormalizedNamePersistenceTests(PosPostgreSqlFixture fixture)
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T03:00:00Z");

    [Fact]
    public async Task PNAME_DB_01_to_06_org_normalized_name_unique_across_status_and_scope()
    {
        var options = CreateOptions();
        await MigrateAsync(options);

        var orgId = PosOrganizationId.From(Guid.NewGuid());
        var branchA = PosBranchId.From(Guid.NewGuid());
        var branchB = PosBranchId.From(Guid.NewGuid());

        var standard = CatalogProduct.Create(orgId, "Coke 1L", UnitOfMeasure.Piece, 50m, Now);
        await SaveAsync(options, standard);

        // Case duplicate
        await AssertUniqueViolationAsync(options, CatalogProduct.Create(orgId, "coke 1l", UnitOfMeasure.Piece, 51m, Now));

        // Whitespace duplicate
        await AssertUniqueViolationAsync(options, CatalogProduct.Create(orgId, "Coke   1L", UnitOfMeasure.Piece, 52m, Now));

        // Standard vs Local
        await AssertUniqueViolationAsync(
            options,
            CatalogProduct.Create(
                orgId, "COKE 1L", UnitOfMeasure.Piece, 53m, Now,
                scope: CatalogProductScope.BranchLocal, originBranchId: branchA));

        // Inactive still reserves
        standard.Deactivate(Now);
        await using (var db = new PosDbContext(options))
        {
            await new CatalogProductRepository(db).UpdateAsync(standard);
            await db.SaveChangesAsync();
        }

        await AssertUniqueViolationAsync(options, CatalogProduct.Create(orgId, "Coke 1L", UnitOfMeasure.Piece, 54m, Now));

        // Cross-branch Local duplicate blocked (via second org product path)
        var localA = CatalogProduct.Create(
            orgId, "Fresh Bangus", UnitOfMeasure.Kilogram, 180m, Now,
            scope: CatalogProductScope.BranchLocal, originBranchId: branchA);
        await SaveAsync(options, localA);
        await AssertUniqueViolationAsync(
            options,
            CatalogProduct.Create(
                orgId, " fresh   bangus ", UnitOfMeasure.Kilogram, 190m, Now,
                scope: CatalogProductScope.BranchLocal, originBranchId: branchB));

        // Different organization allowed
        var org2 = PosOrganizationId.From(Guid.NewGuid());
        await SaveAsync(options, CatalogProduct.Create(org2, "Coke 1L", UnitOfMeasure.Piece, 50m, Now));
    }

    [Fact]
    public async Task PNAME_DB_07_08_unique_violation_maps_to_stable_application_conflict()
    {
        var options = CreateOptions();
        await MigrateAsync(options);
        var orgId = PosOrganizationId.From(Guid.NewGuid());
        await SaveAsync(options, CatalogProduct.Create(orgId, "Coke 1L", UnitOfMeasure.Piece, 50m, Now));

        await using var db = new PosDbContext(options);
        var repo = new CatalogProductRepository(db);
        await repo.AddAsync(CatalogProduct.Create(orgId, "COKE 1L", UnitOfMeasure.Piece, 55m, Now));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(PersistenceExceptionMapper.TryMapUniqueViolation(ex, out var errorCode, out _));
        Assert.Equal(ApplicationErrorCodes.ProductNameConflict, errorCode);
    }

    [Fact]
    public async Task PNAME_DB_sql_normalizer_matches_csharp_for_canonical_examples()
    {
        await using var connection = new NpgsqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        foreach (var input in new[] { "Coke 1L", "coke 1l", " COKE 1L ", "Coke   1L", "Coke\t1L" })
        {
            var csharp = CatalogProduct.NormalizeProductName(input).Normalized;
            await using var cmd = new NpgsqlCommand(
                """
                SELECT upper(regexp_replace(btrim(normalize(@name, NFC)), E'\\s+', ' ', 'g'));
                """,
                connection);
            cmd.Parameters.AddWithValue("name", input);
            var sql = (string)(await cmd.ExecuteScalarAsync())!;
            Assert.Equal(csharp, sql);
            Assert.Equal("COKE 1L", sql);
        }
    }

    private async Task AssertUniqueViolationAsync(DbContextOptions<PosDbContext> options, CatalogProduct product)
    {
        await using var db = new PosDbContext(options);
        await new CatalogProductRepository(db).AddAsync(product);
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Contains("ux_products_org_normalized_name", ex.InnerException?.Message ?? ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SaveAsync(DbContextOptions<PosDbContext> options, CatalogProduct product)
    {
        await using var db = new PosDbContext(options);
        await new CatalogProductRepository(db).AddAsync(product);
        await db.SaveChangesAsync();
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
