using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.Customers;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using ExItS.PinoyBusinessPOS.Domain.Registers;

namespace ExItS.PinoyBusinessPOS.UnitTests.CashierShifts;

public sealed class CashCountDenominationTests
{
    private static readonly Guid Actor = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly PosOrganizationId Org = PosOrganizationId.From(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));
    private static readonly RegisterId Register = RegisterId.From(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"));
    private static readonly DateTimeOffset Now = new(2026, 8, 13, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Default_php_seed_includes_current_useful_denominations()
    {
        Assert.Equal(
            [1000.00m, 500.00m, 200.00m, 100.00m, 50.00m, 20.00m, 10.00m, 5.00m, 1.00m, 0.25m, 0.10m, 0.05m],
            PhilippineCashDenominationDefaults.Values);
        Assert.Contains(0.25m, PhilippineCashDenominationDefaults.Values);
        Assert.Contains(0.10m, PhilippineCashDenominationDefaults.Values);
        Assert.Contains(0.05m, PhilippineCashDenominationDefaults.Values);
        Assert.DoesNotContain(0.01m, PhilippineCashDenominationDefaults.Values);
        Assert.DoesNotContain(0.50m, PhilippineCashDenominationDefaults.Values);
    }

    [Fact]
    public void Centavo_line_totals_use_decimal_money_without_truncation()
    {
        Assert.Equal(0.25m, CashCountDenominationLine.Create(0.25m, 1).LineTotal);
        Assert.Equal(0.75m, CashCountDenominationLine.Create(0.25m, 3).LineTotal);
        Assert.Equal(0.05m, CashCountDenominationLine.Create(0.05m, 1).LineTotal);
        Assert.Equal(0.15m, CashCountDenominationLine.Create(0.05m, 3).LineTotal);
        Assert.Equal(0.50m, CashCountDenominationLine.Create(0.05m, 10).LineTotal);
        Assert.Equal(0.01m, CashCountDenominationLine.Create(0.01m, 1).LineTotal);
        Assert.Equal(0.05m, CashCountDenominationLine.Create(0.01m, 5).LineTotal);
        Assert.Equal(0.25m, CashCountDenominationLine.Create(0.01m, 25).LineTotal);
    }

    [Fact]
    public void Mixed_centavo_count_totals_correctly()
    {
        var lines = new[]
        {
            CashCountDenominationLine.Create(1.00m, 3),
            CashCountDenominationLine.Create(0.25m, 2),
            CashCountDenominationLine.Create(0.05m, 3),
            CashCountDenominationLine.Create(0.01m, 5)
        };

        Assert.Equal(3.70m, CashCountDenominationBreakdown.Recalculate(lines));
        Assert.Equal(lines, CashCountDenominationBreakdown.EnsureMatchesSubmittedTotal(3.70m, lines));
    }

    [Fact]
    public void Recalculate_matches_authoritative_total()
    {
        var lines = new[]
        {
            CashCountDenominationLine.Create(1000m, 2),
            CashCountDenominationLine.Create(500m, 3),
            CashCountDenominationLine.Create(100m, 10),
            CashCountDenominationLine.Create(50m, 5)
        };

        Assert.Equal(4750m, CashCountDenominationBreakdown.Recalculate(lines));
        Assert.Equal(lines, CashCountDenominationBreakdown.EnsureMatchesSubmittedTotal(4750m, lines));
    }

    [Fact]
    public void Zero_quantity_is_allowed_and_negative_is_rejected()
    {
        var zero = CashCountDenominationLine.Create(1000m, 0);
        Assert.Equal(0m, zero.LineTotal);
        var ex = Assert.Throws<DomainException>(() => CashCountDenominationLine.Create(1000m, -1));
        Assert.Equal(DomainErrorCodes.InvalidCashDenominationQuantity, ex.ErrorCode);
    }

    [Fact]
    public void Zero_and_negative_denomination_values_are_rejected()
    {
        Assert.Equal(DomainErrorCodes.InvalidCashDenominationValue,
            Assert.Throws<DomainException>(() => OrganizationCashDenomination.NormalizeValue(0m)).ErrorCode);
        Assert.Equal(DomainErrorCodes.InvalidCashDenominationValue,
            Assert.Throws<DomainException>(() => OrganizationCashDenomination.NormalizeValue(-5m)).ErrorCode);
    }

    [Fact]
    public void Duplicate_lines_and_total_mismatch_are_rejected()
    {
        var duplicate = Assert.Throws<DomainException>(() => CashCountDenominationBreakdown.Normalize(
        [
            CashCountDenominationLine.Create(1000m, 1),
            CashCountDenominationLine.Create(1000m, 2)
        ]));
        Assert.Equal(DomainErrorCodes.CashCountDenominationDuplicateLine, duplicate.ErrorCode);

        var mismatch = Assert.Throws<DomainException>(() => CashCountDenominationBreakdown.EnsureMatchesSubmittedTotal(
            100m,
            [CashCountDenominationLine.Create(1000m, 1)]));
        Assert.Equal(DomainErrorCodes.CashCountDenominationTotalMismatch, mismatch.ErrorCode);
    }

    [Fact]
    public void Disabled_or_unknown_denomination_is_rejected_for_new_counts()
    {
        var enabled = new HashSet<decimal> { 1000m, 500m };
        var ex = Assert.Throws<DomainException>(() => CashCountDenominationBreakdown.EnsureConfigured(
            [CashCountDenominationLine.Create(5000m, 1)],
            enabled));
        Assert.Equal(DomainErrorCodes.CashCountDenominationNotConfigured, ex.ErrorCode);
    }

    [Fact]
    public void Historical_breakdown_keeps_original_values_after_config_change()
    {
        var shift = CashierShift.Open(
            Org,
            "SHIFT-20260813-000001",
            Actor,
            Register,
            2000m,
            Now,
            cashCountMode: CashCountMode.Required,
            openingDenominationLines: [CashCountDenominationLine.Create(1000m, 2)]);

        Assert.Equal(2000m, shift.OpeningCashAmount);
        Assert.Equal(1000m, Assert.Single(shift.OpeningDenominationLines).DenominationValue);
        Assert.Equal(2, shift.OpeningDenominationLines[0].Quantity);
    }

    [Fact]
    public void Custom_future_denomination_works_without_hard_coded_ui_list()
    {
        var fiveThousand = OrganizationCashDenomination.Create(Org, 5000m, 0, Now);
        var fiftyCentavos = OrganizationCashDenomination.Create(Org, 0.50m, 1, Now);
        var tenCentavos = OrganizationCashDenomination.Create(Org, 0.10m, 2, Now);
        Assert.Equal(5000m, fiveThousand.Value);
        Assert.Equal(0.50m, fiftyCentavos.Value);
        Assert.Equal(0.10m, tenCentavos.Value);
        Assert.Equal(5000m, CashCountDenominationBreakdown.Recalculate([CashCountDenominationLine.Create(5000m, 1)]));
        Assert.Equal(0.50m, CashCountDenominationBreakdown.Recalculate([CashCountDenominationLine.Create(0.50m, 1)]));
    }

    [Fact]
    public void Opening_denomination_total_becomes_opening_cash_on_hand()
    {
        var lines = new[]
        {
            CashCountDenominationLine.Create(1000m, 1),
            CashCountDenominationLine.Create(500m, 1),
            CashCountDenominationLine.Create(1m, 3),
            CashCountDenominationLine.Create(0.25m, 2),
            CashCountDenominationLine.Create(0.05m, 3),
            CashCountDenominationLine.Create(0.01m, 5)
        };
        Assert.Equal(1503.70m, CashCountDenominationBreakdown.Recalculate(lines));

        var shift = CashierShift.Open(
            Org,
            "SHIFT-20260813-000010",
            Actor,
            Register,
            1503.70m,
            Now,
            cashCountMode: CashCountMode.Required,
            openingDenominationLines: lines);

        Assert.True(shift.OpeningCashCounted);
        Assert.Equal(1503.70m, shift.OpeningCashAmount);
        Assert.Equal(6, shift.OpeningDenominationLines.Count);
        Assert.Equal(0.50m, shift.OpeningDenominationLines.Single(l => l.DenominationValue == 0.25m).LineTotal);
        Assert.Equal(0.15m, shift.OpeningDenominationLines.Single(l => l.DenominationValue == 0.05m).LineTotal);
        Assert.Equal(0.05m, shift.OpeningDenominationLines.Single(l => l.DenominationValue == 0.01m).LineTotal);
    }

    [Fact]
    public void Closing_denomination_total_becomes_counted_cash_and_does_not_change_expected_formula()
    {
        var openingLines = new[]
        {
            CashCountDenominationLine.Create(1000m, 1),
            CashCountDenominationLine.Create(500m, 1),
            CashCountDenominationLine.Create(1m, 3),
            CashCountDenominationLine.Create(0.25m, 2),
            CashCountDenominationLine.Create(0.05m, 3),
            CashCountDenominationLine.Create(0.01m, 5)
        };
        var shift = CashierShift.Open(
            Org,
            "SHIFT-20260813-000011",
            Actor,
            Register,
            1503.70m,
            Now,
            cashCountMode: CashCountMode.Required,
            openingDenominationLines: openingLines);

        var closingLines = new[]
        {
            CashCountDenominationLine.Create(1000m, 5),
            CashCountDenominationLine.Create(100m, 2),
            CashCountDenominationLine.Create(1m, 3),
            CashCountDenominationLine.Create(0.25m, 2),
            CashCountDenominationLine.Create(0.05m, 3),
            CashCountDenominationLine.Create(0.01m, 5)
        };
        Assert.Equal(5203.70m, CashCountDenominationBreakdown.Recalculate(closingLines));

        var expectedCash = 1503.70m;
        shift.Close(5203.70m, expectedCash, Actor, Now.AddHours(8), closingDenominationLines: closingLines);

        Assert.Equal(5203.70m, shift.ClosingCashAmount);
        Assert.Equal(expectedCash, shift.ExpectedCashAmountSnapshot);
        Assert.Equal(3700.00m, shift.CashVarianceAmount);
        Assert.Equal(0.50m, shift.ClosingDenominationLines.Single(l => l.DenominationValue == 0.25m).LineTotal);
        Assert.Equal(0.15m, shift.ClosingDenominationLines.Single(l => l.DenominationValue == 0.05m).LineTotal);
        Assert.Equal(0.05m, shift.ClosingDenominationLines.Single(l => l.DenominationValue == 0.01m).LineTotal);
    }

    [Fact]
    public void Manual_total_has_no_breakdown()
    {
        var shift = CashierShift.Open(Org, "SHIFT-20260813-000002", Actor, Register, 1000m, Now);
        Assert.Empty(shift.OpeningDenominationLines);
        shift.Close(4750m, 1000m, Actor, Now.AddHours(8));
        Assert.Empty(shift.ClosingDenominationLines);
        Assert.Equal(4750m, shift.ClosingCashAmount);
        Assert.Equal(3750m, shift.CashVarianceAmount);
    }

    [Fact]
    public async Task Seeder_creates_full_default_set_when_empty()
    {
        var repo = new InMemoryCashDenominationRepository();
        await DefaultCashDenominationSeeder.EnsureAsync(repo, Org, Now);

        Assert.Equal(
            PhilippineCashDenominationDefaults.Values,
            repo.Items.OrderBy(d => d.SortOrder).Select(d => d.Value).ToArray());
    }

    [Fact]
    public async Task Seeder_appends_missing_centavo_defaults_without_removing_existing()
    {
        var repo = new InMemoryCashDenominationRepository();
        var pesoOnly = new[] { 1000m, 500m, 200m, 100m, 50m, 20m, 10m, 5m, 1m }
            .Select((value, index) => OrganizationCashDenomination.Create(Org, value, index, Now))
            .ToList();
        await repo.ReplaceAsync(Org, pesoOnly);

        await DefaultCashDenominationSeeder.EnsureAsync(repo, Org, Now);

        var values = repo.Items.Select(d => d.Value).ToHashSet();
        Assert.Equal(9 + 3, values.Count);
        Assert.Contains(0.25m, values);
        Assert.Contains(0.10m, values);
        Assert.Contains(0.05m, values);
        Assert.DoesNotContain(0.01m, values);
        Assert.Contains(1000m, values);
        Assert.Equal(9, repo.Items.Count(d => d.SortOrder < 9));
    }

    [Fact]
    public async Task Seeder_is_idempotent_when_defaults_already_present()
    {
        var repo = new InMemoryCashDenominationRepository();
        await DefaultCashDenominationSeeder.EnsureAsync(repo, Org, Now);
        var firstCount = repo.Items.Count;

        await DefaultCashDenominationSeeder.EnsureAsync(repo, Org, Now);

        Assert.Equal(firstCount, repo.Items.Count);
        Assert.Equal(PhilippineCashDenominationDefaults.Values.Length, firstCount);
    }

    private sealed class InMemoryCashDenominationRepository : IOrganizationCashDenominationRepository
    {
        public List<OrganizationCashDenomination> Items { get; } = [];

        public Task<IReadOnlyList<OrganizationCashDenomination>> ListAsync(
            PosOrganizationId organizationId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationCashDenomination>>(Items.ToList());

        public Task ReplaceAsync(
            PosOrganizationId organizationId,
            IReadOnlyList<OrganizationCashDenomination> denominations,
            CancellationToken cancellationToken = default)
        {
            Items.Clear();
            Items.AddRange(denominations);
            return Task.CompletedTask;
        }
    }
}
