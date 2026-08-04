using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;

namespace ExItS.PinoyBusinessPOS.UnitTests.Catalog;

public sealed class CatalogImportJobTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly Guid GlobalProductA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid GlobalProductB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid TemplateId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void CreateQueued_TemplateBatch_SetsSnapshotAndQueuedStatus()
    {
        var now = DateTimeOffset.Parse("2026-08-05T00:00:00Z");
        var items = new[]
        {
            CatalogImportItemResult.CreatePending(GlobalProductA, 0, "Sardines", "Piece", 25.50m, sku: "SAR-1"),
            CatalogImportItemResult.CreatePending(GlobalProductB, 1, "Noodles", "Pack", 12m)
        };

        var job = CatalogImportJob.CreateQueued(
            Org,
            PosCatalogImportJobKind.TemplateBatch,
            CatalogSource.Template,
            "actor-1",
            items,
            now,
            TemplateId,
            batchNumber: 1,
            idempotencyKey: "idem-1");

        Assert.Equal(PosCatalogImportJobStatus.Queued, job.Status);
        Assert.Equal(2, job.TotalCount);
        Assert.Equal(TemplateId, job.PlatformTemplateId);
        Assert.Equal(1, job.BatchNumber);
        Assert.Equal("idem-1", job.IdempotencyKey);
        Assert.Equal(CatalogSource.Template, job.CatalogSource);
    }

    [Fact]
    public void Complete_WithSkippedItems_CompletedWithWarnings()
    {
        var now = DateTimeOffset.Parse("2026-08-05T00:00:00Z");
        var items = new[]
        {
            CatalogImportItemResult.CreatePending(GlobalProductA, 0, "Sardines", "Piece", 25.50m),
            CatalogImportItemResult.CreatePending(GlobalProductB, 1, "Noodles", "Pack", 12m)
        };
        var job = CatalogImportJob.CreateQueued(
            Org,
            PosCatalogImportJobKind.SelectedProducts,
            CatalogSource.GlobalSearch,
            "actor-1",
            items,
            now);

        job.BeginProcessing(now);
        items[0].MarkImported(CatalogProductId.New(), now);
        items[1].MarkSkipped("pos.product.sku.conflict", "SKU conflict", now);
        job.Complete(now.AddMinutes(1));

        Assert.Equal(PosCatalogImportJobStatus.CompletedWithWarnings, job.Status);
        Assert.Equal(1, job.ImportedCount);
        Assert.Equal(1, job.SkippedCount);
        Assert.Equal(0, job.FailedCount);
    }

    [Fact]
    public void CreateQueued_DuplicateGlobalIds_Throws()
    {
        var now = DateTimeOffset.Parse("2026-08-05T00:00:00Z");
        var items = new[]
        {
            CatalogImportItemResult.CreatePending(GlobalProductA, 0, "A", "Piece", 1m),
            CatalogImportItemResult.CreatePending(GlobalProductA, 1, "B", "Piece", 2m)
        };

        var ex = Assert.Throws<DomainException>(() => CatalogImportJob.CreateQueued(
            Org,
            PosCatalogImportJobKind.SelectedProducts,
            CatalogSource.GlobalSearch,
            "actor",
            items,
            now));

        Assert.Equal(DomainErrorCodes.InvalidCatalogImportJob, ex.ErrorCode);
    }

    [Fact]
    public void CreateImportedSnapshot_SetsProvenance_ManualCreateDoesNot()
    {
        var now = DateTimeOffset.Parse("2026-08-05T00:00:00Z");
        var imported = CatalogProduct.CreateImportedSnapshot(
            Org,
            "Sardines",
            UnitOfMeasure.Piece,
            25.5m,
            GlobalProductA,
            CatalogSource.Template,
            now,
            platformTemplateId: TemplateId,
            sourceGlobalCategoryId: Guid.Parse("44444444-4444-4444-4444-444444444444"));

        Assert.Equal(GlobalProductA, imported.PlatformGlobalProductId);
        Assert.Equal(TemplateId, imported.PlatformTemplateId);
        Assert.Equal(CatalogSource.Template, imported.CatalogSource);
        Assert.Equal(now, imported.CatalogImportedAt);
        Assert.Equal(CatalogImportRules.SnapshotVersion, imported.CatalogSnapshotVersion);
        Assert.NotNull(imported.SourceGlobalCategoryId);

        var manual = CatalogProduct.Create(Org, "Custom", UnitOfMeasure.Piece, 10m, now);
        Assert.Null(manual.PlatformGlobalProductId);
        Assert.Equal(CatalogSource.Manual, manual.CatalogSource);
        Assert.Null(manual.CatalogImportedAt);
    }

    [Fact]
    public void UpdateDetails_DoesNotClearProvenance()
    {
        var now = DateTimeOffset.Parse("2026-08-05T00:00:00Z");
        var product = CatalogProduct.CreateImportedSnapshot(
            Org,
            "Sardines",
            UnitOfMeasure.Piece,
            25.5m,
            GlobalProductA,
            CatalogSource.GlobalSearch,
            now);

        product.UpdateDetails("Local name", null, null, null, null, UnitOfMeasure.Piece, 30m, now.AddMinutes(1));

        Assert.Equal("Local name", product.Name);
        Assert.Equal(30m, product.SellingPrice);
        Assert.Equal(GlobalProductA, product.PlatformGlobalProductId);
        Assert.Equal(CatalogSource.GlobalSearch, product.CatalogSource);
    }
}
