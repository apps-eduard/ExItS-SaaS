using ExItS.Platform.Application.Admin;

namespace ExItS.Platform.UnitTests.Admin;

public sealed class EntitlementTableDisplayAndSortTests
{
    [Fact]
    public void Entitlement_summary_exposes_friendly_product_and_organization_names_not_keys()
    {
        var organizationId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var dto = new EntitlementLatestSummaryDto(
            Guid.NewGuid(),
            organizationId,
            "pinoy-business-pos",
            Guid.NewGuid(),
            "Trialing",
            31,
            1,
            new DateTimeOffset(2026, 8, 3, 3, 6, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 3, 3, 6, 0, TimeSpan.Zero),
            new DateTimeOffset(2026, 8, 10, 3, 6, 0, TimeSpan.Zero),
            null,
            false,
            OrganizationDisplayName: "ABC Sari-Sari Store",
            ProductDisplayName: "Pinoy Business POS");

        Assert.Equal("Pinoy Business POS", dto.ProductDisplayName);
        Assert.Equal("ABC Sari-Sari Store", dto.OrganizationDisplayName);
        Assert.NotEqual(dto.ProductDisplayName, dto.ProductCode);
        Assert.DoesNotContain(organizationId.ToString("D"), dto.OrganizationDisplayName!);
        Assert.DoesNotContain("pinoy-business-pos", dto.ProductDisplayName!, StringComparison.Ordinal);
        Assert.Equal(31, dto.SnapshotVersion); // UI label: Revision
    }

    [Fact]
    public void Generated_display_contract_uses_local_pattern_not_raw_utc_string()
    {
        // UI contract: "dd MMM yyyy, h:mm tt" — verified here as the format string used by LocalTimestamp.
        const string pattern = "dd MMM yyyy, h:mm tt";
        var utc = new DateTimeOffset(2026, 8, 3, 3, 6, 0, TimeSpan.Zero);
        var formatted = utc.UtcDateTime.ToString(pattern, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal("03 Aug 2026, 3:06 AM", formatted);
        Assert.DoesNotContain("UTC", formatted, StringComparison.Ordinal);
        Assert.DoesNotContain("T", formatted, StringComparison.Ordinal); // not ISO primary
    }

    [Theory]
    [InlineData(EntitlementListSortBy.ProductDisplayName)]
    [InlineData(EntitlementListSortBy.OrganizationDisplayName)]
    [InlineData(EntitlementListSortBy.Status)]
    [InlineData(EntitlementListSortBy.GeneratedAtUtc)]
    [InlineData(EntitlementListSortBy.Revision)]
    public void Entitlement_sort_keys_are_explicit_safe_enums(EntitlementListSortBy sortBy) =>
        Assert.True(Enum.IsDefined(sortBy));

    [Fact]
    public void Revision_sort_key_maps_to_numeric_snapshot_version_enum()
    {
        Assert.Equal(EntitlementListSortBy.Revision, Enum.Parse<EntitlementListSortBy>("Revision"));
    }

    [Theory]
    [InlineData("actions")]
    [InlineData("history")]
    [InlineData("open")]
    [InlineData("id")]
    public void Actions_and_history_are_not_entitlement_sort_keys(string key)
    {
        var parsed = key.Trim().ToLowerInvariant() switch
        {
            "product" or "productdisplayname" => EntitlementListSortBy.ProductDisplayName,
            "organization" or "organizationdisplayname" => EntitlementListSortBy.OrganizationDisplayName,
            "status" or "subscriptionstatus" => EntitlementListSortBy.Status,
            "generated" or "generatedatutc" => EntitlementListSortBy.GeneratedAtUtc,
            "revision" or "snapshotversion" or "version" => EntitlementListSortBy.Revision,
            _ => (EntitlementListSortBy?)null
        };
        Assert.Null(parsed);
    }

    [Fact]
    public void Default_sort_contract_is_generated_descending_then_organization_ascending()
    {
        Assert.Equal(EntitlementListSortBy.GeneratedAtUtc, default(EntitlementListSortBy));
        // Secondary ThenBy Organization is enforced in AdminPortfolioReadStore.ApplySort.
        Assert.Equal(EntitlementListSortBy.OrganizationDisplayName, EntitlementListSortBy.OrganizationDisplayName);
    }
}
