using System.Net;
using ExItS.Platform.Api.Common;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;

namespace ExItS.Platform.IntegrationTests;

public sealed class PlatformForwardedHeadersConfigTests
{
    [Fact]
    public void Apply_clears_defaults_and_adds_only_configured_networks()
    {
        var options = new ForwardedHeadersOptions();
        options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse("127.0.0.0/8"));
        options.KnownProxies.Add(IPAddress.Loopback);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ForwardLimit"] = "1",
                ["KnownNetworks:0"] = "172.16.0.0/12",
                ["KnownProxies:0"] = "10.0.0.5"
            })
            .Build();

        PlatformForwardedHeaders.Apply(options, config);

        Assert.Equal(1, options.ForwardLimit);
        Assert.Single(options.KnownIPNetworks);
        Assert.Equal(System.Net.IPNetwork.Parse("172.16.0.0/12"), options.KnownIPNetworks.Single());
        Assert.Single(options.KnownProxies);
        Assert.Equal("10.0.0.5", options.KnownProxies.Single().ToString());
    }

    [Theory]
    [InlineData("172.16.0.0/12", true)]
    [InlineData("10.0.0.0/8", true)]
    [InlineData("not-a-cidr", false)]
    [InlineData("10.0.0.0/99", false)]
    [InlineData("", false)]
    public void TryParseCidr_validates_input(string cidr, bool expected)
    {
        Assert.Equal(expected, PlatformForwardedHeaders.TryParseCidr(cidr, out _));
    }
}
