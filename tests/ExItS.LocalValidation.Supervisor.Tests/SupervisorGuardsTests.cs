using ExItS.LocalValidation.Supervisor;

namespace ExItS.LocalValidation.Supervisor.Tests;

public sealed class SupervisorGuardsTests
{
    [Fact]
    public void Production_environment_is_detected()
    {
        Assert.True(SupervisorGuards.IsProductionEnvironmentName("Production"));
        Assert.False(SupervisorGuards.IsProductionEnvironmentName("Development"));
        Assert.False(SupervisorGuards.IsProductionEnvironmentName("Staging"));
    }

    [Fact]
    public void Unknown_service_keys_are_rejected_by_allowlist()
    {
        Assert.False(SupervisorGuards.IsAllowedServiceKey("not-a-service"));
        Assert.True(SupervisorGuards.IsAllowedServiceKey("pos-api"));
        Assert.True(SupervisorGuards.IsAllowedServiceKey("platform-db"));
        Assert.False(SupervisorGuards.IsRestartableServiceKey("platform-db"));
        Assert.False(SupervisorGuards.IsRestartableServiceKey("pos-db"));
        Assert.True(SupervisorGuards.IsRestartableServiceKey("react-pos"));
    }

    [Fact]
    public void Arbitrary_command_routes_are_not_part_of_allowlist()
    {
        Assert.False(SupervisorGuards.IsAllowedRoute("POST", "/execute"));
        Assert.False(SupervisorGuards.IsAllowedRoute("POST", "/powershell"));
        Assert.False(SupervisorGuards.IsAllowedRoute("POST", "/command"));
        Assert.True(SupervisorGuards.IsAllowedRoute("GET", "/health/services"));
        Assert.True(SupervisorGuards.IsAllowedRoute("POST", "/services/pos-api/restart"));
        Assert.False(SupervisorGuards.IsAllowedRoute("POST", "/services/platform-db/restart"));
        Assert.True(SupervisorGuards.IsAllowedRoute("POST", "/services/restart-all"));
        Assert.True(SupervisorGuards.IsAllowedRoute("POST", "/reset"));
    }
}
