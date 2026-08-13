using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.OperationalSetup;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Application.Reporting;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Domain.CashierShifts;
using ExItS.PinoyBusinessPOS.Domain.Common;
using ExItS.PinoyBusinessPOS.Domain.OperationalSetup;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCashierShiftCashCountApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Owner = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid Personal = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private const string Shifts = "/api/v1/pos/cashier-shifts";
    private const string Setup = "/api/v1/pos/operational-setup";
    private const string Sales = "/api/v1/pos/sales";
    private const string Products = "/api/v1/pos/catalog/products";
    private const string ShiftSummary = "/api/v1/pos/reports/shifts-summary";
    private const string Permissions = "/api/v1/pos/permissions";
    private static readonly Guid Cashier = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public async Task New_organization_defaults_to_required_cash_count()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var get = Scoped(HttpMethod.Get, Setup, org);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        var setup = await response.Content.ReadFromJsonAsync<PosOperationalSetupDto>(JsonOptions);
        Assert.Equal(nameof(CashCountMode.Required), setup!.CashCountMode);

        await CompleteSetupAsync(client, org, CashCountMode.Required);
        using var getCompleted = Scoped(HttpMethod.Get, Setup, org);
        using var completedResponse = await client.SendAsync(getCompleted);
        var completed = await completedResponse.Content.ReadFromJsonAsync<PosOperationalSetupDto>(JsonOptions);
        Assert.Equal(nameof(CashCountMode.Required), completed!.CashCountMode);

        using var off = Scoped(HttpMethod.Post, $"{Setup}/complete", org);
        off.Content = JsonContent.Create(
            new CompleteOperationalSetupRequest("Sari Sari Store", "PHP", "TaxExclusive", 0m, CashCountMode: nameof(CashCountMode.Off)),
            options: JsonOptions);
        using var offResponse = await client.SendAsync(off);
        // Already completed is idempotent success; Off is rejected on update.
        await UpdateCashCountModeAsync(client, org, CashCountMode.Optional);
        using var setOff = Scoped(HttpMethod.Put, Setup, org);
        var current = await GetSetupAsync(client, org);
        setOff.Content = JsonContent.Create(
            new UpdateOperationalSetupRequest(
                current.StoreDisplayName,
                current.CurrencyCode,
                current.TaxPricingMode,
                current.TaxRatePercent,
                current.UpdatedAtUtc,
                current.ReceiptHeader,
                current.ReceiptFooter,
                current.BusinessAddress,
                current.ContactPhone,
                nameof(CashCountMode.Off)),
            options: JsonOptions);
        using var setOffResponse = await client.SendAsync(setOff);
        Assert.Equal(HttpStatusCode.BadRequest, setOffResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.CashCountModeOffRetired, await ReadErrorCodeAsync(setOffResponse));
    }

    [Fact]
    public async Task Optional_allows_skip_while_required_is_enforced_and_snapshot_sticks()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        await CompleteSetupAsync(client, org, CashCountMode.Optional);
        var register = await PosShiftIntegrationSupport.EnsureRegisterAsync(client, org, Owner);

        using var openOptionalFirst = Scoped(HttpMethod.Post, Shifts, org);
        openOptionalFirst.Content = JsonContent.Create(new OpenCashierShiftRequest(register.RegisterId), options: JsonOptions);
        using var openOptionalFirstResponse = await client.SendAsync(openOptionalFirst);
        openOptionalFirstResponse.EnsureSuccessStatusCode();
        var optionalFirst = await openOptionalFirstResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal(nameof(CashCountMode.Optional), optionalFirst!.EffectiveCashCountMode);
        Assert.False(optionalFirst.OpeningCashCounted);

        var product = await CreateProductAsync(client, org, "Candy", "Piece", 10m, "cash-count-candy");
        var saleTotal = await CheckoutCashAsync(client, org, product.ProductId, 2m, 20m);
        Assert.Equal(20m, saleTotal);

        using var closeOptionalFirst = Scoped(HttpMethod.Post, $"{Shifts}/{optionalFirst.ShiftId:D}/close", org);
        closeOptionalFirst.Content = JsonContent.Create(new CloseCashierShiftRequest(ClosingCashAmount: null), options: JsonOptions);
        using var closeOptionalFirstResponse = await client.SendAsync(closeOptionalFirst);
        closeOptionalFirstResponse.EnsureSuccessStatusCode();
        var closedOptionalFirst = await closeOptionalFirstResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal("Closed", closedOptionalFirst!.Status);
        Assert.Null(closedOptionalFirst.ClosingCashAmount);
        Assert.Null(closedOptionalFirst.CashVarianceAmount);
        Assert.Equal(20m, closedOptionalFirst.ExpectedCashAmountSnapshot);
        Assert.Equal(CashCountStates.NotPerformed, closedOptionalFirst.ClosingCashCountState);
        using var openOptional = Scoped(HttpMethod.Post, Shifts, org);
        openOptional.Content = JsonContent.Create(new OpenCashierShiftRequest(register.RegisterId, 1000m), options: JsonOptions);
        using var openOptionalResponse = await client.SendAsync(openOptional);
        openOptionalResponse.EnsureSuccessStatusCode();
        var optionalShift = await openOptionalResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.True(optionalShift!.OpeningCashCounted);
        Assert.Equal(1000m, optionalShift.OpeningCashAmount);

        using var closeSkip = Scoped(HttpMethod.Post, $"{Shifts}/{optionalShift.ShiftId:D}/close", org);
        closeSkip.Content = JsonContent.Create(new CloseCashierShiftRequest(ClosingCashAmount: null), options: JsonOptions);
        using var closeSkipResponse = await client.SendAsync(closeSkip);
        closeSkipResponse.EnsureSuccessStatusCode();
        var skipped = await closeSkipResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Null(skipped!.ClosingCashAmount);
        Assert.Null(skipped.CashVarianceAmount);
        Assert.Equal(CashCountStates.NotPerformed, skipped.ClosingCashCountState);

        using var openSnapshot = Scoped(HttpMethod.Post, Shifts, org);
        openSnapshot.Content = JsonContent.Create(new OpenCashierShiftRequest(register.RegisterId), options: JsonOptions);
        using var openSnapshotResponse = await client.SendAsync(openSnapshot);
        openSnapshotResponse.EnsureSuccessStatusCode();
        var snapshotted = await openSnapshotResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal(nameof(CashCountMode.Optional), snapshotted!.EffectiveCashCountMode);

        await UpdateCashCountModeAsync(client, org, CashCountMode.Required);
        using var closeWhileRequiredOrg = Scoped(HttpMethod.Post, $"{Shifts}/{snapshotted.ShiftId:D}/close", org);
        closeWhileRequiredOrg.Content = JsonContent.Create(new CloseCashierShiftRequest(), options: JsonOptions);
        using var closeWhileRequiredResponse = await client.SendAsync(closeWhileRequiredOrg);
        closeWhileRequiredResponse.EnsureSuccessStatusCode();
        var closedUnderSnapshot = await closeWhileRequiredResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Null(closedUnderSnapshot!.ClosingCashAmount);

        using var openRequired = Scoped(HttpMethod.Post, Shifts, org);
        openRequired.Content = JsonContent.Create(new OpenCashierShiftRequest(register.RegisterId), options: JsonOptions);
        using var openRequiredResponse = await client.SendAsync(openRequired);
        Assert.Equal(HttpStatusCode.BadRequest, openRequiredResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.CashierShiftOpeningCashCountRequired, await ReadErrorCodeAsync(openRequiredResponse));

        using var openRequiredOk = Scoped(HttpMethod.Post, Shifts, org);
        openRequiredOk.Content = JsonContent.Create(new OpenCashierShiftRequest(register.RegisterId, 100m), options: JsonOptions);
        using var openRequiredOkResponse = await client.SendAsync(openRequiredOk);
        openRequiredOkResponse.EnsureSuccessStatusCode();
        var requiredShift = await openRequiredOkResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);

        using var closeRequiredMissing = Scoped(HttpMethod.Post, $"{Shifts}/{requiredShift!.ShiftId:D}/close", org);
        closeRequiredMissing.Content = JsonContent.Create(new CloseCashierShiftRequest(), options: JsonOptions);
        using var closeRequiredMissingResponse = await client.SendAsync(closeRequiredMissing);
        Assert.Equal(HttpStatusCode.BadRequest, closeRequiredMissingResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.CashierShiftClosingCashCountRequired, await ReadErrorCodeAsync(closeRequiredMissingResponse));

        using var closeRequiredOk = Scoped(HttpMethod.Post, $"{Shifts}/{requiredShift.ShiftId:D}/close", org);
        closeRequiredOk.Content = JsonContent.Create(new CloseCashierShiftRequest(40m), options: JsonOptions);
        using var closeRequiredOkResponse = await client.SendAsync(closeRequiredOk);
        closeRequiredOkResponse.EnsureSuccessStatusCode();
        var closedRequired = await closeRequiredOkResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal(40m, closedRequired!.ClosingCashAmount);
        Assert.Equal(-60m, closedRequired.CashVarianceAmount);
        Assert.Equal(CashCountStates.Counted, closedRequired.ClosingCashCountState);

        using var retryClose = Scoped(HttpMethod.Post, $"{Shifts}/{requiredShift.ShiftId:D}/close", org);
        retryClose.Content = JsonContent.Create(new CloseCashierShiftRequest(999m), options: JsonOptions);
        using var retryCloseResponse = await client.SendAsync(retryClose);
        retryCloseResponse.EnsureSuccessStatusCode();
        var retried = await retryCloseResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal(40m, retried!.ClosingCashAmount);
        Assert.Equal(-60m, retried.CashVarianceAmount);
    }

    [Fact]
    public async Task Short_close_does_not_mutate_sale_and_report_keeps_skip_null()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        await CompleteSetupAsync(client, org, CashCountMode.Optional);
        var register = await PosShiftIntegrationSupport.EnsureRegisterAsync(client, org, Owner);

        using var open = Scoped(HttpMethod.Post, Shifts, org);
        open.Content = JsonContent.Create(new OpenCashierShiftRequest(register.RegisterId, 1000m), options: JsonOptions);
        using var openResponse = await client.SendAsync(open);
        openResponse.EnsureSuccessStatusCode();
        var shift = await openResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);

        var product = await CreateProductAsync(client, org, "Soap", "Piece", 50m, "cash-count-soap");
        using var saleReq = Scoped(HttpMethod.Post, Sales, org);
        saleReq.Content = JsonContent.Create(
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(product.ProductId, 2m)], "Cash", AmountTendered: 100m),
            options: JsonOptions);
        using var saleResponse = await client.SendAsync(saleReq);
        saleResponse.EnsureSuccessStatusCode();
        var sale = await saleResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        var originalTotal = sale!.Total;

        using var close = Scoped(HttpMethod.Post, $"{Shifts}/{shift!.ShiftId:D}/close", org);
        close.Content = JsonContent.Create(new CloseCashierShiftRequest(1090m), options: JsonOptions);
        using var closeResponse = await client.SendAsync(close);
        closeResponse.EnsureSuccessStatusCode();
        var closed = await closeResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal(-10m, closed!.CashVarianceAmount);

        using var getSale = Scoped(HttpMethod.Get, $"{Sales}/{sale.SaleId:D}", org);
        using var getSaleResponse = await client.SendAsync(getSale);
        getSaleResponse.EnsureSuccessStatusCode();
        var reloaded = await getSaleResponse.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        Assert.Equal(originalTotal, reloaded!.Total);

        using var openSkip = Scoped(HttpMethod.Post, Shifts, org);
        openSkip.Content = JsonContent.Create(new OpenCashierShiftRequest(register.RegisterId), options: JsonOptions);
        using var openSkipResponse = await client.SendAsync(openSkip);
        openSkipResponse.EnsureSuccessStatusCode();
        var skipShift = await openSkipResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        using var closeSkip = Scoped(HttpMethod.Post, $"{Shifts}/{skipShift!.ShiftId:D}/close", org);
        closeSkip.Content = JsonContent.Create(new CloseCashierShiftRequest(ClosingCashAmount: null), options: JsonOptions);
        (await client.SendAsync(closeSkip)).EnsureSuccessStatusCode();

        using var report = Scoped(HttpMethod.Get, ShiftSummary, org);
        using var reportResponse = await client.SendAsync(report);
        reportResponse.EnsureSuccessStatusCode();
        var summary = await reportResponse.Content.ReadFromJsonAsync<PosShiftSummaryReportDto>(JsonOptions);
        var countedRow = Assert.Single(summary!.Rows, r => r.ShiftId == closed.ShiftId);
        Assert.Equal(-10m, countedRow.CashVarianceAmount);
        Assert.Equal(CashCountStates.Counted, countedRow.CashCountState);
        var skippedRow = Assert.Single(summary.Rows, r => r.ShiftId == skipShift.ShiftId);
        Assert.Null(skippedRow.CashVarianceAmount);
        Assert.Null(skippedRow.ClosingCashAmount);
        Assert.Equal(CashCountStates.NotPerformed, skippedRow.CashCountState);
        Assert.Equal(-10m, summary.TotalCashVariance);
    }

    [Fact]
    public async Task Wrong_organization_and_personal_user_cannot_change_cash_count_mode()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        await CompleteSetupAsync(client, orgA, CashCountMode.Optional);
        await CompleteSetupAsync(client, orgB, CashCountMode.Optional);

        var setupA = await GetSetupAsync(client, orgA);
        using var cross = Scoped(HttpMethod.Put, Setup, orgB);
        cross.Content = JsonContent.Create(
            new UpdateOperationalSetupRequest(
                setupA.StoreDisplayName,
                setupA.CurrencyCode,
                setupA.TaxPricingMode,
                setupA.TaxRatePercent,
                setupA.UpdatedAtUtc,
                setupA.ReceiptHeader,
                setupA.ReceiptFooter,
                setupA.BusinessAddress,
                setupA.ContactPhone,
                nameof(CashCountMode.Required)),
            options: JsonOptions);
        using var crossResponse = await client.SendAsync(cross);
        Assert.True(crossResponse.IsSuccessStatusCode || crossResponse.StatusCode == HttpStatusCode.Conflict);
        var setupAAfter = await GetSetupAsync(client, orgA);
        Assert.Equal(nameof(CashCountMode.Optional), setupAAfter.CashCountMode);
        var setupBAfter = await GetSetupAsync(client, orgB);
        Assert.NotEqual(nameof(CashCountMode.Required), setupBAfter.CashCountMode);

        using var personal = new HttpRequestMessage(HttpMethod.Put, Setup);
        personal.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.ActorHeaderName,
            Personal.ToString("D"));
        personal.Content = JsonContent.Create(
            new UpdateOperationalSetupRequest(
                "Personal",
                "PHP",
                "TaxExclusive",
                0m,
                DateTimeOffset.UtcNow,
                CashCountMode: nameof(CashCountMode.Required)),
            options: JsonOptions);
        using var personalResponse = await client.SendAsync(personal);
        Assert.Equal(HttpStatusCode.BadRequest, personalResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.OrganizationRequired, await ReadErrorCodeAsync(personalResponse));
    }

    [Fact]
    public async Task Denomination_seed_is_idempotent_and_custom_value_can_be_added()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        await CompleteSetupAsync(client, org, CashCountMode.Required);

        using var first = Scoped(HttpMethod.Get, $"{Setup}/cash-denominations", org);
        using var firstResponse = await client.SendAsync(first);
        firstResponse.EnsureSuccessStatusCode();
        var seeded = await firstResponse.Content.ReadFromJsonAsync<List<OrganizationCashDenominationDto>>(JsonOptions);
        Assert.Equal(
            PhilippineCashDenominationDefaults.Values.ToHashSet(),
            seeded!.Select(d => d.Value).ToHashSet());

        using var second = Scoped(HttpMethod.Get, $"{Setup}/cash-denominations", org);
        using var secondResponse = await client.SendAsync(second);
        var seededAgain = await secondResponse.Content.ReadFromJsonAsync<List<OrganizationCashDenominationDto>>(JsonOptions);
        Assert.Equal(seeded.Count, seededAgain!.Count);

        var items = seeded
            .Select(d => new CashDenominationWriteDto(d.Value, d.IsEnabled, d.SortOrder, d.DisplayLabel, d.DenominationId))
            .ToList();
        items.Add(new CashDenominationWriteDto(5000m, true, items.Count));
        using var replace = Scoped(HttpMethod.Put, $"{Setup}/cash-denominations", org);
        replace.Content = JsonContent.Create(new ReplaceCashDenominationsRequest(items), options: JsonOptions);
        using var replaceResponse = await client.SendAsync(replace);
        replaceResponse.EnsureSuccessStatusCode();
        var updated = await replaceResponse.Content.ReadFromJsonAsync<List<OrganizationCashDenominationDto>>(JsonOptions);
        Assert.Contains(updated!, d => d.Value == 5000m);

        items.Add(new CashDenominationWriteDto(5000m, true, items.Count));
        using var duplicate = Scoped(HttpMethod.Put, $"{Setup}/cash-denominations", org);
        duplicate.Content = JsonContent.Create(new ReplaceCashDenominationsRequest(items), options: JsonOptions);
        using var duplicateResponse = await client.SendAsync(duplicate);
        Assert.Equal(HttpStatusCode.BadRequest, duplicateResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.DuplicateCashDenomination, await ReadErrorCodeAsync(duplicateResponse));
    }

    [Fact]
    public async Task Denomination_assisted_open_and_close_recalculate_and_reject_mismatch()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        await CompleteSetupAsync(client, org, CashCountMode.Required);
        var register = await PosShiftIntegrationSupport.EnsureRegisterAsync(client, org, Owner);
        using var seed = Scoped(HttpMethod.Get, $"{Setup}/cash-denominations", org);
        (await client.SendAsync(seed)).EnsureSuccessStatusCode();

        var openingLines = new[]
        {
            new CashCountDenominationLineDto(1000m, 1),
            new CashCountDenominationLineDto(500m, 0)
        };
        using var mismatch = Scoped(HttpMethod.Post, Shifts, org);
        mismatch.Content = JsonContent.Create(
            new OpenCashierShiftRequest(register.RegisterId, 999m, DenominationLines: openingLines),
            options: JsonOptions);
        using var mismatchResponse = await client.SendAsync(mismatch);
        Assert.Equal(HttpStatusCode.BadRequest, mismatchResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.CashCountDenominationTotalMismatch, await ReadErrorCodeAsync(mismatchResponse));

        using var open = Scoped(HttpMethod.Post, Shifts, org);
        open.Content = JsonContent.Create(
            new OpenCashierShiftRequest(register.RegisterId, 1000m, DenominationLines: openingLines),
            options: JsonOptions);
        using var openResponse = await client.SendAsync(open);
        openResponse.EnsureSuccessStatusCode();
        var shift = await openResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal(1000m, shift!.OpeningCashAmount);
        Assert.Equal(1000m, Assert.Single(shift.OpeningDenominationLines!, l => l.Quantity > 0).DenominationValue);

        var closingLines = new[]
        {
            new CashCountDenominationLineDto(1000m, 2),
            new CashCountDenominationLineDto(500m, 3),
            new CashCountDenominationLineDto(100m, 10),
            new CashCountDenominationLineDto(50m, 5)
        };
        using var close = Scoped(HttpMethod.Post, $"{Shifts}/{shift.ShiftId:D}/close", org);
        close.Content = JsonContent.Create(
            new CloseCashierShiftRequest(4750m, DenominationLines: closingLines),
            options: JsonOptions);
        using var closeResponse = await client.SendAsync(close);
        closeResponse.EnsureSuccessStatusCode();
        var closed = await closeResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal(4750m, closed!.ClosingCashAmount);
        Assert.Equal(4, closed.ClosingDenominationLines!.Count);

        using var retry = Scoped(HttpMethod.Post, $"{Shifts}/{shift.ShiftId:D}/close", org);
        retry.Content = JsonContent.Create(
            new CloseCashierShiftRequest(1m, DenominationLines: [new CashCountDenominationLineDto(1000m, 1)]),
            options: JsonOptions);
        using var retryResponse = await client.SendAsync(retry);
        retryResponse.EnsureSuccessStatusCode();
        var retried = await retryResponse.Content.ReadFromJsonAsync<PosCashierShiftDto>(JsonOptions);
        Assert.Equal(4750m, retried!.ClosingCashAmount);
        Assert.Equal(4, retried.ClosingDenominationLines!.Count);
    }

    [Fact]
    public async Task Cashier_cannot_replace_denominations_and_cross_org_lines_are_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();
        await CompleteSetupAsync(client, orgA, CashCountMode.Required);
        await CompleteSetupAsync(client, orgB, CashCountMode.Required);
        using var bootstrap = Scoped(HttpMethod.Get, $"{Permissions}/effective", orgA);
        (await client.SendAsync(bootstrap)).EnsureSuccessStatusCode();
        using var assign = Scoped(HttpMethod.Post, $"{Permissions}/assignments", orgA);
        assign.Content = JsonContent.Create(new AssignPosRoleRequest(Cashier, "Cashier"), options: JsonOptions);
        (await client.SendAsync(assign)).EnsureSuccessStatusCode();

        using var seedA = Scoped(HttpMethod.Get, $"{Setup}/cash-denominations", orgA);
        (await client.SendAsync(seedA)).EnsureSuccessStatusCode();

        using var cashier = new HttpRequestMessage(HttpMethod.Put, $"{Setup}/cash-denominations");
        cashier.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.OrganizationHeaderName,
            orgA.ToString("D"));
        cashier.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.ActorHeaderName,
            Cashier.ToString("D"));
        cashier.Content = JsonContent.Create(
            new ReplaceCashDenominationsRequest([new CashDenominationWriteDto(5000m)]),
            options: JsonOptions);
        using var cashierResponse = await client.SendAsync(cashier);
        Assert.Equal(HttpStatusCode.Forbidden, cashierResponse.StatusCode);

        var register = await PosShiftIntegrationSupport.EnsureRegisterAsync(client, orgB, Owner);
        using var open = Scoped(HttpMethod.Post, Shifts, orgB);
        open.Content = JsonContent.Create(
            new OpenCashierShiftRequest(
                register.RegisterId,
                9000m,
                DenominationLines: [new CashCountDenominationLineDto(9000m, 1)]),
            options: JsonOptions);
        using var openResponse = await client.SendAsync(open);
        Assert.Equal(HttpStatusCode.BadRequest, openResponse.StatusCode);
        Assert.Equal(DomainErrorCodes.CashCountDenominationNotConfigured, await ReadErrorCodeAsync(openResponse));
    }

    private static async Task CompleteSetupAsync(HttpClient client, Guid org, CashCountMode mode)
    {
        using var complete = Scoped(HttpMethod.Post, $"{Setup}/complete", org);
        complete.Content = JsonContent.Create(
            new CompleteOperationalSetupRequest(
                "Sari Sari Store",
                "PHP",
                "TaxExclusive",
                0m,
                CashCountMode: mode.ToString()),
            options: JsonOptions);
        using var response = await client.SendAsync(complete);
        response.EnsureSuccessStatusCode();
    }

    private static async Task UpdateCashCountModeAsync(HttpClient client, Guid org, CashCountMode mode)
    {
        var setup = await GetSetupAsync(client, org);
        using var update = Scoped(HttpMethod.Put, Setup, org);
        update.Content = JsonContent.Create(
            new UpdateOperationalSetupRequest(
                setup.StoreDisplayName,
                setup.CurrencyCode,
                setup.TaxPricingMode,
                setup.TaxRatePercent,
                setup.UpdatedAtUtc,
                setup.ReceiptHeader,
                setup.ReceiptFooter,
                setup.BusinessAddress,
                setup.ContactPhone,
                mode.ToString()),
            options: JsonOptions);
        using var response = await client.SendAsync(update);
        response.EnsureSuccessStatusCode();
    }

    private static async Task<PosOperationalSetupDto> GetSetupAsync(HttpClient client, Guid org)
    {
        using var get = Scoped(HttpMethod.Get, Setup, org);
        using var response = await client.SendAsync(get);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosOperationalSetupDto>(JsonOptions))!;
    }

    private static async Task<decimal> CheckoutCashAsync(
        HttpClient client,
        Guid org,
        Guid productId,
        decimal qty,
        decimal tendered)
    {
        using var saleReq = Scoped(HttpMethod.Post, Sales, org);
        saleReq.Content = JsonContent.Create(
            new CheckoutSaleRequest([new CheckoutSaleLineRequest(productId, qty)], "Cash", AmountTendered: tendered),
            options: JsonOptions);
        using var response = await client.SendAsync(saleReq);
        response.EnsureSuccessStatusCode();
        var sale = await response.Content.ReadFromJsonAsync<PosSaleDto>(JsonOptions);
        return sale!.Total;
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        string name,
        string uom,
        decimal price,
        string sku)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(
            new CreatePosCatalogProductRequest(name, uom, price, null, sku),
            options: JsonOptions);
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions))!;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private static HttpRequestMessage Scoped(HttpMethod method, string path, Guid organizationId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));
        request.Headers.TryAddWithoutValidation(
            ExItS.PinoyBusinessPOS.Api.Common.PosOrganizationHeaders.ActorHeaderName,
            Owner.ToString("D"));
        return request;
    }

    private sealed class PosApiFactory(string connectionString) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", connectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = connectionString
                });
            });
        }
    }
}
