using System.Reflection;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Inventory;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Sales;
using Microsoft.EntityFrameworkCore;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class DirectPurchaseHistoryQueryIntegrationTests(PosPostgreSqlFixture fixture)
{
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    [Fact]
    public async Task Unified_list_paginates_local_and_b2b_globally_and_respects_filters()
    {
        var options = new DbContextOptionsBuilder<PosDbContext>()
            .UseNpgsql(fixture.ConnectionString)
            .Options;
        await using (var migrate = new PosDbContext(options))
        {
            await migrate.Database.MigrateAsync();
        }

        var buyer = Guid.NewGuid();
        var seller = Guid.NewGuid();
        var otherBuyer = Guid.NewGuid();
        var otherSeller = Guid.NewGuid();

        var b2bCompleted = Guid.NewGuid();
        var b2bVoided = Guid.NewGuid();
        var localPosted = Guid.NewGuid();
        var localVoided = Guid.NewGuid();
        var foreignSale = Guid.NewGuid();

        var t1 = new DateTimeOffset(2026, 9, 10, 10, 0, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 9, 9, 10, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 9, 8, 10, 0, 0, TimeSpan.Zero);
        var t4 = new DateTimeOffset(2026, 9, 7, 10, 0, 0, TimeSpan.Zero);

        await using (var db = new PosDbContext(options))
        {
            db.ConnectedSupplierRelationships.Add(new ConnectedSupplierRelationshipRecord
            {
                Id = Guid.NewGuid(),
                BuyerOrganizationId = buyer,
                SupplierOrganizationId = seller,
                Status = 3, // Disconnected — history must still resolve
                RequestedAtUtc = t4,
                DisconnectedAtUtc = t1,
                SupplierDisplayNameSnapshot = "Mica Store",
                SupplierPublicOrganizationIdSnapshot = "ORG000001",
                SupplierBranchNameSnapshot = "Main Branch",
                CreatedAtUtc = t4,
                UpdatedAtUtc = t1
            });

            db.Sales.Add(MakeSale(b2bCompleted, seller, buyer, "TXN-001245", "Completed", 1250m, t1, unitCost: 9m));
            db.Sales.Add(MakeSale(b2bVoided, seller, buyer, "TXN-000900", "Voided", 400m, t3, voided: true));
            db.Sales.Add(MakeSale(foreignSale, otherSeller, otherBuyer, "TXN-OTHER", "Completed", 999m, t1));

            db.DirectPurchaseReceipts.Add(MakeLocal(localPosted, buyer, "DPR-000044", "Public Market", 2100m, new DateOnly(2026, 9, 9), t2));
            db.DirectPurchaseReceipts.Add(MakeLocal(localVoided, buyer, "DPR-000010", "Old Stall", 50m, new DateOnly(2026, 9, 7), t4, voided: true));

            await db.SaveChangesAsync();
        }

        var query = new DirectPurchaseHistoryQuery(new PosDbContext(options));

        var (all, allTotal) = await query.ListAsync(buyer, new DirectPurchaseHistoryFilter(), 0, 10);
        Assert.Equal(4, allTotal);
        Assert.Equal(4, all.Count);
        Assert.Equal(["B2B", "Local", "B2B", "Local"], all.Select(x => x.SourceType).ToArray());
        Assert.Equal(b2bCompleted, all[0].SourceId);
        Assert.Equal("Mica Store", all[0].SellerDisplayName);
        Assert.Equal("ORG000001", all[0].SellerPublicOrganizationId);
        Assert.Equal(localPosted, all[1].SourceId);
        Assert.Equal(b2bVoided, all[2].SourceId);
        Assert.Equal("Voided", all[2].Status);

        var (page1, pageTotal) = await query.ListAsync(buyer, new DirectPurchaseHistoryFilter(), 0, 2);
        Assert.Equal(4, pageTotal);
        Assert.Equal(2, page1.Count);
        Assert.Equal(b2bCompleted, page1[0].SourceId);
        Assert.Equal(localPosted, page1[1].SourceId);

        var (page2, _) = await query.ListAsync(buyer, new DirectPurchaseHistoryFilter(), 2, 2);
        Assert.Equal(2, page2.Count);
        Assert.Equal(b2bVoided, page2[0].SourceId);
        Assert.Equal(localVoided, page2[1].SourceId);

        var (b2bOnly, b2bTotal) = await query.ListAsync(
            buyer,
            new DirectPurchaseHistoryFilter(SourceType: DirectPurchaseHistorySourceTypes.B2B),
            0,
            20);
        Assert.Equal(2, b2bTotal);
        Assert.All(b2bOnly, x => Assert.Equal(DirectPurchaseHistorySourceTypes.B2B, x.SourceType));

        var (localOnly, localTotal) = await query.ListAsync(
            buyer,
            new DirectPurchaseHistoryFilter(SourceType: DirectPurchaseHistorySourceTypes.Local),
            0,
            20);
        Assert.Equal(2, localTotal);
        Assert.All(localOnly, x => Assert.Equal(DirectPurchaseHistorySourceTypes.Local, x.SourceType));

        var (completed, completedTotal) = await query.ListAsync(
            buyer,
            new DirectPurchaseHistoryFilter(Status: DirectPurchaseHistoryStatuses.Completed),
            0,
            20);
        Assert.Equal(2, completedTotal);
        Assert.All(completed, x => Assert.Equal("Completed", x.Status));

        var (search, searchTotal) = await query.ListAsync(
            buyer,
            new DirectPurchaseHistoryFilter(Search: "TXN-001245"),
            0,
            20);
        Assert.Equal(1, searchTotal);
        Assert.Equal(b2bCompleted, search[0].SourceId);

        var (other, otherTotal) = await query.ListAsync(otherBuyer, new DirectPurchaseHistoryFilter(), 0, 20);
        Assert.Equal(1, otherTotal);
        Assert.Equal(foreignSale, other[0].SourceId);

        var detail = await query.GetB2bDetailAsync(buyer, b2bCompleted);
        Assert.NotNull(detail);
        Assert.Equal("Mica Store", detail!.SellerDisplayName);
        Assert.Equal("Main Branch", detail.SellerStoreDisplayName);
        Assert.Equal(1250m, detail.TotalAmount);
        AssertBuyerSafeDetail(detail);

        Assert.Null(await query.GetB2bDetailAsync(otherBuyer, b2bCompleted));
        Assert.Null(await query.GetB2bDetailAsync(buyer, foreignSale));

        // Projection must not invent buyer DirectPurchaseReceipt duplicates for B2B sales.
        await using (var db = new PosDbContext(options))
        {
            var duplicateReceipts = await db.DirectPurchaseReceipts
                .CountAsync(r => r.OrganizationId == buyer && r.ReferenceNumber == "TXN-001245");
            Assert.Equal(0, duplicateReceipts);
        }
    }

    private static void AssertBuyerSafeDetail(DirectPurchaseB2bDetailDto detail)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(detail);
        Assert.DoesNotContain("UnitCost", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LineCost", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("TotalCost", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GrossProfit", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GrossMargin", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RecordedBy", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("VoidedBy", json, StringComparison.OrdinalIgnoreCase);

        foreach (var prop in typeof(DirectPurchaseB2bDetailDto).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            Assert.False(prop.Name.Contains("Cost", StringComparison.OrdinalIgnoreCase));
            Assert.False(prop.Name.Contains("Profit", StringComparison.OrdinalIgnoreCase));
            Assert.False(prop.Name.Contains("Margin", StringComparison.OrdinalIgnoreCase));
        }
    }

    private static SaleRecord MakeSale(
        Guid id,
        Guid sellerOrg,
        Guid buyerOrg,
        string number,
        string status,
        decimal total,
        DateTimeOffset recordedAt,
        bool voided = false,
        decimal? unitCost = null) =>
        new()
        {
            Id = id,
            OrganizationId = sellerOrg,
            SaleNumber = number,
            Status = status,
            PaymentMethod = "Cash",
            Subtotal = total,
            Total = total,
            TaxAmount = 0m,
            GrossSubtotal = total,
            LineDiscountTotal = 0m,
            SaleDiscountTotal = 0m,
            DiscountTotal = 0m,
            AmountTendered = total,
            ChangeAmount = 0m,
            BuyerPartyKind = "Organization",
            BuyerOrganizationId = buyerOrg,
            BuyerPublicOrganizationId = "ORGBUYER1",
            BuyerDisplayNameSnapshot = "Kizy",
            StockReservationState = "None",
            RecordedAtUtc = recordedAt,
            RecordedBy = Actor,
            UpdatedAtUtc = recordedAt,
            VoidedAtUtc = voided ? recordedAt : null,
            VoidedBy = voided ? Actor : null,
            VoidReason = voided ? "Test void" : null,
            TotalCostSnapshot = unitCost * 10m,
            CostStatus = unitCost is null ? null : "Complete"
        };

    private static DirectPurchaseReceiptRecord MakeLocal(
        Guid id,
        Guid buyerOrg,
        string number,
        string source,
        decimal total,
        DateOnly purchaseDate,
        DateTimeOffset createdAt,
        bool voided = false) =>
        new()
        {
            Id = id,
            OrganizationId = buyerOrg,
            ReceiptNumber = number,
            PurchaseDate = purchaseDate,
            SourceNameSnapshot = source,
            ReferenceNumber = number,
            TotalCost = total,
            CreatedByUserId = Actor,
            CreatedAtUtc = createdAt,
            Status = voided ? "Voided" : "Posted",
            VoidedAtUtc = voided ? createdAt : null,
            VoidedByUserId = voided ? Actor : null,
            VoidReason = voided ? "Test void" : null
        };
}
