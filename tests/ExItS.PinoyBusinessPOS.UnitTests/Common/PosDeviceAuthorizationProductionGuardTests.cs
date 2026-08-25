using ExItS.PinoyBusinessPOS.Api.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace ExItS.PinoyBusinessPOS.UnitTests.Common;

public sealed class PosDeviceAuthorizationProductionGuardTests
{
    [Fact]
    public void Production_cannot_disable_device_authorization_enforcement()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:PosDatabase"] = "Host=db;Database=exits_pos;Username=pos;Password=prod_secret",
                ["AllowedHosts"] = "pos.example.com",
                ["PosOffline:PriceAuthoritySigningKey"] = "production-signing-key-not-dev-marker-xx",
                ["PosOffline:OperatingGrantSigningPrivateKeyPem"] =
                    "-----BEGIN PRIVATE KEY-----\nprod-only-key-material\n-----END PRIVATE KEY-----",
                ["PlatformAuth:BaseUrl"] = "https://platform.example.com/",
                ["PosDeviceAuthorization:EnforcementEnabled"] = "false",
            });

        var exception = Assert.Throws<InvalidOperationException>(
            () => PosProductionSecurityGuard.ValidateOrThrow(builder));
        Assert.Contains(
            "POS device authorization cannot be disabled in Production",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Production_allows_enforcement_enabled_default()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Environment.EnvironmentName = Environments.Production;
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["ConnectionStrings:PosDatabase"] = "Host=db;Database=exits_pos;Username=pos;Password=prod_secret",
                ["AllowedHosts"] = "pos.example.com",
                ["PosOffline:PriceAuthoritySigningKey"] = "production-signing-key-not-dev-marker-xx",
                ["PosOffline:OperatingGrantSigningPrivateKeyPem"] =
                    "-----BEGIN PRIVATE KEY-----\nprod-only-key-material\n-----END PRIVATE KEY-----",
                ["PlatformAuth:BaseUrl"] = "https://platform.example.com/",
                ["PosDeviceAuthorization:EnforcementEnabled"] = "true",
            });

        var exception = Record.Exception(() => PosProductionSecurityGuard.ValidateOrThrow(builder));
        Assert.Null(exception);
    }
}
