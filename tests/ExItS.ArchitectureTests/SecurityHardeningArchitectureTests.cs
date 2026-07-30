namespace ExItS.ArchitectureTests;

/// <summary>P9-WP01: security hardening architecture and secret-pattern guards.</summary>
public sealed class SecurityHardeningArchitectureTests
{
    [Fact]
    public void Pos_and_platform_program_declare_security_pipeline_and_phase_marker()
    {
        var pos = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Program.cs"));
        var platform = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Api", "Program.cs"));

        Assert.Contains("P9-WP02-performance-and-reliability", pos, StringComparison.Ordinal);
        Assert.Contains("P9-WP02-performance-and-reliability", platform, StringComparison.Ordinal);
        Assert.Contains("AddPosSecurity", pos, StringComparison.Ordinal);
        Assert.Contains("UsePosSecurity", pos, StringComparison.Ordinal);
        Assert.Contains("AddPlatformSecurity", platform, StringComparison.Ordinal);
        Assert.Contains("UsePlatformSecurity", platform, StringComparison.Ordinal);
        var posPipeline = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Common", "PosSecurityPipeline.cs"));
        var platformPipeline = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Api", "Common", "PlatformSecurityPipeline.cs"));
        Assert.Contains("AddRateLimiter", posPipeline, StringComparison.Ordinal);
        Assert.Contains("AddRateLimiter", platformPipeline, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate(", pos, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate(", platform, StringComparison.Ordinal);
    }

    [Fact]
    public void Base_appsettings_do_not_embed_development_database_password()
    {
        var pos = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "appsettings.json"));
        var platform = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Api", "appsettings.json"));

        Assert.DoesNotContain("exits_platform_dev_only", pos, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("exits_platform_dev_only", platform, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\"PosDatabase\": \"\"", pos, StringComparison.Ordinal);
        Assert.Contains("\"PlatformDatabase\": \"\"", platform, StringComparison.Ordinal);
    }

    [Fact]
    public void Development_appsettings_may_contain_local_dev_password_only()
    {
        var posDev = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "appsettings.Development.json"));
        Assert.Contains("exits_platform_dev_only", posDev, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Dev_platform_actor_accessor_gates_header_outside_approved_environments()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Platform", "ExItS.Platform.Infrastructure", "Authorization", "DevelopmentPlatformActorAccessor.cs"));

        Assert.Contains("IsDevelopment()", source, StringComparison.Ordinal);
        Assert.Contains("IsEnvironment(\"Testing\")", source, StringComparison.Ordinal);
        Assert.Contains("DevPlatformUserIdHeader", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Pos_organization_scope_rejects_headers_outside_development_testing()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Common", "PosOrganizationScope.cs"));

        Assert.Contains("IsApprovedDevelopmentEnvironment", source, StringComparison.Ordinal);
        Assert.Contains("DevelopmentHeadersUnavailable", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Android_network_security_config_documents_cleartext_as_development_only()
    {
        var path = Path.Combine(FindRepositoryRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Maui",
            "Platforms", "Android", "Resources", "xml", "network_security_config.xml");
        var xml = File.ReadAllText(path);
        Assert.Contains("before any production release", xml, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("10.0.2.2", xml, StringComparison.Ordinal);
        Assert.DoesNotContain("cleartextTrafficPermitted=\"true\"", xml.Replace(
            """
            <domain-config cleartextTrafficPermitted="true">
                    <domain includeSubdomains="false">10.0.2.2</domain>
                    <domain includeSubdomains="false">localhost</domain>
                    <domain includeSubdomains="false">127.0.0.1</domain>
                </domain-config>
            """,
            string.Empty,
            StringComparison.Ordinal), StringComparison.Ordinal);
    }

    [Fact]
    public void No_healthcare_or_phi_coupling_in_pos_security_pipeline()
    {
        var pipeline = File.ReadAllText(Path.Combine(FindRepositoryRoot(),
            "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Common", "PosSecurityPipeline.cs"));
        Assert.DoesNotContain("HealthCare", pipeline, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PHI", pipeline, StringComparison.Ordinal);
        Assert.DoesNotContain("Patient", pipeline, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
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
}
