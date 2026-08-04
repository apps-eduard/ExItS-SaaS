namespace ExItS.PinoyBusinessPOS.Domain.Catalog;

public enum PosCatalogImportJobKind
{
    TemplateBatch = 0,
    SelectedProducts = 1
}

public enum PosCatalogImportJobStatus
{
    Queued = 0,
    Processing = 1,
    Completed = 2,
    CompletedWithWarnings = 3,
    Failed = 4,
    Cancelled = 5
}

public enum PosCatalogImportItemStatus
{
    Pending = 0,
    Imported = 1,
    Skipped = 2,
    Failed = 3
}
