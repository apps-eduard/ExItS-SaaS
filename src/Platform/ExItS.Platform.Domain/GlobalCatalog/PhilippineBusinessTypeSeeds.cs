namespace ExItS.Platform.Domain.GlobalCatalog;

/// <summary>
/// WP10A Philippine Pinoy Business POS default Business Types.
/// Legacy six keep stable GUIDs from <see cref="LegacyBusinessTypeSeeds"/>; ten additional types use stable GUIDs.
/// Codes are immutable; display names may be refreshed by Ensure.
/// </summary>
public static class PhilippineBusinessTypeSeeds
{
    public static readonly Guid VegetableVendorId = Guid.Parse("a1000001-0000-4000-8000-000000000007");
    public static readonly Guid FruitVendorId = Guid.Parse("a1000001-0000-4000-8000-000000000008");
    public static readonly Guid FishVendorId = Guid.Parse("a1000001-0000-4000-8000-000000000009");
    public static readonly Guid MeatVendorId = Guid.Parse("a1000001-0000-4000-8000-00000000000a");
    public static readonly Guid RiceRetailerId = Guid.Parse("a1000001-0000-4000-8000-00000000000b");
    public static readonly Guid FrozenGoodsId = Guid.Parse("a1000001-0000-4000-8000-00000000000c");
    public static readonly Guid CarinderiaId = Guid.Parse("a1000001-0000-4000-8000-00000000000d");
    public static readonly Guid StreetFoodVendorId = Guid.Parse("a1000001-0000-4000-8000-00000000000e");
    public static readonly Guid FoodCartId = Guid.Parse("a1000001-0000-4000-8000-00000000000f");
    public static readonly Guid WaterRefillingId = Guid.Parse("a1000001-0000-4000-8000-000000000010");

    public const string VegetableVendorCode = "VegetableVendor";
    public const string FruitVendorCode = "FruitVendor";
    public const string FishVendorCode = "FishVendor";
    public const string MeatVendorCode = "MeatVendor";
    public const string RiceRetailerCode = "RiceRetailer";
    public const string FrozenGoodsCode = "FrozenGoods";
    public const string CarinderiaCode = "Carinderia";
    public const string StreetFoodVendorCode = "StreetFoodVendor";
    public const string FoodCartCode = "FoodCart";
    public const string WaterRefillingCode = "WaterRefilling";

    /// <summary>Ten additive Philippine vendor/food types (not in the original migration InsertData).</summary>
    public static IReadOnlyList<(Guid Id, string Code, string Name, int SortOrder)> Additional { get; } =
    [
        (VegetableVendorId, VegetableVendorCode, "Vegetable Vendor", 70),
        (FruitVendorId, FruitVendorCode, "Fruit Vendor", 80),
        (FishVendorId, FishVendorCode, "Fish Vendor", 90),
        (MeatVendorId, MeatVendorCode, "Meat Vendor", 100),
        (RiceRetailerId, RiceRetailerCode, "Rice Retailer", 110),
        (FrozenGoodsId, FrozenGoodsCode, "Frozen Goods", 120),
        (CarinderiaId, CarinderiaCode, "Carinderia / Eatery", 130),
        (StreetFoodVendorId, StreetFoodVendorCode, "Street Food Vendor", 140),
        (FoodCartId, FoodCartCode, "Food Cart", 150),
        (WaterRefillingId, WaterRefillingCode, "Water Refilling Station", 160)
    ];

    /// <summary>All 16 default Philippine POS Business Types (legacy + additional).</summary>
    public static IReadOnlyList<(Guid Id, string Code, string Name, int SortOrder)> All { get; } =
        LegacyBusinessTypeSeeds.All.Concat(Additional).ToArray();

    public static bool TryGetIdByCode(string code, out Guid id)
    {
        foreach (var row in All)
        {
            if (string.Equals(row.Code, code, StringComparison.OrdinalIgnoreCase))
            {
                id = row.Id;
                return true;
            }
        }

        id = Guid.Empty;
        return false;
    }
}
