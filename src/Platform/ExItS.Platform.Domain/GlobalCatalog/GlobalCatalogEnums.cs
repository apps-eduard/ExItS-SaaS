namespace ExItS.Platform.Domain.GlobalCatalog;

public enum GlobalProductStatus
{
    Draft = 0,
    Active = 1,
    Archived = 2
}

public enum GlobalCategoryStatus
{
    Active = 0,
    Inactive = 1,
    Archived = 2
}

public enum BusinessType
{
    SariSari = 0,
    MiniGrocery = 1,
    Bakery = 2,
    Cafe = 3,
    Pharmacy = 4,
    GeneralRetail = 5
}

public enum ProductUnit
{
    Piece = 0,
    Pack = 1,
    Box = 2,
    Bottle = 3,
    Can = 4,
    Sachet = 5,
    Kilogram = 6,
    Gram = 7,
    Liter = 8,
    Milliliter = 9
}

public enum CatalogTemplateStatus
{
    Draft = 0,
    Published = 1,
    Archived = 2
}

public enum SelectionMode
{
    Curated = 0,
    Auto = 1,
    Hybrid = 2
}

public enum CatalogImportJobStatus
{
    Validated = 0,
    Queued = 1,
    Processing = 2,
    Completed = 3,
    CompletedWithWarnings = 4,
    Failed = 5
}

public enum CatalogImportItemStatus
{
    Pending = 0,
    Imported = 1,
    Skipped = 2,
    Failed = 3
}

public enum CatalogImportFileFormat
{
    Csv = 0,
    Xlsx = 1
}

/// <summary>Whitelist for global product list sorting. Never map arbitrary client strings into SQL.</summary>
public enum GlobalProductListSortBy
{
    Name = 0,
    Sku = 1,
    Barcode = 2,
    Brand = 3,
    Category = 4,
    Unit = 5,
    Status = 6,
    UpdatedAtUtc = 7,
    CreatedAtUtc = 8,
    CostPrice = 9,
    SellingPrice = 10
}

/// <summary>Whitelist for global category list sorting.</summary>
public enum GlobalCategoryListSortBy
{
    Name = 0,
    SortOrder = 1,
    Status = 2,
    UpdatedAtUtc = 3,
    CreatedAtUtc = 4
}

/// <summary>Whitelist for catalog template list sorting.</summary>
public enum CatalogTemplateListSortBy
{
    Name = 0,
    Slug = 1,
    Status = 2,
    PrimaryBusinessType = 3,
    UpdatedAtUtc = 4,
    CreatedAtUtc = 5,
    ProductCount = 6
}
