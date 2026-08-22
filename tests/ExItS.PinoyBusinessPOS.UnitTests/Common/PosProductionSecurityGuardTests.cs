using ExItS.PinoyBusinessPOS.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ExItS.PinoyBusinessPOS.UnitTests.Common;

public sealed class PosProductionSecurityGuardTests
{
    [Fact]
    public void Staging_with_LocalValidation_enabled_does_not_require_production_signing_key()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Staging;
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["LocalValidation:Enabled"] = "true",
            });

        var exception = Record.Exception(() => PosProductionSecurityGuard.ValidateOrThrow(builder));
        Assert.Null(exception);
    }

    [Fact]
    public void Staging_without_LocalValidation_requires_production_signing_key()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Staging;
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:PosDatabase"] = "Host=db;Database=exits_pos;Username=pos;Password=secret",
                ["AllowedHosts"] = "localhost",
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => PosProductionSecurityGuard.ValidateOrThrow(builder));
        Assert.Contains("PosOffline:PriceAuthoritySigningKey", exception.Message, StringComparison.Ordinal);
    }
}
