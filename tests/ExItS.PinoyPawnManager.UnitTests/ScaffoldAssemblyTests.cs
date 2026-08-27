using ExItS.PinoyPawnManager.Application;
using ExItS.PinoyPawnManager.Domain;
using ExItS.PinoyPawnManager.Domain.Access;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ExItS.PinoyPawnManager.UnitTests;

public sealed class ScaffoldAssemblyTests
{
    [Fact]
    public void Domain_and_application_assemblies_load()
    {
        Assert.Equal("ExItS.PinoyPawnManager.Domain", typeof(DomainAssembly).Assembly.GetName().Name);
        Assert.Equal("ExItS.PinoyPawnManager.Application", typeof(ApplicationAssembly).Assembly.GetName().Name);
        Assert.Equal("ExItS.PinoyPawnManager.Infrastructure", typeof(Infrastructure.InfrastructureAssembly).Assembly.GetName().Name);
    }

    [Fact]
    public void Ppm_product_identity_matches_platform_catalog_code()
    {
        Assert.Equal("pinoy-pawn-manager", PpmProductIdentity.PinoyPawnManagerCode);
        Assert.True(PpmProductIdentity.IsPinoyPawnManager("pinoy-pawn-manager"));
        Assert.True(PpmProductIdentity.IsPinoyPawnManager("Pinoy-Pawn-Manager"));
        Assert.False(PpmProductIdentity.IsPinoyPawnManager("pinoy-loan-manager"));
        Assert.False(PpmProductIdentity.IsPinoyPawnManager("pinoy-business-pos"));
        Assert.False(PpmProductIdentity.IsPinoyPawnManager("pinoy-buy-now-pay-later"));
        Assert.False(PpmProductIdentity.IsPinoyPawnManager("pinoy-service-pro"));
    }

    [Fact]
    public async Task Api_health_returns_ok()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal("ok", await response.Content.ReadAsStringAsync());
    }
}
