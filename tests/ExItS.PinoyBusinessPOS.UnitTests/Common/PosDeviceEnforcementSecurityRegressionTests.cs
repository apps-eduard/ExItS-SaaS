namespace ExItS.PinoyBusinessPOS.UnitTests.Common;

/// <summary>
/// Proves device-enforcement pause is not a general POS security bypass.
/// Money endpoints still require commercial capability checks before the device gate.
/// </summary>
public sealed class PosDeviceEnforcementSecurityRegressionTests
{
    [Fact]
    public void Sale_checkout_still_authorizes_capability_before_device_gate()
    {
        var path = FindSource("Sales", "SaleEndpoints.cs");
        var source = File.ReadAllText(path);

        Assert.Contains("TryAuthorize(request, access, UtangCapability.CreateSale", source, StringComparison.Ordinal);
        Assert.Contains("deviceAuthorization.EnsureAuthorizedAsync", source, StringComparison.Ordinal);

        var capabilityIndex = source.IndexOf(
            "TryAuthorize(request, access, UtangCapability.CreateSale",
            StringComparison.Ordinal);
        var deviceIndex = source.IndexOf(
            "deviceAuthorization.EnsureAuthorizedAsync",
            StringComparison.Ordinal);
        Assert.True(capabilityIndex >= 0 && deviceIndex > capabilityIndex);
    }

    [Fact]
    public void Write_off_endpoints_do_not_rely_on_device_gate_for_permission()
    {
        // Device gate is intentionally not the write-off permission model; write-offs use
        // commercial capabilities. Pausing device enforcement must not imply write-off is open.
        var writeOffPath = FindSource("Payments", "WriteOffEndpoints.cs");
        var writeOffSource = File.ReadAllText(writeOffPath);
        Assert.DoesNotContain("IPosDeviceTransactionAuthorizer", writeOffSource, StringComparison.Ordinal);
        Assert.Contains("WriteOff", writeOffSource, StringComparison.Ordinal);

        var authorizerPath = FindSource("Common", "PosDeviceTransactionAuthorization.cs");
        var authorizerSource = File.ReadAllText(authorizerPath);
        Assert.Contains("EnforcementEnabled", authorizerSource, StringComparison.Ordinal);
        Assert.Contains("user/org/capability/business rules still apply", File.ReadAllText(
            Path.Combine(
                FindRepoRoot(),
                "src",
                "Products",
                "PinoyBusinessPOS",
                "ExItS.PinoyBusinessPOS.Application",
                "Options",
                "PosDeviceAuthorizationOptions.cs")), StringComparison.OrdinalIgnoreCase);
    }

    private static string FindSource(string folder, string fileName)
    {
        var root = FindRepoRoot();
        return Path.Combine(
            root,
            "src",
            "Products",
            "PinoyBusinessPOS",
            "ExItS.PinoyBusinessPOS.Api",
            folder,
            fileName);
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "ExItS.slnx"))
                || File.Exists(Path.Combine(dir.FullName, "ExItS.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test base directory.");
    }
}
