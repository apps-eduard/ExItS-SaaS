using ExItS.Platform.Domain.GlobalCatalog;

namespace ExItS.Platform.Application.GlobalCatalog;

/// <summary>WP10A curated Philippine starter catalog definitions (idempotent Ensure input).</summary>
public static class PhilippinePosStarterCatalogData
{
    public const string GenericBrand = "Generic";
    public const string LocalProduceBrand = "Local Produce";

    public sealed record CategoryDef(string Name, int SortOrder, string[] BusinessTypeCodes);

    public sealed record ProductDef(
        string Sku,
        string Name,
        string CategoryName,
        ProductUnit Unit,
        ProductSellingMode SellingMode,
        decimal CostPrice,
        decimal SellingPrice,
        string Brand,
        string[] BusinessTypeCodes);

    public sealed record TemplateDef(
        string Name,
        string Slug,
        string PrimaryBusinessTypeCode,
        string Description,
        string[] ProductSkus);

    public static IReadOnlyList<CategoryDef> Categories { get; } =
    [
        new("Beverages", 10, [Bt.SariSari, Bt.MiniGrocery, Bt.Cafe, Bt.Bakery, Bt.Pharmacy, Bt.Carinderia, Bt.StreetFood, Bt.FoodCart, Bt.FrozenGoods, Bt.RiceRetailer, Bt.WaterRefilling, Bt.GeneralRetail]),
        new("Snacks", 20, [Bt.SariSari, Bt.MiniGrocery, Bt.Bakery, Bt.RiceRetailer, Bt.GeneralRetail, Bt.FoodCart]),
        new("Canned Goods", 30, [Bt.SariSari, Bt.MiniGrocery, Bt.RiceRetailer, Bt.GeneralRetail]),
        new("Condiments", 40, [Bt.SariSari, Bt.MiniGrocery, Bt.Carinderia, Bt.RiceRetailer, Bt.GeneralRetail]),
        new("Household Basics", 50, [Bt.SariSari, Bt.MiniGrocery, Bt.WaterRefilling, Bt.GeneralRetail]),
        new("Toiletries", 60, [Bt.SariSari, Bt.MiniGrocery, Bt.Pharmacy, Bt.WaterRefilling, Bt.GeneralRetail]),
        new("Fresh Vegetables", 70, [Bt.VegetableVendor, Bt.MiniGrocery, Bt.Carinderia]),
        new("Fresh Fruits", 80, [Bt.FruitVendor, Bt.MiniGrocery, Bt.StreetFood]),
        new("Fresh Fish", 90, [Bt.FishVendor, Bt.SariSari]),
        new("Fresh Meat", 100, [Bt.MeatVendor, Bt.Carinderia, Bt.FoodCart, Bt.FrozenGoods, Bt.SariSari]),
        new("Rice", 110, [Bt.RiceRetailer, Bt.SariSari, Bt.MiniGrocery, Bt.Carinderia]),
        new("Frozen Goods", 120, [Bt.FrozenGoods, Bt.MiniGrocery, Bt.FoodCart, Bt.SariSari]),
        new("Baked Goods", 130, [Bt.Bakery, Bt.Cafe, Bt.SariSari]),
        new("Coffee & Drinks", 140, [Bt.Cafe]),
        new("Prepared Meals", 150, [Bt.Carinderia, Bt.StreetFood, Bt.FoodCart]),
        new("Street Food", 160, [Bt.StreetFood, Bt.FoodCart]),
        new("Water Refill", 170, [Bt.WaterRefilling, Bt.SariSari]),
        new("Pharmacy Basics", 180, [Bt.Pharmacy, Bt.WaterRefilling, Bt.GeneralRetail, Bt.SariSari]),
        new("General Merchandise", 190, [Bt.GeneralRetail, Bt.MiniGrocery, Bt.SariSari, Bt.WaterRefilling])
    ];

    public static IReadOnlyList<ProductDef> Products { get; } =
    [
        // Shared beverages / staples
        P("PH-BEV-WATER-500", "Bottled Water 500ml", "Beverages", ProductUnit.Bottle, 8m, 12m, Bt.SariSari, Bt.MiniGrocery, Bt.Cafe, Bt.Bakery, Bt.Pharmacy, Bt.Carinderia, Bt.StreetFood, Bt.FoodCart, Bt.FrozenGoods, Bt.RiceRetailer, Bt.GeneralRetail, Bt.WaterRefilling),
        P("PH-BEV-SOFTDRINK-CAN", "Soft Drink Can", "Beverages", ProductUnit.Can, 18m, 25m, Bt.SariSari, Bt.MiniGrocery, Bt.Bakery, Bt.Carinderia, Bt.StreetFood, Bt.FoodCart, Bt.FrozenGoods, Bt.GeneralRetail),
        P("PH-BEV-SOFTDRINK-1L", "Soft Drink 1L", "Beverages", ProductUnit.Bottle, 35m, 48m, Bt.SariSari, Bt.MiniGrocery, Bt.GeneralRetail),
        P("PH-BEV-JUICE-BOX", "Juice Drink Box", "Beverages", ProductUnit.Piece, 12m, 18m, Bt.SariSari, Bt.MiniGrocery, Bt.Pharmacy, Bt.FoodCart, Bt.StreetFood, Bt.GeneralRetail),
        P("PH-SNK-BISCUIT-PACK", "Biscuit Pack", "Snacks", ProductUnit.Pack, 8m, 12m, Bt.SariSari, Bt.MiniGrocery, Bt.Bakery, Bt.RiceRetailer, Bt.GeneralRetail),
        P("PH-SNK-CHIPS-SMALL", "Potato Chips Small", "Snacks", ProductUnit.Pack, 10m, 15m, Bt.SariSari, Bt.MiniGrocery, Bt.FoodCart, Bt.GeneralRetail),
        P("PH-SNK-CANDY-PACK", "Hard Candy Pack", "Snacks", ProductUnit.Pack, 5m, 8m, Bt.SariSari, Bt.MiniGrocery, Bt.GeneralRetail),
        P("PH-CAN-SARDINES", "Canned Sardines", "Canned Goods", ProductUnit.Can, 22m, 32m, Bt.SariSari, Bt.MiniGrocery, Bt.RiceRetailer, Bt.GeneralRetail),
        P("PH-CAN-CORNEDBEEF", "Canned Corned Beef", "Canned Goods", ProductUnit.Can, 35m, 48m, Bt.SariSari, Bt.MiniGrocery, Bt.GeneralRetail),
        P("PH-CAN-MEATLOAF", "Canned Meat Loaf", "Canned Goods", ProductUnit.Can, 28m, 38m, Bt.SariSari, Bt.MiniGrocery, Bt.GeneralRetail),
        P("PH-NOODLE-INSTANT", "Instant Noodles Pack", "Snacks", ProductUnit.Pack, 10m, 15m, Bt.SariSari, Bt.MiniGrocery, Bt.RiceRetailer, Bt.GeneralRetail),
        P("PH-COFFEE-SACHET", "Instant Coffee Sachet", "Beverages", ProductUnit.Sachet, 6m, 10m, Bt.SariSari, Bt.MiniGrocery, Bt.Cafe, Bt.Bakery, Bt.GeneralRetail),
        P("PH-COND-SOYSAUCE", "Soy Sauce Bottle", "Condiments", ProductUnit.Bottle, 18m, 28m, Bt.SariSari, Bt.MiniGrocery, Bt.Carinderia, Bt.RiceRetailer, Bt.GeneralRetail),
        P("PH-COND-VINEGAR", "Vinegar Bottle", "Condiments", ProductUnit.Bottle, 15m, 22m, Bt.SariSari, Bt.MiniGrocery, Bt.Carinderia, Bt.GeneralRetail),
        P("PH-COND-FISHSAUCE", "Fish Sauce Bottle", "Condiments", ProductUnit.Bottle, 16m, 24m, Bt.SariSari, Bt.MiniGrocery, Bt.Carinderia, Bt.GeneralRetail),
        P("PH-HH-DETERGENT-SACHET", "Detergent Sachet", "Household Basics", ProductUnit.Sachet, 7m, 12m, Bt.SariSari, Bt.MiniGrocery, Bt.GeneralRetail),
        P("PH-HH-DISHWASH", "Dishwashing Liquid", "Household Basics", ProductUnit.Bottle, 25m, 38m, Bt.SariSari, Bt.MiniGrocery, Bt.WaterRefilling, Bt.GeneralRetail),
        P("PH-TOIL-SOAP-BAR", "Bath Soap Bar", "Toiletries", ProductUnit.Piece, 18m, 28m, Bt.SariSari, Bt.MiniGrocery, Bt.Pharmacy, Bt.WaterRefilling, Bt.GeneralRetail),
        P("PH-TOIL-TOOTHPASTE", "Toothpaste Tube", "Toiletries", ProductUnit.Piece, 35m, 52m, Bt.SariSari, Bt.MiniGrocery, Bt.Pharmacy, Bt.GeneralRetail),
        P("PH-TOIL-SHAMPOO-SACHET", "Shampoo Sachet", "Toiletries", ProductUnit.Sachet, 5m, 8m, Bt.SariSari, Bt.MiniGrocery, Bt.Pharmacy, Bt.GeneralRetail),

        // Vegetables (ByWeight kg)
        W("PH-VEG-TOMATO", "Tomato", "Fresh Vegetables", 80m, 120m, Bt.VegetableVendor, Bt.MiniGrocery, Bt.Carinderia),
        W("PH-VEG-ONION", "Onion", "Fresh Vegetables", 70m, 100m, Bt.VegetableVendor, Bt.MiniGrocery, Bt.Carinderia),
        W("PH-VEG-GARLIC", "Garlic", "Fresh Vegetables", 120m, 180m, Bt.VegetableVendor, Bt.MiniGrocery, Bt.Carinderia),
        W("PH-VEG-POTATO", "Potato", "Fresh Vegetables", 60m, 90m, Bt.VegetableVendor, Bt.MiniGrocery),
        W("PH-VEG-CARROT", "Carrot", "Fresh Vegetables", 70m, 110m, Bt.VegetableVendor, Bt.MiniGrocery),
        W("PH-VEG-PECHAY", "Pechay", "Fresh Vegetables", 40m, 70m, Bt.VegetableVendor, Bt.Carinderia),
        W("PH-VEG-CABBAGE", "Cabbage", "Fresh Vegetables", 45m, 75m, Bt.VegetableVendor, Bt.MiniGrocery, Bt.Carinderia),
        W("PH-VEG-EGGPLANT", "Eggplant", "Fresh Vegetables", 50m, 80m, Bt.VegetableVendor, Bt.Carinderia),
        W("PH-VEG-SQUASH", "Squash", "Fresh Vegetables", 35m, 55m, Bt.VegetableVendor, Bt.Carinderia),
        W("PH-VEG-OKRA", "Okra", "Fresh Vegetables", 55m, 85m, Bt.VegetableVendor),
        W("PH-VEG-SITAW", "Sitaw", "Fresh Vegetables", 50m, 80m, Bt.VegetableVendor),
        W("PH-VEG-KALABASA", "Kalabasa", "Fresh Vegetables", 35m, 55m, Bt.VegetableVendor),
        W("PH-VEG-AMPLALAYA", "Ampalaya", "Fresh Vegetables", 60m, 95m, Bt.VegetableVendor),
        W("PH-VEG-KANGKONG", "Kangkong", "Fresh Vegetables", 30m, 50m, Bt.VegetableVendor, Bt.Carinderia),
        W("PH-VEG-GINGER", "Ginger", "Fresh Vegetables", 100m, 150m, Bt.VegetableVendor, Bt.Carinderia),

        // Fruits
        W("PH-FRU-BANANA", "Banana Lakatan", "Fresh Fruits", 50m, 80m, Bt.FruitVendor, Bt.MiniGrocery),
        W("PH-FRU-MANGO", "Mango", "Fresh Fruits", 120m, 180m, Bt.FruitVendor),
        W("PH-FRU-PAPAYA", "Papaya", "Fresh Fruits", 40m, 65m, Bt.FruitVendor),
        W("PH-FRU-PINEAPPLE", "Pineapple", "Fresh Fruits", 45m, 70m, Bt.FruitVendor),
        W("PH-FRU-WATERMELON", "Watermelon", "Fresh Fruits", 30m, 50m, Bt.FruitVendor),
        W("PH-FRU-CALAMANSI", "Calamansi", "Fresh Fruits", 60m, 95m, Bt.FruitVendor, Bt.Carinderia),
        W("PH-FRU-APPLE", "Apple", "Fresh Fruits", 140m, 200m, Bt.FruitVendor, Bt.MiniGrocery),
        W("PH-FRU-ORANGE", "Orange", "Fresh Fruits", 100m, 150m, Bt.FruitVendor, Bt.MiniGrocery),
        W("PH-FRU-GRAPES", "Grapes", "Fresh Fruits", 180m, 260m, Bt.FruitVendor),
        W("PH-FRU-AVOCADO", "Avocado", "Fresh Fruits", 90m, 140m, Bt.FruitVendor),
        W("PH-FRU-GUAVA", "Guava", "Fresh Fruits", 55m, 85m, Bt.FruitVendor),
        W("PH-FRU-LANZONES", "Lanzones", "Fresh Fruits", 110m, 160m, Bt.FruitVendor),
        W("PH-FRU-RAMBUTAN", "Rambutan", "Fresh Fruits", 100m, 150m, Bt.FruitVendor),
        W("PH-FRU-DURIAN", "Durian", "Fresh Fruits", 200m, 300m, Bt.FruitVendor),
        W("PH-FRU-COCONUT", "Young Coconut", "Fresh Fruits", 25m, 40m, Bt.FruitVendor, Bt.StreetFood),

        // Fish
        W("PH-FISH-BANGUS", "Bangus", "Fresh Fish", 160m, 220m, Bt.FishVendor),
        W("PH-FISH-TILAPIA", "Tilapia", "Fresh Fish", 120m, 170m, Bt.FishVendor),
        W("PH-FISH-GALUNGGONG", "Galunggong", "Fresh Fish", 100m, 150m, Bt.FishVendor),
        W("PH-FISH-TUNA", "Tuna Cut", "Fresh Fish", 220m, 300m, Bt.FishVendor),
        W("PH-FISH-SQUID", "Squid", "Fresh Fish", 180m, 250m, Bt.FishVendor),
        W("PH-FISH-SHRIMP", "Shrimp", "Fresh Fish", 280m, 380m, Bt.FishVendor),
        W("PH-FISH-CRAB", "Crab", "Fresh Fish", 300m, 420m, Bt.FishVendor),
        W("PH-FISH-TANIGUE", "Tanigue", "Fresh Fish", 240m, 330m, Bt.FishVendor),
        W("PH-FISH-LAPULAPU", "Lapu-Lapu", "Fresh Fish", 350m, 480m, Bt.FishVendor),
        W("PH-FISH-SARDINES-FRESH", "Fresh Sardines", "Fresh Fish", 80m, 120m, Bt.FishVendor),
        W("PH-FISH-MACKEREL", "Mackerel", "Fresh Fish", 140m, 200m, Bt.FishVendor),
        W("PH-FISH-MUSSEL", "Mussels", "Fresh Fish", 90m, 140m, Bt.FishVendor),
        W("PH-FISH-CLAM", "Clams", "Fresh Fish", 85m, 130m, Bt.FishVendor),
        W("PH-FISH-MILKFISH-BELLY", "Bangus Belly", "Fresh Fish", 200m, 280m, Bt.FishVendor),
        W("PH-FISH-DRIED-FISH", "Dried Fish", "Fresh Fish", 150m, 220m, Bt.FishVendor, Bt.SariSari),

        // Meat
        W("PH-MEAT-PORK", "Pork", "Fresh Meat", 240m, 340m, Bt.MeatVendor),
        W("PH-MEAT-BEEF", "Beef", "Fresh Meat", 320m, 450m, Bt.MeatVendor),
        W("PH-MEAT-CHICKEN", "Chicken", "Fresh Meat", 160m, 220m, Bt.MeatVendor, Bt.Carinderia),
        W("PH-MEAT-PORK-BELLY", "Pork Belly", "Fresh Meat", 280m, 380m, Bt.MeatVendor),
        W("PH-MEAT-GROUND-PORK", "Ground Pork", "Fresh Meat", 220m, 300m, Bt.MeatVendor),
        W("PH-MEAT-CHICKEN-THIGH", "Chicken Thigh", "Fresh Meat", 170m, 240m, Bt.MeatVendor),
        W("PH-MEAT-CHICKEN-WING", "Chicken Wing", "Fresh Meat", 180m, 250m, Bt.MeatVendor),
        W("PH-MEAT-PORK-CHOP", "Pork Chop", "Fresh Meat", 250m, 350m, Bt.MeatVendor),
        W("PH-MEAT-BEEF-STEW", "Beef Stew Cut", "Fresh Meat", 300m, 420m, Bt.MeatVendor),
        W("PH-MEAT-LIVER", "Pork Liver", "Fresh Meat", 150m, 220m, Bt.MeatVendor),
        W("PH-MEAT-HOTDOG-BULK", "Hotdog Bulk", "Fresh Meat", 140m, 200m, Bt.MeatVendor, Bt.FoodCart, Bt.FrozenGoods),
        W("PH-MEAT-LONGANISA", "Longganisa", "Fresh Meat", 200m, 280m, Bt.MeatVendor, Bt.SariSari),
        W("PH-MEAT-TOCINO", "Tocino", "Fresh Meat", 190m, 270m, Bt.MeatVendor),
        W("PH-MEAT-BACON", "Bacon", "Fresh Meat", 260m, 360m, Bt.MeatVendor),
        W("PH-MEAT-RIBS", "Pork Ribs", "Fresh Meat", 270m, 370m, Bt.MeatVendor),

        // Rice
        W("PH-RICE-SINANDOMENG", "Rice Sinandomeng", "Rice", 45m, 58m, Bt.RiceRetailer, Bt.SariSari, Bt.MiniGrocery, Bt.Carinderia),
        W("PH-RICE-JASMINE", "Rice Jasmine", "Rice", 55m, 72m, Bt.RiceRetailer, Bt.MiniGrocery),
        W("PH-RICE-DINORADO", "Rice Dinorado", "Rice", 50m, 65m, Bt.RiceRetailer),
        W("PH-RICE-BROWN", "Brown Rice", "Rice", 60m, 80m, Bt.RiceRetailer, Bt.MiniGrocery),
        W("PH-RICE-GLUTINOUS", "Glutinous Rice", "Rice", 55m, 75m, Bt.RiceRetailer),
        P("PH-RICE-25KG-SACK", "Rice 25kg Sack", "Rice", ProductUnit.Pack, 1300m, 1550m, Bt.RiceRetailer, Bt.MiniGrocery),
        P("PH-RICE-5KG-PACK", "Rice 5kg Pack", "Rice", ProductUnit.Pack, 280m, 340m, Bt.RiceRetailer, Bt.SariSari, Bt.MiniGrocery),

        // Frozen
        W("PH-FRZ-CHICKEN", "Frozen Chicken", "Frozen Goods", 150m, 210m, Bt.FrozenGoods, Bt.MiniGrocery),
        W("PH-FRZ-FISH", "Frozen Fish Fillet", "Frozen Goods", 180m, 250m, Bt.FrozenGoods),
        W("PH-FRZ-VEG-MIX", "Frozen Mixed Vegetables", "Frozen Goods", 90m, 140m, Bt.FrozenGoods, Bt.MiniGrocery),
        P("PH-FRZ-ICECREAM-CUP", "Ice Cream Cup", "Frozen Goods", ProductUnit.Piece, 25m, 40m, Bt.FrozenGoods, Bt.SariSari, Bt.MiniGrocery),
        P("PH-FRZ-SIOPAO", "Frozen Siopao", "Frozen Goods", ProductUnit.Piece, 20m, 35m, Bt.FrozenGoods, Bt.FoodCart),
        P("PH-FRZ-DUMPLING", "Frozen Dumpling Pack", "Frozen Goods", ProductUnit.Pack, 45m, 70m, Bt.FrozenGoods),
        P("PH-FRZ-HOTDOG", "Frozen Hotdog Pack", "Frozen Goods", ProductUnit.Pack, 55m, 85m, Bt.FrozenGoods, Bt.FoodCart, Bt.MiniGrocery),
        P("PH-FRZ-NUGGETS", "Chicken Nuggets Pack", "Frozen Goods", ProductUnit.Pack, 80m, 120m, Bt.FrozenGoods, Bt.MiniGrocery),
        P("PH-FRZ-FRIES", "Frozen Fries Pack", "Frozen Goods", ProductUnit.Pack, 70m, 110m, Bt.FrozenGoods, Bt.FoodCart),
        P("PH-FRZ-CORNDOG", "Frozen Corndog", "Frozen Goods", ProductUnit.Piece, 18m, 30m, Bt.FrozenGoods, Bt.FoodCart),

        // Bakery
        P("PH-BAK-PANDESAL", "Pandesal", "Baked Goods", ProductUnit.Piece, 2m, 4m, Bt.Bakery, Bt.SariSari, Bt.Cafe),
        P("PH-BAK-ENSAYMADA", "Ensaymada", "Baked Goods", ProductUnit.Piece, 18m, 30m, Bt.Bakery, Bt.Cafe),
        P("PH-BAK-MONAY", "Monay", "Baked Goods", ProductUnit.Piece, 8m, 14m, Bt.Bakery),
        P("PH-BAK-SPANISH-BREAD", "Spanish Bread", "Baked Goods", ProductUnit.Piece, 10m, 16m, Bt.Bakery),
        P("PH-BAK-CHEESE-ROLL", "Cheese Roll", "Baked Goods", ProductUnit.Piece, 12m, 20m, Bt.Bakery, Bt.Cafe),
        P("PH-BAK-CAKE-SLICE", "Cake Slice", "Baked Goods", ProductUnit.Piece, 35m, 55m, Bt.Bakery, Bt.Cafe),
        P("PH-BAK-DONUT", "Donut", "Baked Goods", ProductUnit.Piece, 15m, 25m, Bt.Bakery, Bt.Cafe),
        P("PH-BAK-CROISSANT", "Croissant", "Baked Goods", ProductUnit.Piece, 28m, 45m, Bt.Bakery, Bt.Cafe),
        P("PH-BAK-LOAF-BREAD", "Loaf Bread", "Baked Goods", ProductUnit.Piece, 40m, 60m, Bt.Bakery, Bt.MiniGrocery),
        P("PH-BAK-COOKIE-PACK", "Cookie Pack", "Baked Goods", ProductUnit.Pack, 30m, 48m, Bt.Bakery, Bt.Cafe),

        // Cafe
        P("PH-CAF-BREWED", "Brewed Coffee", "Coffee & Drinks", ProductUnit.Piece, 25m, 55m, Bt.Cafe),
        P("PH-CAF-AMERICANO", "Americano", "Coffee & Drinks", ProductUnit.Piece, 30m, 70m, Bt.Cafe),
        P("PH-CAF-LATTE", "Latte", "Coffee & Drinks", ProductUnit.Piece, 40m, 95m, Bt.Cafe),
        P("PH-CAF-CAPPUCCINO", "Cappuccino", "Coffee & Drinks", ProductUnit.Piece, 40m, 95m, Bt.Cafe),
        P("PH-CAF-ICED-COFFEE", "Iced Coffee", "Coffee & Drinks", ProductUnit.Piece, 35m, 85m, Bt.Cafe),
        P("PH-CAF-HOT-CHOC", "Hot Chocolate", "Coffee & Drinks", ProductUnit.Piece, 30m, 75m, Bt.Cafe),
        P("PH-CAF-SANDWICH", "Cafe Sandwich", "Coffee & Drinks", ProductUnit.Piece, 50m, 110m, Bt.Cafe),
        P("PH-CAF-PASTRY", "Pastry", "Baked Goods", ProductUnit.Piece, 25m, 55m, Bt.Cafe, Bt.Bakery),

        // Pharmacy
        P("PH-PHARM-PARACETAMOL", "Paracetamol Tablet Strip", "Pharmacy Basics", ProductUnit.Pack, 8m, 15m, Bt.Pharmacy),
        P("PH-PHARM-VITAMIN-C", "Vitamin C Tablet Bottle", "Pharmacy Basics", ProductUnit.Bottle, 45m, 75m, Bt.Pharmacy),
        P("PH-PHARM-ALCOHOL", "Isopropyl Alcohol", "Pharmacy Basics", ProductUnit.Bottle, 35m, 55m, Bt.Pharmacy, Bt.WaterRefilling, Bt.GeneralRetail),
        P("PH-PHARM-MASK", "Face Mask Pack", "Pharmacy Basics", ProductUnit.Pack, 25m, 40m, Bt.Pharmacy, Bt.GeneralRetail),
        P("PH-PHARM-BANDAGE", "Adhesive Bandage Pack", "Pharmacy Basics", ProductUnit.Pack, 20m, 35m, Bt.Pharmacy),
        P("PH-PHARM-COTTON", "Cotton Balls Pack", "Pharmacy Basics", ProductUnit.Pack, 18m, 30m, Bt.Pharmacy),
        P("PH-PHARM-ANTISEPTIC", "Antiseptic Solution", "Pharmacy Basics", ProductUnit.Bottle, 40m, 65m, Bt.Pharmacy),
        P("PH-PHARM-ORS", "ORS Sachet", "Pharmacy Basics", ProductUnit.Sachet, 10m, 18m, Bt.Pharmacy, Bt.SariSari),
        P("PH-PHARM-MULTIVIT", "Multivitamin Bottle", "Pharmacy Basics", ProductUnit.Bottle, 80m, 130m, Bt.Pharmacy),
        P("PH-PHARM-THERMOMETER", "Digital Thermometer", "Pharmacy Basics", ProductUnit.Piece, 150m, 250m, Bt.Pharmacy),

        // Carinderia
        P("PH-CAR-RICE-PLATE", "Rice Serving", "Prepared Meals", ProductUnit.Piece, 8m, 15m, Bt.Carinderia),
        P("PH-CAR-ADOBO", "Chicken Adobo Plate", "Prepared Meals", ProductUnit.Piece, 45m, 80m, Bt.Carinderia),
        P("PH-CAR-MENUDO", "Menudo Plate", "Prepared Meals", ProductUnit.Piece, 45m, 80m, Bt.Carinderia),
        P("PH-CAR-SINIGANG", "Sinigang Plate", "Prepared Meals", ProductUnit.Piece, 50m, 90m, Bt.Carinderia),
        P("PH-CAR-PANCIT", "Pancit Serving", "Prepared Meals", ProductUnit.Piece, 40m, 70m, Bt.Carinderia, Bt.StreetFood),
        P("PH-CAR-FRIED-CHICKEN", "Fried Chicken Piece", "Prepared Meals", ProductUnit.Piece, 35m, 60m, Bt.Carinderia, Bt.FoodCart),
        P("PH-CAR-VEG-ULAM", "Vegetable Ulam", "Prepared Meals", ProductUnit.Piece, 30m, 55m, Bt.Carinderia),
        P("PH-CAR-LUMPIA", "Lumpiang Shanghai", "Prepared Meals", ProductUnit.Piece, 8m, 15m, Bt.Carinderia, Bt.StreetFood),

        // Street food / food cart
        P("PH-SF-FISHBALL", "Fishball Stick", "Street Food", ProductUnit.Piece, 8m, 15m, Bt.StreetFood, Bt.FoodCart),
        P("PH-SF-KIKIAM", "Kikiam Stick", "Street Food", ProductUnit.Piece, 10m, 18m, Bt.StreetFood, Bt.FoodCart),
        P("PH-SF-SQUIDBALL", "Squidball Stick", "Street Food", ProductUnit.Piece, 12m, 20m, Bt.StreetFood),
        P("PH-SF-BANANA-CUE", "Banana Cue", "Street Food", ProductUnit.Piece, 10m, 18m, Bt.StreetFood),
        P("PH-SF-CAMOTE-CUE", "Camote Cue", "Street Food", ProductUnit.Piece, 10m, 18m, Bt.StreetFood),
        P("PH-SF-TAHO", "Taho Cup", "Street Food", ProductUnit.Piece, 15m, 25m, Bt.StreetFood),
        P("PH-FC-HOTDOG", "Hotdog Sandwich", "Street Food", ProductUnit.Piece, 20m, 35m, Bt.FoodCart, Bt.StreetFood),
        P("PH-FC-BURGER", "Burger", "Street Food", ProductUnit.Piece, 35m, 60m, Bt.FoodCart),
        P("PH-FC-FRIES", "Fries Serving", "Street Food", ProductUnit.Piece, 25m, 45m, Bt.FoodCart),
        P("PH-FC-SIOMAI", "Siomai (4pcs)", "Street Food", ProductUnit.Pack, 20m, 35m, Bt.FoodCart, Bt.StreetFood),
        P("PH-FC-SIOPAO", "Siopao", "Street Food", ProductUnit.Piece, 25m, 40m, Bt.FoodCart, Bt.StreetFood, Bt.FrozenGoods),
        P("PH-FC-NACHOS", "Nachos", "Street Food", ProductUnit.Piece, 30m, 55m, Bt.FoodCart),

        // Water refill
        P("PH-WR-REFILL-5G", "Purified Water Refill 5 Gallon", "Water Refill", ProductUnit.Piece, 20m, 35m, Bt.WaterRefilling),
        P("PH-WR-REFILL-GALLON", "Purified Water Refill 1 Gallon", "Water Refill", ProductUnit.Piece, 8m, 15m, Bt.WaterRefilling),
        P("PH-WR-CONTAINER-5G", "Empty Round Container 5 Gallon", "Water Refill", ProductUnit.Piece, 150m, 250m, Bt.WaterRefilling),
        P("PH-WR-DISPENSER-RENT", "Water Dispenser Rental Day", "Water Refill", ProductUnit.Piece, 30m, 50m, Bt.WaterRefilling),
        P("PH-WR-HALF-LITER", "Purified Water 500ml Seal", "Water Refill", ProductUnit.Bottle, 5m, 10m, Bt.WaterRefilling, Bt.SariSari),

        // General retail extras
        P("PH-GEN-NOTEBOOK", "Notebook", "General Merchandise", ProductUnit.Piece, 20m, 35m, Bt.GeneralRetail),
        P("PH-GEN-BALLPEN", "Ballpen", "General Merchandise", ProductUnit.Piece, 5m, 10m, Bt.GeneralRetail),
        P("PH-GEN-BATTERY-AA", "Battery AA Pack", "General Merchandise", ProductUnit.Pack, 40m, 65m, Bt.GeneralRetail, Bt.MiniGrocery),
        P("PH-GEN-LIGHTER", "Lighter", "General Merchandise", ProductUnit.Piece, 8m, 15m, Bt.GeneralRetail, Bt.SariSari, Bt.WaterRefilling),
        P("PH-GEN-UMBRELLA", "Foldable Umbrella", "General Merchandise", ProductUnit.Piece, 80m, 140m, Bt.GeneralRetail)
    ];

    public static IReadOnlyList<TemplateDef> Templates { get; } =
    [
        T("Sari-Sari Starter", "sari-sari-starter", Bt.SariSari,
            "PH-BEV-WATER-500", "PH-BEV-SOFTDRINK-CAN", "PH-BEV-SOFTDRINK-1L", "PH-BEV-JUICE-BOX",
            "PH-SNK-BISCUIT-PACK", "PH-SNK-CHIPS-SMALL", "PH-SNK-CANDY-PACK", "PH-NOODLE-INSTANT",
            "PH-COFFEE-SACHET", "PH-CAN-SARDINES", "PH-CAN-CORNEDBEEF", "PH-CAN-MEATLOAF",
            "PH-COND-SOYSAUCE", "PH-COND-VINEGAR", "PH-COND-FISHSAUCE",
            "PH-HH-DETERGENT-SACHET", "PH-TOIL-SOAP-BAR", "PH-TOIL-SHAMPOO-SACHET",
            "PH-RICE-5KG-PACK", "PH-PHARM-ORS", "PH-GEN-LIGHTER", "PH-FISH-DRIED-FISH"),
        T("Mini Grocery Starter", "mini-grocery-starter", Bt.MiniGrocery,
            "PH-BEV-WATER-500", "PH-BEV-SOFTDRINK-1L", "PH-SNK-BISCUIT-PACK", "PH-NOODLE-INSTANT",
            "PH-CAN-SARDINES", "PH-CAN-CORNEDBEEF", "PH-COND-SOYSAUCE", "PH-HH-DISHWASH",
            "PH-TOIL-TOOTHPASTE", "PH-RICE-5KG-PACK", "PH-RICE-25KG-SACK", "PH-VEG-TOMATO",
            "PH-VEG-ONION", "PH-FRU-BANANA", "PH-FRU-APPLE", "PH-FRZ-CHICKEN",
            "PH-FRZ-HOTDOG", "PH-BAK-LOAF-BREAD", "PH-GEN-BATTERY-AA", "PH-BEV-JUICE-BOX"),
        T("Bakery Starter", "bakery-starter", Bt.Bakery,
            "PH-BAK-PANDESAL", "PH-BAK-ENSAYMADA", "PH-BAK-MONAY", "PH-BAK-SPANISH-BREAD",
            "PH-BAK-CHEESE-ROLL", "PH-BAK-CAKE-SLICE", "PH-BAK-DONUT", "PH-BAK-CROISSANT",
            "PH-BAK-LOAF-BREAD", "PH-BAK-COOKIE-PACK", "PH-CAF-PASTRY", "PH-BEV-WATER-500",
            "PH-COFFEE-SACHET", "PH-BEV-SOFTDRINK-CAN", "PH-SNK-BISCUIT-PACK"),
        T("Cafe / Coffee Shop Starter", "cafe-coffee-shop-starter", Bt.Cafe,
            "PH-CAF-BREWED", "PH-CAF-AMERICANO", "PH-CAF-LATTE", "PH-CAF-CAPPUCCINO",
            "PH-CAF-ICED-COFFEE", "PH-CAF-HOT-CHOC", "PH-CAF-SANDWICH", "PH-CAF-PASTRY",
            "PH-BAK-CAKE-SLICE", "PH-BAK-CROISSANT", "PH-BAK-DONUT", "PH-BEV-WATER-500",
            "PH-COFFEE-SACHET", "PH-BAK-ENSAYMADA", "PH-BAK-CHEESE-ROLL"),
        T("Pharmacy Starter", "pharmacy-starter", Bt.Pharmacy,
            "PH-PHARM-PARACETAMOL", "PH-PHARM-VITAMIN-C", "PH-PHARM-ALCOHOL", "PH-PHARM-MASK",
            "PH-PHARM-BANDAGE", "PH-PHARM-COTTON", "PH-PHARM-ANTISEPTIC", "PH-PHARM-ORS",
            "PH-PHARM-MULTIVIT", "PH-PHARM-THERMOMETER", "PH-TOIL-SOAP-BAR", "PH-TOIL-TOOTHPASTE",
            "PH-TOIL-SHAMPOO-SACHET", "PH-BEV-WATER-500", "PH-BEV-JUICE-BOX"),
        T("General Retail Starter", "general-retail-starter", Bt.GeneralRetail,
            "PH-BEV-WATER-500", "PH-BEV-SOFTDRINK-CAN", "PH-SNK-BISCUIT-PACK", "PH-NOODLE-INSTANT",
            "PH-CAN-SARDINES", "PH-COND-SOYSAUCE", "PH-HH-DETERGENT-SACHET", "PH-TOIL-SOAP-BAR",
            "PH-GEN-NOTEBOOK", "PH-GEN-BALLPEN", "PH-GEN-BATTERY-AA", "PH-GEN-LIGHTER",
            "PH-GEN-UMBRELLA", "PH-PHARM-ALCOHOL", "PH-PHARM-MASK", "PH-SNK-CHIPS-SMALL"),
        T("Vegetable Vendor Starter", "vegetable-vendor-starter", Bt.VegetableVendor,
            "PH-VEG-TOMATO", "PH-VEG-ONION", "PH-VEG-GARLIC", "PH-VEG-POTATO", "PH-VEG-CARROT",
            "PH-VEG-PECHAY", "PH-VEG-CABBAGE", "PH-VEG-EGGPLANT", "PH-VEG-SQUASH", "PH-VEG-OKRA",
            "PH-VEG-SITAW", "PH-VEG-KALABASA", "PH-VEG-AMPLALAYA", "PH-VEG-KANGKONG", "PH-VEG-GINGER"),
        T("Fruit Vendor Starter", "fruit-vendor-starter", Bt.FruitVendor,
            "PH-FRU-BANANA", "PH-FRU-MANGO", "PH-FRU-PAPAYA", "PH-FRU-PINEAPPLE", "PH-FRU-WATERMELON",
            "PH-FRU-CALAMANSI", "PH-FRU-APPLE", "PH-FRU-ORANGE", "PH-FRU-GRAPES", "PH-FRU-AVOCADO",
            "PH-FRU-GUAVA", "PH-FRU-LANZONES", "PH-FRU-RAMBUTAN", "PH-FRU-DURIAN", "PH-FRU-COCONUT"),
        T("Fish Vendor Starter", "fish-vendor-starter", Bt.FishVendor,
            "PH-FISH-BANGUS", "PH-FISH-TILAPIA", "PH-FISH-GALUNGGONG", "PH-FISH-TUNA", "PH-FISH-SQUID",
            "PH-FISH-SHRIMP", "PH-FISH-CRAB", "PH-FISH-TANIGUE", "PH-FISH-LAPULAPU", "PH-FISH-SARDINES-FRESH",
            "PH-FISH-MACKEREL", "PH-FISH-MUSSEL", "PH-FISH-CLAM", "PH-FISH-MILKFISH-BELLY", "PH-FISH-DRIED-FISH"),
        T("Meat Vendor Starter", "meat-vendor-starter", Bt.MeatVendor,
            "PH-MEAT-PORK", "PH-MEAT-BEEF", "PH-MEAT-CHICKEN", "PH-MEAT-PORK-BELLY", "PH-MEAT-GROUND-PORK",
            "PH-MEAT-CHICKEN-THIGH", "PH-MEAT-CHICKEN-WING", "PH-MEAT-PORK-CHOP", "PH-MEAT-BEEF-STEW",
            "PH-MEAT-LIVER", "PH-MEAT-HOTDOG-BULK", "PH-MEAT-LONGANISA", "PH-MEAT-TOCINO", "PH-MEAT-BACON",
            "PH-MEAT-RIBS"),
        T("Rice Retailer Starter", "rice-retailer-starter", Bt.RiceRetailer,
            "PH-RICE-SINANDOMENG", "PH-RICE-JASMINE", "PH-RICE-DINORADO", "PH-RICE-BROWN",
            "PH-RICE-GLUTINOUS", "PH-RICE-25KG-SACK", "PH-RICE-5KG-PACK", "PH-BEV-WATER-500",
            "PH-CAN-SARDINES", "PH-COND-SOYSAUCE", "PH-NOODLE-INSTANT", "PH-SNK-BISCUIT-PACK"),
        T("Frozen Goods Starter", "frozen-goods-starter", Bt.FrozenGoods,
            "PH-FRZ-CHICKEN", "PH-FRZ-FISH", "PH-FRZ-VEG-MIX", "PH-FRZ-ICECREAM-CUP", "PH-FRZ-SIOPAO",
            "PH-FRZ-DUMPLING", "PH-FRZ-HOTDOG", "PH-FRZ-NUGGETS", "PH-FRZ-FRIES", "PH-FRZ-CORNDOG",
            "PH-BEV-WATER-500", "PH-BEV-SOFTDRINK-CAN", "PH-MEAT-HOTDOG-BULK", "PH-FC-SIOPAO"),
        T("Carinderia / Eatery Starter", "carinderia-eatery-starter", Bt.Carinderia,
            "PH-CAR-RICE-PLATE", "PH-CAR-ADOBO", "PH-CAR-MENUDO", "PH-CAR-SINIGANG", "PH-CAR-PANCIT",
            "PH-CAR-FRIED-CHICKEN", "PH-CAR-VEG-ULAM", "PH-CAR-LUMPIA", "PH-BEV-WATER-500",
            "PH-BEV-SOFTDRINK-CAN", "PH-VEG-TOMATO", "PH-VEG-ONION", "PH-VEG-PECHAY", "PH-RICE-SINANDOMENG",
            "PH-MEAT-CHICKEN", "PH-COND-SOYSAUCE"),
        T("Street Food Starter", "street-food-starter", Bt.StreetFood,
            "PH-SF-FISHBALL", "PH-SF-KIKIAM", "PH-SF-SQUIDBALL", "PH-SF-BANANA-CUE", "PH-SF-CAMOTE-CUE",
            "PH-SF-TAHO", "PH-FC-SIOMAI", "PH-FC-SIOPAO", "PH-CAR-PANCIT", "PH-CAR-LUMPIA",
            "PH-BEV-WATER-500", "PH-BEV-SOFTDRINK-CAN", "PH-BEV-JUICE-BOX", "PH-FRU-COCONUT",
            "PH-FC-HOTDOG"),
        T("Food Cart Starter", "food-cart-starter", Bt.FoodCart,
            "PH-FC-HOTDOG", "PH-FC-BURGER", "PH-FC-FRIES", "PH-FC-SIOMAI", "PH-FC-SIOPAO", "PH-FC-NACHOS",
            "PH-CAR-FRIED-CHICKEN", "PH-SF-FISHBALL", "PH-SF-KIKIAM", "PH-BEV-WATER-500",
            "PH-BEV-SOFTDRINK-CAN", "PH-BEV-JUICE-BOX", "PH-SNK-CHIPS-SMALL", "PH-FRZ-FRIES",
            "PH-FRZ-HOTDOG"),
        T("Water Refilling Starter", "water-refilling-starter", Bt.WaterRefilling,
            "PH-WR-REFILL-5G", "PH-WR-REFILL-GALLON", "PH-WR-CONTAINER-5G", "PH-WR-DISPENSER-RENT",
            "PH-WR-HALF-LITER", "PH-BEV-WATER-500", "PH-PHARM-ALCOHOL", "PH-HH-DISHWASH",
            "PH-TOIL-SOAP-BAR", "PH-GEN-LIGHTER")
    ];

    private static class Bt
    {
        public const string SariSari = LegacyBusinessTypeSeeds.SariSariCode;
        public const string MiniGrocery = LegacyBusinessTypeSeeds.MiniGroceryCode;
        public const string Bakery = LegacyBusinessTypeSeeds.BakeryCode;
        public const string Cafe = LegacyBusinessTypeSeeds.CafeCode;
        public const string Pharmacy = LegacyBusinessTypeSeeds.PharmacyCode;
        public const string GeneralRetail = LegacyBusinessTypeSeeds.GeneralRetailCode;
        public const string VegetableVendor = PhilippineBusinessTypeSeeds.VegetableVendorCode;
        public const string FruitVendor = PhilippineBusinessTypeSeeds.FruitVendorCode;
        public const string FishVendor = PhilippineBusinessTypeSeeds.FishVendorCode;
        public const string MeatVendor = PhilippineBusinessTypeSeeds.MeatVendorCode;
        public const string RiceRetailer = PhilippineBusinessTypeSeeds.RiceRetailerCode;
        public const string FrozenGoods = PhilippineBusinessTypeSeeds.FrozenGoodsCode;
        public const string Carinderia = PhilippineBusinessTypeSeeds.CarinderiaCode;
        public const string StreetFood = PhilippineBusinessTypeSeeds.StreetFoodVendorCode;
        public const string FoodCart = PhilippineBusinessTypeSeeds.FoodCartCode;
        public const string WaterRefilling = PhilippineBusinessTypeSeeds.WaterRefillingCode;
    }

    private static ProductDef P(
        string sku,
        string name,
        string category,
        ProductUnit unit,
        decimal cost,
        decimal sell,
        params string[] bts) =>
        new(sku, name, category, unit, ProductSellingMode.PerItem, cost, sell, GenericBrand, bts);

    private static ProductDef W(
        string sku,
        string name,
        string category,
        decimal cost,
        decimal sell,
        params string[] bts) =>
        new(sku, name, category, ProductUnit.Kilogram, ProductSellingMode.ByWeight, cost, sell, LocalProduceBrand, bts);

    private static TemplateDef T(string name, string slug, string primaryBt, params string[] skus) =>
        new(name, slug, primaryBt, $"{name} for Philippine Pinoy Business POS (optional curated starter).", skus);
}
