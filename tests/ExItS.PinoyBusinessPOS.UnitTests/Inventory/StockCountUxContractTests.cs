using System.Text.Json;
using System.Text.RegularExpressions;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Domain.Catalog;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.Inventory;

namespace ExItS.PinoyBusinessPOS.UnitTests.Inventory;

public sealed class StockCountUxContractTests
{
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    private static readonly CatalogProductId ProductA = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly CatalogProductId ProductB = CatalogProductId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbcc"));
    private static readonly Guid Actor = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly DateTimeOffset Utc = new(2026, 8, 14, 16, 36, 0, TimeSpan.Zero);

    [Fact]
    public void CreateDraft_rejects_duplicate_products()
    {
        var ex = Assert.Throws<DomainException>(() =>
            StockCount.CreateDraft(
                Org,
                [new StockCountLineDraft(ProductA, null), new StockCountLineDraft(ProductA, null)],
                Utc,
                "Weekly count",
                Actor));
        Assert.Equal(DomainErrorCodes.StockCountDuplicateProduct, ex.ErrorCode);
    }

    [Fact]
    public void CreateDraft_accepts_multiple_distinct_products()
    {
        var draft = StockCount.CreateDraft(
            Org,
            [new StockCountLineDraft(ProductA, null), new StockCountLineDraft(ProductB, null)],
            Utc,
            "Weekly count",
            Actor);
        Assert.Equal(2, draft.Lines.Count);
        Assert.Equal("Weekly count", draft.Title);
    }

    [Fact]
    public void CreateDraft_persists_predefined_and_custom_titles_separately_from_notes()
    {
        var weekly = StockCount.CreateDraft(
            Org,
            [new StockCountLineDraft(ProductA, null)],
            Utc,
            "Weekly count",
            Actor,
            notes: "Counted after Friday closing.");
        Assert.Equal("Weekly count", weekly.Title);
        Assert.Equal("Counted after Friday closing.", weekly.Notes);

        var custom = StockCount.CreateDraft(
            Org,
            [new StockCountLineDraft(ProductA, null)],
            Utc,
            "  Freezer inventory check  ",
            Actor,
            notes: null);
        Assert.Equal("Freezer inventory check", custom.Title);
        Assert.Null(custom.Notes);
    }

    [Fact]
    public void CreateDraft_rejects_blank_custom_title()
    {
        var ex = Assert.Throws<DomainException>(() =>
            StockCount.CreateDraft(Org, [new StockCountLineDraft(ProductA, null)], Utc, "   ", Actor));
        Assert.Equal(DomainErrorCodes.InvalidStockCountTitle, ex.ErrorCode);
    }

    [Fact]
    public void Rehydrate_uses_safe_title_when_missing()
    {
        var count = StockCount.Rehydrate(
            StockCountId.New(),
            Org,
            "CNT-20260801-000001",
            StockCountStatus.Completed,
            new DateOnly(2026, 8, 1),
            title: null,
            notes: null,
            startedAtUtc: Utc,
            startedBy: Actor,
            completedAtUtc: Utc.AddMinutes(20),
            completedBy: Actor,
            cancelledAtUtc: null,
            cancelledBy: null,
            createdBy: null,
            createdAtUtc: Utc,
            updatedAtUtc: Utc.AddMinutes(20),
            lines: []);
        Assert.Equal(StockCount.HistoricalTitle, count.Title);
        Assert.Equal("CNT-20260801-000001", count.CountNumber);
    }

    [Fact]
    public void StockCountNumbers_format_uses_two_digit_sequence_and_expands_naturally()
    {
        var day = new DateOnly(2026, 8, 14);
        Assert.Equal("CNT-20260814-01", StockCountNumbers.Format(day, 1));
        Assert.Equal("CNT-20260814-02", StockCountNumbers.Format(day, 2));
        Assert.Equal("CNT-20260814-100", StockCountNumbers.Format(day, 100));
        Assert.Equal("CNT-20260815-01", StockCountNumbers.Format(new DateOnly(2026, 8, 15), 1));
    }

    [Fact]
    public void StockCountNumbers_normalize_keeps_historical_six_digit_references_readable()
    {
        Assert.Equal("CNT-20260814-000001", StockCountNumbers.Normalize("cnt-20260814-000001"));
        Assert.Equal("CNT-20260814-01", StockCountNumbers.Normalize("CNT-20260814-01"));
        Assert.Throws<DomainException>(() => StockCountNumbers.Normalize("CNT-20260814-1"));
    }

    [Fact]
    public void Created_timestamps_remain_utc()
    {
        var draft = StockCount.CreateDraft(
            Org,
            [new StockCountLineDraft(ProductA, null)],
            Utc,
            "Weekly count",
            Actor);
        Assert.Equal(TimeSpan.Zero, draft.CreatedAtUtc.Offset);
        Assert.Equal(Utc, draft.CreatedAtUtc);
    }

    [Fact]
    public void CreateDraft_persists_created_by_actor()
    {
        var draft = StockCount.CreateDraft(
            Org,
            [new StockCountLineDraft(ProductA, null)],
            Utc,
            "Weekly count",
            Actor);
        Assert.Equal(Actor, draft.CreatedBy);
    }

    [Fact]
    public void CreateDraft_rejects_empty_actor()
    {
        var ex = Assert.Throws<DomainException>(() =>
            StockCount.CreateDraft(
                Org,
                [new StockCountLineDraft(ProductA, null)],
                Utc,
                "Weekly count",
                Guid.Empty));
        Assert.Equal(DomainErrorCodes.InvalidSaleActor, ex.ErrorCode);
    }

    [Fact]
    public void Display_title_and_local_timestamp_helpers_are_user_facing()
    {
        Assert.Equal(StockCount.HistoricalTitle, StockCountDisplay.DisplayTitle(null));
        Assert.Equal("Weekly count", StockCountDisplay.DisplayTitle(" Weekly count "));

        var formatted = StockCountDisplay.FormatLocalTimestamp(Utc);
        Assert.DoesNotContain("T", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("Z", formatted, StringComparison.Ordinal);
        Assert.Contains("2026", formatted, StringComparison.Ordinal);
        Assert.Matches(new Regex(@"\b(AM|PM)\b"), formatted);
    }

    [Fact]
    public void Create_request_json_keeps_title_and_notes_separate()
    {
        var request = new CreateStockCountRequest(
            [new CreateStockCountLineRequest(ProductA.Value)],
            "Weekly count",
            Notes: "Counted after Friday closing.");
        var json = JsonSerializer.Serialize(request);
        Assert.Contains("\"Title\":\"Weekly count\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Notes\":\"Counted after Friday closing.\"", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Live_difference_matches_physical_count_minus_system_qty()
    {
        Assert.Equal(-1m, StockCountDisplay.LiveDifference(11m, "10"));
        Assert.Equal(1m, StockCountDisplay.LiveDifference(0m, "1"));
        Assert.Null(StockCountDisplay.LiveDifference(11m, ""));
    }
}
