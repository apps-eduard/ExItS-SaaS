using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.GlobalCatalog;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Common;
using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.UnitTests.GlobalCatalog;

public sealed class DynamicBusinessTypeTests
{
    private static readonly DateTimeOffset T0 = new(2026, 8, 10, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Philippine_default_business_types_are_sixteen_unique_active_codes()
    {
        Assert.Equal(16, PhilippineBusinessTypeSeeds.All.Count);
        Assert.Equal(16, PhilippineBusinessTypeSeeds.All.Select(r => r.Code).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(16, PhilippineBusinessTypeSeeds.All.Select(r => r.Id).Distinct().Count());
        Assert.Contains(PhilippineBusinessTypeSeeds.All, r => r.Code == "SariSari" && r.Name == "Sari-Sari Store");
        Assert.Contains(PhilippineBusinessTypeSeeds.All, r => r.Code == "Cafe" && r.Name == "Cafe / Coffee Shop");
        Assert.Contains(PhilippineBusinessTypeSeeds.All, r => r.Code == "GeneralRetail" && r.Name == "General Retail / Other");
        Assert.Contains(PhilippineBusinessTypeSeeds.All, r => r.Code == PhilippineBusinessTypeSeeds.VegetableVendorCode);
        Assert.Contains(PhilippineBusinessTypeSeeds.All, r => r.Code == PhilippineBusinessTypeSeeds.WaterRefillingCode);
        Assert.True(PhilippineBusinessTypeSeeds.TryGetIdByCode("fishvendor", out var fishId));
        Assert.Equal(PhilippineBusinessTypeSeeds.FishVendorId, fishId);
    }

    [Fact]
    public void Legacy_seeds_preserve_six_codes_and_stable_ids()
    {
        Assert.Equal(6, LegacyBusinessTypeSeeds.All.Count);
        Assert.Contains(LegacyBusinessTypeSeeds.All, r => r.Code == "SariSari" && r.Id == LegacyBusinessTypeSeeds.SariSariId);
        Assert.Contains(LegacyBusinessTypeSeeds.All, r => r.Code == "MiniGrocery");
        Assert.Contains(LegacyBusinessTypeSeeds.All, r => r.Code == "Bakery");
        Assert.Contains(LegacyBusinessTypeSeeds.All, r => r.Code == "Cafe");
        Assert.Contains(LegacyBusinessTypeSeeds.All, r => r.Code == "Pharmacy");
        Assert.Contains(LegacyBusinessTypeSeeds.All, r => r.Code == "GeneralRetail");
        Assert.True(LegacyBusinessTypeSeeds.TryGetIdByCode("bakery", out var bakeryId));
        Assert.Equal(LegacyBusinessTypeSeeds.BakeryId, bakeryId);
    }

    [Fact]
    public void Create_normalizes_code_and_defaults_active()
    {
        var type = BusinessType.Create("  SpecialtyShop ", " Specialty Shop ", T0, description: " Niche ", sortOrder: 70);
        Assert.Equal("SpecialtyShop", type.Code);
        Assert.Equal("Specialty Shop", type.Name);
        Assert.Equal("Niche", type.Description);
        Assert.Equal(BusinessTypeStatus.Active, type.Status);
        Assert.Equal(70, type.SortOrder);
    }

    [Fact]
    public void Archive_and_reactivate_lifecycle()
    {
        var type = BusinessType.Create("Kiosk", "Kiosk", T0);
        type.SetStatus(BusinessTypeStatus.Archived, T0.AddMinutes(1));
        Assert.Equal(BusinessTypeStatus.Archived, type.Status);
        type.SetStatus(BusinessTypeStatus.Active, T0.AddMinutes(2));
        Assert.Equal(BusinessTypeStatus.Active, type.Status);
    }

    [Fact]
    public void Invalid_code_is_rejected()
    {
        var ex = Assert.Throws<DomainException>(() => BusinessType.Create("bad code", "Bad", T0));
        Assert.Equal(DomainErrorCodes.InvalidGlobalCatalogBusinessType, ex.ErrorCode);
    }

    [Fact]
    public async Task Crud_rejects_duplicate_code_and_name()
    {
        var repo = new FakeBusinessTypeRepository();
        var create = new CreateBusinessType(repo, new BtFakeUnitOfWork(), new BtFixedClock(T0));

        var dupCode = await create.ExecuteAsync(new CreateBusinessTypeRequest("SariSari", "Another Name"));
        Assert.False(dupCode.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateBusinessTypeCode, dupCode.ErrorCode);

        var dupName = await create.ExecuteAsync(new CreateBusinessTypeRequest("UniqueCode", "Sari-Sari Store"));
        Assert.False(dupName.IsSuccess);
        Assert.Equal(ApplicationErrorCodes.DuplicateBusinessTypeName, dupName.ErrorCode);
    }

    [Fact]
    public async Task List_active_only_excludes_archived()
    {
        var repo = new FakeBusinessTypeRepository();
        var bakery = (await repo.GetByIdAsync(BusinessTypeId.From(LegacyBusinessTypeSeeds.BakeryId)))!;
        bakery.SetStatus(BusinessTypeStatus.Archived, T0.AddMinutes(1));
        await repo.UpdateAsync(bakery);

        var query = new BusinessTypeQueryService(repo);
        var active = await query.ListAsync(BusinessTypeStatus.Active, search: null, page: 1, pageSize: 50);
        Assert.DoesNotContain(active.Items, i => i.Code == LegacyBusinessTypeSeeds.BakeryCode);
        Assert.Contains(active.Items, i => i.Code == LegacyBusinessTypeSeeds.SariSariCode);

        var all = await query.ListAsync(status: null, search: null, page: 1, pageSize: 50);
        Assert.Contains(all.Items, i => i.Code == LegacyBusinessTypeSeeds.BakeryCode && i.Status == nameof(BusinessTypeStatus.Archived));
    }

    [Fact]
    public void Category_add_remove_replace_are_idempotent()
    {
        var bakery = BusinessTypeId.From(LegacyBusinessTypeSeeds.BakeryId);
        var cafe = BusinessTypeId.From(LegacyBusinessTypeSeeds.CafeId);
        var category = GlobalCategory.Create("Bread", T0, businessTypeIds: [bakery]);

        category.AddBusinessTypes([bakery, cafe], T0.AddMinutes(1));
        Assert.Equal(2, category.BusinessTypeIds.Count);
        category.AddBusinessTypes([cafe], T0.AddMinutes(2));
        Assert.Equal(2, category.BusinessTypeIds.Count);

        category.RemoveBusinessTypes([bakery], T0.AddMinutes(3));
        Assert.Equal([cafe], category.BusinessTypeIds);
        category.RemoveBusinessTypes([bakery], T0.AddMinutes(4));
        Assert.Equal([cafe], category.BusinessTypeIds);

        category.AssignBusinessTypes([bakery], T0.AddMinutes(5));
        Assert.Equal([bakery], category.BusinessTypeIds);
    }

    [Fact]
    public async Task Resolver_maps_code_and_name_and_rejects_unknown()
    {
        var repo = new FakeBusinessTypeRepository();

        var byCode = await BusinessTypeResolver.ResolveManyAsync(repo, ["bakery", "Cafe"], ids: null);
        Assert.Equal(2, byCode.Count);

        var byName = await BusinessTypeResolver.ResolveManyAsync(repo, ["Mini Grocery"], ids: null);
        Assert.Equal(LegacyBusinessTypeSeeds.MiniGroceryId, byName[0].Value);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => BusinessTypeResolver.ResolveManyAsync(repo, ["NotAType"], ids: null));
        Assert.Equal(DomainErrorCodes.InvalidGlobalCatalogBusinessType, ex.ErrorCode);
    }

    [Fact]
    public void Multiple_templates_can_share_same_primary_business_type()
    {
        var bakery = BusinessTypeId.From(LegacyBusinessTypeSeeds.BakeryId);
        var a = CatalogTemplate.Create("Starter Bakery", bakery, T0);
        var b = CatalogTemplate.Create("Standard Bakery", bakery, T0.AddMinutes(1));
        Assert.Equal(bakery, a.PrimaryBusinessTypeId);
        Assert.Equal(bakery, b.PrimaryBusinessTypeId);
        Assert.NotEqual(a.Id, b.Id);
    }

    [Fact]
    public void Product_applicability_uses_business_type_ids()
    {
        var category = GlobalCategory.Create("Pastries", T0);
        var product = GlobalProduct.Create(
            "Croissant",
            ProductUnit.Piece,
            "SKU-C",
            "4809000000005",
            "BrandX",
            category.Id,
            T0,
            10m,
            15m,
            businessTypeIds: [BusinessTypeId.From(LegacyBusinessTypeSeeds.BakeryId)]);
        Assert.Single(product.BusinessTypeIds);
        Assert.Equal(LegacyBusinessTypeSeeds.BakeryId, product.BusinessTypeIds[0].Value);
    }
}

file sealed class BtFixedClock(DateTimeOffset utcNow) : IClock
{
    public DateTimeOffset UtcNow { get; } = utcNow;
}

file sealed class BtFakeUnitOfWork : IPlatformUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
