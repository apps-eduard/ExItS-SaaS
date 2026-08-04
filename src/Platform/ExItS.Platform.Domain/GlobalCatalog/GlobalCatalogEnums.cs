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
