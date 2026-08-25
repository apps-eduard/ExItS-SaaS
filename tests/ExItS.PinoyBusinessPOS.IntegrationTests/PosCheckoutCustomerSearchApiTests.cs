using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Api.Common;
using ExItS.PinoyBusinessPOS.Api.Customers;
using ExItS.PinoyBusinessPOS.Application.Commercial;
using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Application.Customers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ExItS.PinoyBusinessPOS.IntegrationTests;

[Collection(PosPostgreSqlCollection.Name)]
public sealed class PosCheckoutCustomerSearchApiTests(PosPostgreSqlFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Guid Actor = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private const string CashierGrants =
        $"{PosFeatureCodes.StoreSalesView},{PosFeatureCodes.StoreSalesCreate},{PosFeatureCodes.CustomerCreditCreate}";

    private const string ViewOnlyGrants = PosFeatureCodes.CustomerCreditView;

    [Fact]
    public async Task Cashier_can_checkout_search_but_not_full_customer_list()
    {
        await using var factory = new PosApiFactory(fixture.ConnectionString);
        var client = factory.CreateClient();
        var org = Guid.NewGuid();

        using var create = Scoped(HttpMethod.Post, "/api/v1/pos/customers", org, grants: null);
        create.Content = JsonContent.Create(new CreateCustomerRequest("Aling Rosa", "09171112222", null, null));
        using var createdResponse = await client.SendAsync(create);
        createdResponse.EnsureSuccessStatusCode();

        using var blank = Scoped(
            HttpMethod.Get,
            "/api/v1/pos/customers/checkout-search?search=%20",
            org,
            PosSubscriptionStatuses.Active,
            CashierGrants);
        using var blankResponse = await client.SendAsync(blank);
        Assert.Equal(HttpStatusCode.BadRequest, blankResponse.StatusCode);

        using var search = Scoped(
            HttpMethod.Get,
            "/api/v1/pos/customers/checkout-search?search=Rosa&pageSize=50",
            org,
            PosSubscriptionStatuses.Active,
            CashierGrants);
        using var searchResponse = await client.SendAsync(search);
        searchResponse.EnsureSuccessStatusCode();
        var raw = await searchResponse.Content.ReadAsStringAsync();
        var page = JsonSerializer.Deserialize<CheckoutCustomerSearchResult>(raw, JsonOptions);
        Assert.NotNull(page);
        Assert.True(page!.PageSize <= 20);
        var hit = Assert.Single(page.Items);
        Assert.Equal("Aling Rosa", hit.DisplayName);
        Assert.Equal("Active", hit.Status);
        Assert.DoesNotContain("\"notes\"", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"address\"", raw, StringComparison.OrdinalIgnoreCase);

        using var listDenied = Scoped(
            HttpMethod.Get,
            "/api/v1/pos/customers?search=Rosa",
            org,
            PosSubscriptionStatuses.Active,
            CashierGrants);
        using var listDeniedResponse = await client.SendAsync(listDenied);
        Assert.Equal(HttpStatusCode.Forbidden, listDeniedResponse.StatusCode);

        using var viewOnlySearch = Scoped(
            HttpMethod.Get,
            "/api/v1/pos/customers/checkout-search?search=Rosa",
            org,
            PosSubscriptionStatuses.Active,
            ViewOnlyGrants);
        using var viewOnlyResponse = await client.SendAsync(viewOnlySearch);
        Assert.Equal(HttpStatusCode.Forbidden, viewOnlyResponse.StatusCode);
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
        request.Headers.TryAddWithoutValidation(
            PosOrganizationHeaders.ActorHeaderName,
            Actor.ToString("D"));
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
