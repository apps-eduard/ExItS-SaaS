extern alias PlatformApi;

using ExItS.PinoyBusinessPOS.Api.Common;

using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ExItS.Platform.Application.Catalog;
using ExItS.Platform.Application.Common;
using ExItS.Platform.Application.Organizations;
using ExItS.Platform.Domain.Abstractions;
using ExItS.Platform.Domain.Catalog;
using ExItS.Platform.Domain.GlobalCatalog;
using ExItS.Platform.Domain.Organizations;
using ExItS.Platform.Domain.Products;
using ExItS.Platform.Domain.Subscriptions;
using ExItS.Platform.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ExItS.PinoyBusinessPOS.IntegrationTests.Support;

internal sealed record SpineBusinessContext(
    Guid OrganizationId,
    Guid SubscriptionId,
    Guid UserId,
    string Email,
    string Password,
    string SessionToken,
    string AccessToken);

internal static class PlatformCommercialSpineSupport
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static string Unique(string prefix) =>
        $"{prefix}{Guid.NewGuid():N}"[..Math.Min(20, prefix.Length + 32)].ToLowerInvariant();

    internal static HttpRequestMessage Authed(HttpMethod method, string url, string sessionToken, object? body = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-ExItS-Session-Token", sessionToken);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    internal static async Task EnsureMvpCatalogAsync(string platformConnectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = platformConnectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPlatformPersistence(configuration);
        services.AddLogging();
        services.AddScoped<CreateProduct>();
        services.AddScoped<CreateFeatureDefinition>();
        services.AddScoped<CreatePlan>();
        services.AddScoped<ActivatePlan>();
        services.AddScoped<UpdatePlanCommercialPackage>();
        services.AddScoped<CreateDraftPlanVersion>();
        services.AddScoped<PublishExistingPlanVersion>();
        services.AddScoped<CreateTrialDefinition>();
        services.AddScoped<RetirePlan>();
        services.AddScoped<EnsureMvpPosPlans>();
        services.AddSingleton<IClock>(new SpineTestUtcClock(DateTimeOffset.UtcNow));

        await using var provider = services.BuildServiceProvider();
        var createProduct = provider.GetRequiredService<CreateProduct>();
        var productResult = await createProduct.ExecuteAsync(ProductCode.PinoyBusinessPos, "Pinoy Business POS");
        if (!productResult.IsSuccess && productResult.ErrorCode != ApplicationErrorCodes.DuplicateProductCode)
        {
            throw new InvalidOperationException(
                $"POS product seed failed: {productResult.ErrorCode} {productResult.ErrorMessage}");
        }

        await provider.GetRequiredService<EnsureMvpPosPlans>().ExecuteAsync();
    }

    internal static async Task<SpineBusinessContext> StartMvpBusinessAsync(
        HttpClient platformClient,
        string planKey,
        string prefix)
    {
        var (_, email, password) = await RegisterPersonalWithPasswordAsync(platformClient, prefix);
        var login = await platformClient.PostAsJsonAsync(
            "/api/v1/platform/auth/login",
            new { usernameOrEmail = email, password });
        login.EnsureSuccessStatusCode();
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var sessionToken = loginBody.GetProperty("sessionToken").GetString()!;
        var userId = loginBody.GetProperty("userId").GetGuid();

        using var start = Authed(
            HttpMethod.Post,
            "/api/v1/personal/start-business",
            sessionToken,
            new
            {
                displayName = $"{prefix} Store",
                slug = Unique(prefix),
                productCode = ProductCode.PinoyBusinessPos,
                planKey,
                billingCycle = BillingCycle.Monthly,
                startAsTrial = true,
                payNow = false,
                activatePosEntitlement = true,
                activateProductAccess = true,
                assignPosOwnerRole = true,
                primaryBusinessTypeId = LegacyBusinessTypeSeeds.SariSariId
            });
        var startResponse = await platformClient.SendAsync(start);
        if (!startResponse.IsSuccessStatusCode)
        {
            var errorBody = await startResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Start business failed ({(int)startResponse.StatusCode}): {errorBody}");
        }
        var started = await startResponse.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var organizationId = started.GetProperty("organizationId").GetGuid();
        var subscriptionId = started.GetProperty("subscriptionId").GetGuid();
        var effectiveSessionToken = started.TryGetProperty("sessionToken", out var rotated)
            && !string.IsNullOrWhiteSpace(rotated.GetString())
            ? rotated.GetString()!
            : sessionToken;

        var accessToken = await IssueProductAccessTokenAsync(
            platformClient,
            effectiveSessionToken,
            organizationId);
        return new SpineBusinessContext(
            organizationId,
            subscriptionId,
            userId,
            email,
            password,
            effectiveSessionToken,
            accessToken);
    }

    internal static async Task<string> IssueProductAccessTokenAsync(
        HttpClient platformClient,
        string sessionToken,
        Guid organizationId)
    {
        using var grant = Authed(
            HttpMethod.Post,
            "/api/v1/platform/auth/token",
            sessionToken,
            new
            {
                grantType = "session",
                organizationId,
                productCode = ProductCode.PinoyBusinessPos
            });
        var issued = await platformClient.SendAsync(grant);
        issued.EnsureSuccessStatusCode();
        var body = await issued.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var accessToken = body.GetProperty("accessToken").GetString();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new InvalidOperationException("Platform session grant did not return an access token.");
        }

        return accessToken;
    }

    internal static async Task<JsonElement> IntrospectAsync(HttpClient platformClient, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/platform/auth/introspect");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = JsonContent.Create(new { token = (string?)null });
        var response = await platformClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))!;
    }

    internal static bool IntrospectionHasFeature(JsonElement introspection, string featureCode)
    {
        if (!introspection.TryGetProperty("enabledFeatureCodes", out var codes))
        {
            return false;
        }

        return codes.EnumerateArray().Any(item =>
            string.Equals(item.GetString(), featureCode, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<(Guid UserId, string Email, string Password)> RegisterPersonalWithPasswordAsync(
        HttpClient client,
        string prefix)
    {
        var emailLocal = Unique(prefix);
        var email = $"{emailLocal}@example.com";
        var password = "Correct-Horse-9!";
        var register = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/register",
            new { displayName = "Personal User", email });
        register.EnsureSuccessStatusCode();
        var body = await register.Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var token = body.GetProperty("debugToken").GetString();
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Personal registration did not return debugToken.");
        }

        var activate = await client.PostAsJsonAsync(
            "/api/v1/platform/auth/activate-account",
            new { token, password });
        activate.EnsureSuccessStatusCode();

        var list = await client.GetAsync($"/api/v1/platform/users?search={emailLocal}&pageSize=5");
        list.EnsureSuccessStatusCode();
        var userId = (await list.Content.ReadFromJsonAsync<JsonElement>(JsonOptions))!
            .GetProperty("items")[0]
            .GetProperty("id")
            .GetGuid();
        return (userId, email, password);
    }

    internal static async Task<ApplicationResult<RegisterPosDeviceOutcome>> TryRegisterDeviceAsync(
        string platformConnectionString,
        Guid organizationId,
        Guid branchId,
        string installationId,
        string displayName)
    {
        await using var provider = BuildPlatformDeviceProvider(platformConnectionString);
        var register = provider.GetRequiredService<RegisterCurrentDevice>();
        return await register.ExecuteAsync(
            PlatformOrganizationId.From(organizationId),
            new RegisterPosDeviceCommand(branchId, installationId, displayName));
    }

    internal static async Task RegisterDeviceAsync(
        string platformConnectionString,
        Guid organizationId,
        Guid branchId,
        string installationId,
        string displayName)
    {
        var result = await TryRegisterDeviceAsync(
            platformConnectionString,
            organizationId,
            branchId,
            installationId,
            displayName);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException($"{result.ErrorCode}: {result.ErrorMessage}");
        }
    }

    internal static ServiceProvider BuildPlatformDeviceProvider(string connectionString)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PlatformDatabase"] = connectionString
            })
            .Build();

        var services = new ServiceCollection();
        services.AddPlatformPersistence(configuration);
        services.AddScoped<RegisterCurrentDevice>();
        services.AddSingleton<IClock>(new SpineTestUtcClock(DateTimeOffset.UtcNow));
        return services.BuildServiceProvider();
    }

    private sealed class SpineTestUtcClock(DateTimeOffset utcNow) : IClock
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
    }

    internal sealed class PosSpineClientScope : IAsyncDisposable
    {
        private readonly PosSpineApiFactory _factory;

        public HttpClient Client { get; }

        internal PosSpineClientScope(string posConnectionString, PlatformSpineApiFactory platformFactory)
        {
            _factory = new PosSpineApiFactory(
                posConnectionString,
                platformFactory.Server.BaseAddress,
                platformFactory.Server.CreateHandler());
            Client = _factory.CreateClient();
        }

        public ValueTask DisposeAsync()
        {
            Client.Dispose();
            _factory.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    internal static PosSpineClientScope CreatePosClientScope(
        string posConnectionString,
        PlatformSpineApiFactory platformFactory)
    {
        PlatformTokenIntrospectionClient.ClearCacheForTests();
        return new(posConnectionString, platformFactory);
    }

    internal static HttpRequestMessage PosBearerGet(string path, string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return request;
    }

    internal sealed class PosSpineApiFactory(
        string posConnectionString,
        Uri platformBaseAddress,
        HttpMessageHandler platformHandler)
        : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:PosDatabase", posConnectionString);
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PosDatabase"] = posConnectionString,
                    ["PlatformAuth:BaseUrl"] = platformBaseAddress.ToString().TrimEnd('/'),
                    ["CommercialValidation:Strict"] = "true"
                });
            });
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IPlatformTokenIntrospectionClient>();
                services.AddHttpClient<IPlatformTokenIntrospectionClient, PlatformTokenIntrospectionClient>()
                    .ConfigurePrimaryHttpMessageHandler(() => platformHandler)
                    .ConfigureHttpClient(client =>
                    {
                        client.BaseAddress = new Uri(platformBaseAddress.ToString().TrimEnd('/') + "/");
                        client.Timeout = TimeSpan.FromSeconds(30);
                    });
            });
        }
    }

    internal sealed class PlatformSpineApiFactory(string connectionString)
        : WebApplicationFactory<PlatformApi::Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:PlatformDatabase", connectionString);
            builder.UseSetting("LocalValidation:Enabled", "true");
            builder.UseSetting("LocalValidation:RunHostedSeed", "false");
            builder.UseSetting("LocalValidation:SharedPassword", "LocalValidationTestPass123");
            builder.UseSetting("Payments:Provider", "LocalValidation");
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:PlatformDatabase"] = connectionString,
                    ["Security:EnforceHttps"] = "false",
                    ["PlatformAuthentication:External:TestingEndpointEnabled"] = "true",
                    ["PlatformAuthentication:Lifecycle:ExposeDebugTokens"] = "true",
                    ["LocalValidation:Enabled"] = "true",
                    ["LocalValidation:RunHostedSeed"] = "false",
                    ["LocalValidation:SharedPassword"] = "LocalValidationTestPass123",
                    ["Payments:Provider"] = "LocalValidation"
                });
            });
        }
    }
}
