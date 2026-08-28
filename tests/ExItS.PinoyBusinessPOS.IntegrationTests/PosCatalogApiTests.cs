using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCatalogApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string Categories = "/api/v1/pos/catalog/categories";
    private const string Brands = "/api/v1/pos/catalog/brands";
    private const string Products = "/api/v1/pos/catalog/products";

    [Fact]
    public async Task Product_lifecycle_search_and_cross_organization_isolation()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        var beverages = await CreateCategoryAsync(client, orgA, "Beverages");
        Assert.Equal("Active", beverages.Status);

        using var createRequest = Scoped(HttpMethod.Post, Products, orgA);
        createRequest.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "  Kopiko Black 3in1  ",
            "Sachet",
            8.50m,
            "Instant coffee mix",
            " kop-blk-3in1 ",
            "4006381333931",
            beverages.CategoryId));
        using var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Kopiko Black 3in1", created!.Name);
        Assert.Equal("kop-blk-3in1", created.Sku);
        Assert.Equal("4006381333931", created.Barcode);
        Assert.Equal(8.50m, created.SellingPrice);
        Assert.Equal("Sachet", created.UnitOfMeasure);
        Assert.Equal(beverages.CategoryId, created.CategoryId);
        Assert.Equal(orgA, created.OrganizationId);

        using var searchByName = Scoped(HttpMethod.Get, $"{Products}?search=kopiko", orgA);
        using var searchByNameResponse = await client.SendAsync(searchByName);
        searchByNameResponse.EnsureSuccessStatusCode();
        var byName = await searchByNameResponse.Content.ReadFromJsonAsync<PagedResult<PosCatalogProductDto>>(JsonOptions);
        Assert.Contains(byName!.Items, p => p.ProductId == created.ProductId);

        using var searchBySku = Scoped(HttpMethod.Get, $"{Products}?search=KOP-BLK", orgA);
        using var searchBySkuResponse = await client.SendAsync(searchBySku);
        searchBySkuResponse.EnsureSuccessStatusCode();
        var bySku = await searchBySkuResponse.Content.ReadFromJsonAsync<PagedResult<PosCatalogProductDto>>(JsonOptions);
        Assert.Contains(bySku!.Items, p => p.ProductId == created.ProductId);

        using var filtered = Scoped(
            HttpMethod.Get,
            $"{Products}?status=Active&categoryId={beverages.CategoryId:D}&unitOfMeasure=Sachet",
            orgA);
        using var filteredResponse = await client.SendAsync(filtered);
        filteredResponse.EnsureSuccessStatusCode();
        var filteredPage = await filteredResponse.Content.ReadFromJsonAsync<PagedResult<PosCatalogProductDto>>(JsonOptions);
        Assert.Contains(filteredPage!.Items, p => p.ProductId == created.ProductId);

        using var reread = Scoped(HttpMethod.Get, $"{Products}/{created.ProductId:D}", orgA);
        using var rereadResponse = await client.SendAsync(reread);
        rereadResponse.EnsureSuccessStatusCode();
        var persisted = await rereadResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);

        using var update = Scoped(HttpMethod.Put, $"{Products}/{created.ProductId:D}", orgA);
        update.Content = JsonContent.Create(new UpdatePosCatalogProductRequest(
            "Kopiko Black 3in1 Twin",
            "Pack",
            16m,
            null,
            "kop-blk-3in1",
            "4006381333931",
            beverages.CategoryId,
            ExpectedUpdatedAtUtc: persisted!.UpdatedAtUtc));
        using var updateResponse = await client.SendAsync(update);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal("Kopiko Black 3in1 Twin", updated!.Name);
        Assert.Equal("Pack", updated.UnitOfMeasure);
        Assert.Equal(16m, updated.SellingPrice);

        using var staleUpdate = Scoped(HttpMethod.Put, $"{Products}/{created.ProductId:D}", orgA);
        staleUpdate.Content = JsonContent.Create(new UpdatePosCatalogProductRequest(
            "Stale",
            "Pack",
            16m,
            null,
            "kop-blk-3in1",
            "4006381333931",
            beverages.CategoryId,
            ExpectedUpdatedAtUtc: persisted.UpdatedAtUtc.AddSeconds(-30)));
        using var staleResponse = await client.SendAsync(staleUpdate);
        Assert.Equal(HttpStatusCode.Conflict, staleResponse.StatusCode);
        Assert.Equal(
            ApplicationErrorCodes.CatalogConcurrencyConflict,
            await ReadErrorCodeAsync(staleResponse));

        using var crossGet = Scoped(HttpMethod.Get, $"{Products}/{created.ProductId:D}", orgB);
        using var crossGetResponse = await client.SendAsync(crossGet);
        Assert.Equal(HttpStatusCode.NotFound, crossGetResponse.StatusCode);

        using var crossUpdate = Scoped(HttpMethod.Put, $"{Products}/{created.ProductId:D}", orgB);
        crossUpdate.Content = JsonContent.Create(new UpdatePosCatalogProductRequest("Hijacked", "Piece", 1m));
        using var crossUpdateResponse = await client.SendAsync(crossUpdate);
        Assert.Equal(HttpStatusCode.NotFound, crossUpdateResponse.StatusCode);

        // Other organizations may reuse the same SKU and barcode.
        using var sameIdentifiersElsewhere = Scoped(HttpMethod.Post, Products, orgB);
        sameIdentifiersElsewhere.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Kopiko Black 3in1",
            "Sachet",
            8.50m,
            null,
            "kop-blk-3in1",
            "4006381333931"));
        using var sameIdentifiersResponse = await client.SendAsync(sameIdentifiersElsewhere);
        Assert.Equal(HttpStatusCode.Created, sameIdentifiersResponse.StatusCode);
    }

    [Fact]
    public async Task Duplicate_sku_and_barcode_conflict_and_stay_reserved_while_inactive()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(
            "Rice 25kg",
            "Kilogram",
            1450m,
            null,
            "RC-25KG",
            "96385074"));

        using var duplicateSku = Scoped(HttpMethod.Post, Products, org);
        duplicateSku.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Rice clone",
            "Kilogram",
            1450m,
            null,
            "rc-25kg"));
        using var duplicateSkuResponse = await client.SendAsync(duplicateSku);
        Assert.Equal(HttpStatusCode.Conflict, duplicateSkuResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.ProductSkuConflict, await ReadErrorCodeAsync(duplicateSkuResponse));

        using var duplicateBarcode = Scoped(HttpMethod.Post, Products, org);
        duplicateBarcode.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Rice clone",
            "Kilogram",
            1450m,
            null,
            null,
            "96385074"));
        using var duplicateBarcodeResponse = await client.SendAsync(duplicateBarcode);
        Assert.Equal(HttpStatusCode.Conflict, duplicateBarcodeResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.ProductBarcodeConflict, await ReadErrorCodeAsync(duplicateBarcodeResponse));

        using var deactivate = Scoped(HttpMethod.Post, $"{Products}/{product.ProductId:D}/deactivate", org);
        using var deactivateResponse = await client.SendAsync(deactivate);
        deactivateResponse.EnsureSuccessStatusCode();
        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal("Inactive", deactivated!.Status);

        using var reuseAfterDeactivate = Scoped(HttpMethod.Post, Products, org);
        reuseAfterDeactivate.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Rice clone",
            "Kilogram",
            1450m,
            null,
            "RC-25KG"));
        using var reuseResponse = await client.SendAsync(reuseAfterDeactivate);
        Assert.Equal(HttpStatusCode.Conflict, reuseResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.ProductSkuConflict, await ReadErrorCodeAsync(reuseResponse));

        using var reactivate = Scoped(HttpMethod.Post, $"{Products}/{product.ProductId:D}/reactivate", org);
        using var reactivateResponse = await client.SendAsync(reactivate);
        reactivateResponse.EnsureSuccessStatusCode();
        var reactivated = await reactivateResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal("Active", reactivated!.Status);
    }

    [Fact]
    public async Task Lookup_by_sku_and_barcode_defaults_to_active_only()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(
            "Lucky Me Pancit Canton",
            "Piece",
            15m,
            null,
            "LM-PC-80",
            "036000291452"));

        using var bySku = Scoped(HttpMethod.Get, $"{Products}/by-sku/lm-pc-80", org);
        using var bySkuResponse = await client.SendAsync(bySku);
        bySkuResponse.EnsureSuccessStatusCode();
        var skuMatch = await bySkuResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal(product.ProductId, skuMatch!.ProductId);

        using var byBarcode = Scoped(HttpMethod.Get, $"{Products}/by-barcode/036000291452", org);
        using var byBarcodeResponse = await client.SendAsync(byBarcode);
        byBarcodeResponse.EnsureSuccessStatusCode();
        var barcodeMatch = await byBarcodeResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal(product.ProductId, barcodeMatch!.ProductId);

        using var deactivate = Scoped(HttpMethod.Post, $"{Products}/{product.ProductId:D}/deactivate", org);
        (await client.SendAsync(deactivate)).EnsureSuccessStatusCode();

        using var inactiveLookup = Scoped(HttpMethod.Get, $"{Products}/by-barcode/036000291452", org);
        using var inactiveLookupResponse = await client.SendAsync(inactiveLookup);
        Assert.Equal(HttpStatusCode.NotFound, inactiveLookupResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.ProductNotFound, await ReadErrorCodeAsync(inactiveLookupResponse));

        using var includeInactive = Scoped(
            HttpMethod.Get,
            $"{Products}/by-barcode/036000291452?includeInactive=true",
            org);
        using var includeInactiveResponse = await client.SendAsync(includeInactive);
        includeInactiveResponse.EnsureSuccessStatusCode();
        var inactiveMatch = await includeInactiveResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal("Inactive", inactiveMatch!.Status);

        using var unknown = Scoped(HttpMethod.Get, $"{Products}/by-sku/does-not-exist", org);
        using var unknownResponse = await client.SendAsync(unknown);
        Assert.Equal(HttpStatusCode.NotFound, unknownResponse.StatusCode);
    }

    [Fact]
    public async Task Invalid_barcode_price_and_unit_are_rejected()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var badCheckDigit = Scoped(HttpMethod.Post, Products, org);
        badCheckDigit.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Bad barcode", "Piece", 1m, null, null, "4006381333932"));
        using var badCheckDigitResponse = await client.SendAsync(badCheckDigit);
        Assert.Equal(HttpStatusCode.BadRequest, badCheckDigitResponse.StatusCode);

        using var negativePrice = Scoped(HttpMethod.Post, Products, org);
        negativePrice.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Negative", "Piece", -1m));
        using var negativePriceResponse = await client.SendAsync(negativePrice);
        Assert.Equal(HttpStatusCode.BadRequest, negativePriceResponse.StatusCode);

        using var unknownUnit = Scoped(HttpMethod.Post, Products, org);
        unknownUnit.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Crate goods", "Crate", 1m));
        using var unknownUnitResponse = await client.SendAsync(unknownUnit);
        Assert.Equal(HttpStatusCode.BadRequest, unknownUnitResponse.StatusCode);
    }

    [Fact]
    public async Task Category_lifecycle_enforces_active_name_uniqueness_and_assignment_rules()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var snacks = await CreateCategoryAsync(client, org, "Snacks");

        using var duplicate = Scoped(HttpMethod.Post, Categories, org);
        duplicate.Content = JsonContent.Create(new CreatePosProductCategoryRequest("  snacks  "));
        using var duplicateResponse = await client.SendAsync(duplicate);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.CategoryNameConflict, await ReadErrorCodeAsync(duplicateResponse));

        using var deactivate = Scoped(HttpMethod.Post, $"{Categories}/{snacks.CategoryId:D}/deactivate", org);
        using var deactivateResponse = await client.SendAsync(deactivate);
        deactivateResponse.EnsureSuccessStatusCode();

        // Names are only unique among active categories, so the name frees up.
        var snacksAgain = await CreateCategoryAsync(client, org, "Snacks");
        Assert.NotEqual(snacks.CategoryId, snacksAgain.CategoryId);

        using var assignInactive = Scoped(HttpMethod.Post, Products, org);
        assignInactive.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Chippy", "Pack", 12m, null, null, null, snacks.CategoryId));
        using var assignInactiveResponse = await client.SendAsync(assignInactive);
        Assert.Equal(HttpStatusCode.BadRequest, assignInactiveResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.CategoryNotAssignable, await ReadErrorCodeAsync(assignInactiveResponse));

        using var assignMissing = Scoped(HttpMethod.Post, Products, org);
        assignMissing.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Chippy", "Pack", 12m, null, null, null, Guid.NewGuid()));
        using var assignMissingResponse = await client.SendAsync(assignMissing);
        Assert.Equal(HttpStatusCode.NotFound, assignMissingResponse.StatusCode);

        var product = await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(
            "Chippy", "Pack", 12m, null, null, null, snacksAgain.CategoryId));
        Assert.Equal(snacksAgain.CategoryId, product.CategoryId);

        // Deactivating a category keeps its products; the assignment simply survives.
        using var deactivateAssigned = Scoped(HttpMethod.Post, $"{Categories}/{snacksAgain.CategoryId:D}/deactivate", org);
        (await client.SendAsync(deactivateAssigned)).EnsureSuccessStatusCode();

        using var reread = Scoped(HttpMethod.Get, $"{Products}/{product.ProductId:D}", org);
        using var rereadResponse = await client.SendAsync(reread);
        rereadResponse.EnsureSuccessStatusCode();
        var survivor = await rereadResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal(snacksAgain.CategoryId, survivor!.CategoryId);
        Assert.Equal("Active", survivor.Status);

        using var reactivate = Scoped(HttpMethod.Post, $"{Categories}/{snacksAgain.CategoryId:D}/reactivate", org);
        using var reactivateResponse = await client.SendAsync(reactivate);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
    }

    [Fact]
    public async Task Brand_lifecycle_enforces_active_name_uniqueness_and_assignment_rules()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();
        var otherOrg = Guid.NewGuid();

        var nestle = await CreateBrandAsync(client, org, "Nestle");

        using var duplicate = Scoped(HttpMethod.Post, Brands, org);
        duplicate.Content = JsonContent.Create(new CreatePosProductBrandRequest("  nestle  "));
        using var duplicateResponse = await client.SendAsync(duplicate);
        Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.BrandNameConflict, await ReadErrorCodeAsync(duplicateResponse));

        using var deactivate = Scoped(HttpMethod.Post, $"{Brands}/{nestle.BrandId:D}/deactivate", org);
        using var deactivateResponse = await client.SendAsync(deactivate);
        deactivateResponse.EnsureSuccessStatusCode();

        // Names are only unique among active brands, so the name frees up.
        var nestleAgain = await CreateBrandAsync(client, org, "Nestle");
        Assert.NotEqual(nestle.BrandId, nestleAgain.BrandId);

        using var assignInactive = Scoped(HttpMethod.Post, Products, org);
        assignInactive.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Milo", "Pack", 12m, BrandId: nestle.BrandId));
        using var assignInactiveResponse = await client.SendAsync(assignInactive);
        Assert.Equal(HttpStatusCode.BadRequest, assignInactiveResponse.StatusCode);
        Assert.Equal(ApplicationErrorCodes.BrandNotAssignable, await ReadErrorCodeAsync(assignInactiveResponse));

        using var assignMissing = Scoped(HttpMethod.Post, Products, org);
        assignMissing.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Milo", "Pack", 12m, BrandId: Guid.NewGuid()));
        using var assignMissingResponse = await client.SendAsync(assignMissing);
        Assert.Equal(HttpStatusCode.NotFound, assignMissingResponse.StatusCode);

        var foreignBrand = await CreateBrandAsync(client, otherOrg, "Foreign Brand");
        using var assignCrossOrg = Scoped(HttpMethod.Post, Products, org);
        assignCrossOrg.Content = JsonContent.Create(new CreatePosCatalogProductRequest(
            "Milo", "Pack", 12m, BrandId: foreignBrand.BrandId));
        using var assignCrossOrgResponse = await client.SendAsync(assignCrossOrg);
        Assert.Equal(HttpStatusCode.NotFound, assignCrossOrgResponse.StatusCode);

        var product = await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(
            "Milo", "Pack", 12m, BrandId: nestleAgain.BrandId));
        Assert.Equal(nestleAgain.BrandId, product.BrandId);
        Assert.Equal("Nestle", product.BrandName);

        using var filter = Scoped(HttpMethod.Get, $"{Products}?brandId={nestleAgain.BrandId:D}", org);
        using var filterResponse = await client.SendAsync(filter);
        filterResponse.EnsureSuccessStatusCode();
        var filtered = await filterResponse.Content.ReadFromJsonAsync<PagedResult<PosCatalogProductDto>>(JsonOptions);
        Assert.Contains(filtered!.Items, i => i.ProductId == product.ProductId);

        using var search = Scoped(HttpMethod.Get, $"{Products}?search=nestle", org);
        using var searchResponse = await client.SendAsync(search);
        searchResponse.EnsureSuccessStatusCode();
        var searched = await searchResponse.Content.ReadFromJsonAsync<PagedResult<PosCatalogProductDto>>(JsonOptions);
        Assert.Contains(searched!.Items, i => i.ProductId == product.ProductId);

        // Deactivating a brand keeps its products; the assignment simply survives.
        using var deactivateAssigned = Scoped(HttpMethod.Post, $"{Brands}/{nestleAgain.BrandId:D}/deactivate", org);
        (await client.SendAsync(deactivateAssigned)).EnsureSuccessStatusCode();

        using var reread = Scoped(HttpMethod.Get, $"{Products}/{product.ProductId:D}", org);
        using var rereadResponse = await client.SendAsync(reread);
        rereadResponse.EnsureSuccessStatusCode();
        var survivor = await rereadResponse.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.Equal(nestleAgain.BrandId, survivor!.BrandId);
        Assert.Equal("Nestle", survivor.BrandName);
        Assert.Equal("Active", survivor.Status);

        using var reactivate = Scoped(HttpMethod.Post, $"{Brands}/{nestleAgain.BrandId:D}/reactivate", org);
        using var reactivateResponse = await client.SendAsync(reactivate);
        Assert.Equal(HttpStatusCode.OK, reactivateResponse.StatusCode);
    }

    [Fact]
    public async Task Product_pagination_is_stable_by_name_then_id()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        foreach (var name in new[] { "Zesto", "Argentina", "Milo", "Bear Brand" })
        {
            await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(name, "Piece", 10m));
        }

        using var page1 = Scoped(HttpMethod.Get, $"{Products}?page=1&pageSize=2", org);
        using var page1Response = await client.SendAsync(page1);
        page1Response.EnsureSuccessStatusCode();
        var first = await page1Response.Content.ReadFromJsonAsync<PagedResult<PosCatalogProductDto>>(JsonOptions);
        Assert.Equal(new[] { "Argentina", "Bear Brand" }, first!.Items.Select(i => i.Name).ToArray());
        Assert.Equal(4, first.TotalCount);

        using var page2 = Scoped(HttpMethod.Get, $"{Products}?page=2&pageSize=2", org);
        using var page2Response = await client.SendAsync(page2);
        page2Response.EnsureSuccessStatusCode();
        var second = await page2Response.Content.ReadFromJsonAsync<PagedResult<PosCatalogProductDto>>(JsonOptions);
        Assert.Equal(new[] { "Milo", "Zesto" }, second!.Items.Select(i => i.Name).ToArray());
    }

    [Fact]
    public async Task Commercial_headers_gate_catalog_view_and_manage()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        var product = await CreateProductAsync(client, org, new CreatePosCatalogProductRequest(
            "Gated", "Piece", 5m));

        using var viewOnlyList = Scoped(
            HttpMethod.Get,
            Products,
            org,
            status: PosSubscriptionStatuses.Expired,
            grants: PosFeatureCodes.StoreCatalogView);
        using var viewOnlyListResponse = await client.SendAsync(viewOnlyList);
        viewOnlyListResponse.EnsureSuccessStatusCode();

        using var viewOnlyCreate = Scoped(
            HttpMethod.Post,
            Products,
            org,
            status: PosSubscriptionStatuses.Expired,
            grants: PosFeatureCodes.StoreCatalogView);
        viewOnlyCreate.Content = JsonContent.Create(new CreatePosCatalogProductRequest("Blocked", "Piece", 1m));
        using var viewOnlyCreateResponse = await client.SendAsync(viewOnlyCreate);
        Assert.Equal(HttpStatusCode.Forbidden, viewOnlyCreateResponse.StatusCode);

        using var manageInContinuity = Scoped(
            HttpMethod.Post,
            $"{Products}/{product.ProductId:D}/deactivate",
            org,
            status: PosSubscriptionStatuses.Expired,
            grants: $"{PosFeatureCodes.StoreCatalogView},{PosFeatureCodes.StoreCatalogManage}");
        using var manageInContinuityResponse = await client.SendAsync(manageInContinuity);
        Assert.Equal(HttpStatusCode.Forbidden, manageInContinuityResponse.StatusCode);

        using var creditGrantsOnly = Scoped(
            HttpMethod.Get,
            Products,
            org,
            status: PosSubscriptionStatuses.Active,
            grants: PosFeatureCodes.CustomerCreditView);
        using var creditGrantsOnlyResponse = await client.SendAsync(creditGrantsOnly);
        Assert.Equal(HttpStatusCode.Forbidden, creditGrantsOnlyResponse.StatusCode);

        using var suspended = Scoped(
            HttpMethod.Get,
            Products,
            org,
            status: PosSubscriptionStatuses.Suspended,
            grants: $"{PosFeatureCodes.StoreCatalogView},{PosFeatureCodes.StoreCatalogManage}");
        using var suspendedResponse = await client.SendAsync(suspended);
        Assert.Equal(HttpStatusCode.Forbidden, suspendedResponse.StatusCode);

        using var missingOrganization = new HttpRequestMessage(HttpMethod.Get, Products);
        using var missingOrganizationResponse = await client.SendAsync(missingOrganization);
        Assert.Equal(HttpStatusCode.BadRequest, missingOrganizationResponse.StatusCode);
    }

    [Fact]
    public async Task Catalog_endpoints_expose_no_sales_stock_or_pricing_rule_routes()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(FindRepoRoot(), "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api",
                "Catalog", "CatalogEndpoints.cs"));

        foreach (var forbidden in new[]
                 {
                     "/stock", "/inventory", "/sales", "/cart", "/checkout",
                     "/tax", "/discounts", "/barcodes", "StockLevel", "SaleLine"
                 })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static async Task<PosProductCategoryDto> CreateCategoryAsync(HttpClient client, Guid org, string name)
    {
        using var request = Scoped(HttpMethod.Post, Categories, org);
        request.Content = JsonContent.Create(new CreatePosProductCategoryRequest(name));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<PosProductCategoryDto>(JsonOptions);
        Assert.NotNull(category);
        return category!;
    }

    private static async Task<PosProductBrandDto> CreateBrandAsync(HttpClient client, Guid org, string name)
    {
        using var request = Scoped(HttpMethod.Post, Brands, org);
        request.Content = JsonContent.Create(new CreatePosProductBrandRequest(name));
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var brand = await response.Content.ReadFromJsonAsync<PosProductBrandDto>(JsonOptions);
        Assert.NotNull(brand);
        return brand!;
    }

    private static async Task<PosCatalogProductDto> CreateProductAsync(
        HttpClient client,
        Guid org,
        CreatePosCatalogProductRequest body)
    {
        using var request = Scoped(HttpMethod.Post, Products, org);
        request.Content = JsonContent.Create(body);
        using var response = await client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var product = await response.Content.ReadFromJsonAsync<PosCatalogProductDto>(JsonOptions);
        Assert.NotNull(product);
        return product!;
    }

    private static async Task<string?> ReadErrorCodeAsync(HttpResponseMessage response)
    {
        var problem = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        return problem.TryGetProperty("errorCode", out var code) ? code.GetString() : null;
    }

    private static HttpRequestMessage Scoped(
        HttpMethod method,
        string path,
        Guid organizationId,
        string? status = null,
        string? grants = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.OrganizationHeaderName,
            organizationId.ToString("D"));

        if (status is not null)
        {
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.SubscriptionStatusHeaderName, status);
        }

        if (grants is not null)
        {
            request.Headers.TryAddWithoutValidation(PosCommercialHeaders.FeatureGrantsHeaderName, grants);
        }

        return request;
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Repository root not found.");
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
