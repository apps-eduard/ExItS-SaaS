using ExItS.Platform.Application.Operations;

namespace ExItS.Platform.UnitTests.Operations;

public sealed class SystemHealthStatusRulesTests
{
    [Fact]
    public void Aggregate_all_healthy_is_healthy()
    {
        Assert.Equal(
            SystemHealthStatuses.Healthy,
            SystemHealthStatusRules.Aggregate(
            [
                SystemHealthStatuses.Healthy,
                SystemHealthStatuses.Healthy
            ]));
    }

    [Fact]
    public void Aggregate_any_unhealthy_is_unhealthy()
    {
        Assert.Equal(
            SystemHealthStatuses.Unhealthy,
            SystemHealthStatusRules.Aggregate(
            [
                SystemHealthStatuses.Healthy,
                SystemHealthStatuses.Unhealthy,
                SystemHealthStatuses.Degraded
            ]));
    }

    [Theory]
    [InlineData(SystemHealthStatuses.Degraded)]
    [InlineData(SystemHealthStatuses.Unavailable)]
    [InlineData(SystemHealthStatuses.Unknown)]
    [InlineData(SystemHealthStatuses.NotAvailable)]
    public void Aggregate_non_healthy_dependency_is_degraded_not_healthy(string status)
    {
        Assert.Equal(
            SystemHealthStatuses.Degraded,
            SystemHealthStatusRules.Aggregate([SystemHealthStatuses.Healthy, status]));
    }

    [Fact]
    public void Aggregate_empty_is_unknown()
    {
        Assert.Equal(SystemHealthStatuses.Unknown, SystemHealthStatusRules.Aggregate([]));
    }

    [Theory]
    [InlineData("Healthy", 200, SystemHealthStatuses.Healthy)]
    [InlineData("Degraded", 200, SystemHealthStatuses.Degraded)]
    [InlineData("Unhealthy", 503, SystemHealthStatuses.Unhealthy)]
    [InlineData("", 200, SystemHealthStatuses.Unknown)]
    [InlineData("bogus", 503, SystemHealthStatuses.Unhealthy)]
    [InlineData("bogus", 404, SystemHealthStatuses.Unavailable)]
    public void FromHealthEndpointBody_is_truthful(string body, int httpStatus, string expected)
    {
        Assert.Equal(expected, SystemHealthStatusRules.FromHealthEndpointBody(body, httpStatus));
    }
}
