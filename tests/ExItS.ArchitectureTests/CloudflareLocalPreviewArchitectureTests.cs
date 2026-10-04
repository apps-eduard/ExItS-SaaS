namespace ExItS.ArchitectureTests;

/// <summary>
/// Opt-in Cloudflare Tunnel preview for FULL Docker Local Validation.
/// Local validation only. Not a production deployment.
/// </summary>
public class CloudflareLocalPreviewArchitectureTests
{
    [Fact]
    public void Base_local_validation_compose_does_not_require_a_cloudflare_token()
    {
        var compose = Read("deploy", "docker", "compose.local-validation.yaml");

        Assert.DoesNotContain("cloudflared", compose, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LOCAL_VALIDATION_CLOUDFLARE_TUNNEL_TOKEN", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("exitsapps.com", compose, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Cloudflare_preview_is_an_opt_in_overlay_on_the_local_validation_network()
    {
        var overlay = Read("deploy", "docker", "compose.cloudflare-preview.yaml");
        var start = Read("tools", "Start-CloudflareLocalPreview.ps1");
        var stop = Read("tools", "Stop-CloudflareLocalPreview.ps1");

        Assert.Contains("name: exits-local-validation", overlay, StringComparison.Ordinal);
        Assert.Contains("image: cloudflare/cloudflared:2025.8.1", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("cloudflare/cloudflared:latest", overlay, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TUNNEL_TOKEN: ${LOCAL_VALIDATION_CLOUDFLARE_TUNNEL_TOKEN:?Set LOCAL_VALIDATION_CLOUDFLARE_TUNNEL_TOKEN}", overlay, StringComparison.Ordinal);
        Assert.Contains("networks:\n      - exits-local-validation", overlay.Replace("\r\n", "\n", StringComparison.Ordinal), StringComparison.Ordinal);
        Assert.DoesNotContain("ports:", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("network_mode:", overlay, StringComparison.Ordinal);
        Assert.Contains("Start-DockerLocalValidation.ps1", start, StringComparison.Ordinal);
        Assert.Contains("exits-local-validation-cloudflared", stop, StringComparison.Ordinal);
        Assert.DoesNotContain("compose down", stop, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("down -v", stop, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("--volumes", stop, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Preview_publishes_only_current_runnable_hosts_and_keeps_data_services_private()
    {
        var overlay = Read("deploy", "docker", "compose.cloudflare-preview.yaml");
        var guide = Read("deploy", "docker", "README.cloudflare-local-preview.md");

        Assert.Contains("https://app.exitsapps.com    -> http://react-pos:80", overlay, StringComparison.Ordinal);
        Assert.Contains("https://my.exitsapps.com     -> http://react-pos:80", overlay, StringComparison.Ordinal);
        Assert.Contains("https://pos.exitsapps.com    -> http://react-pos:80", overlay, StringComparison.Ordinal);
        Assert.Contains("https://admin.exitsapps.com  -> http://admin-web:8080", overlay, StringComparison.Ordinal);

        foreach (var blocked in new[]
        {
            "bnpl.exitsapps.com",
            "loan.exitsapps.com",
            "pawn.exitsapps.com",
            "service.exitsapps.com",
            "api.exitsapps.com",
            "http://mailpit",
            "mailpit:",
            "platform-db:",
            "pos-db:",
            "15533",
            "15534",
            ":8025",
            ":1025",
        })
        {
            Assert.DoesNotContain(blocked, overlay, StringComparison.OrdinalIgnoreCase);
        }

        Assert.Contains("http://react-pos:80", guide, StringComparison.Ordinal);
        Assert.DoesNotContain("http://org-web:8080", guide, StringComparison.Ordinal);
        Assert.Contains("| `my.exitsapps.com` | `http://react-pos:80` |", guide, StringComparison.Ordinal);
        Assert.DoesNotContain("http://personal-web:8080", guide, StringComparison.Ordinal);
        Assert.Contains("http://react-pos:80", guide, StringComparison.Ordinal);
        Assert.Contains("http://admin-web:8080", guide, StringComparison.Ordinal);
        Assert.Contains("LOCAL VALIDATION", guide, StringComparison.Ordinal);
        Assert.Contains("NOT PRODUCTION", guide, StringComparison.Ordinal);
        Assert.Contains("Cloudflare Access", guide, StringComparison.Ordinal);
    }

    [Fact]
    public void Preview_does_not_widen_allowed_hosts_cors_or_proxy_trust()
    {
        var overlay = Read("deploy", "docker", "compose.cloudflare-preview.yaml");
        var guide = Read("deploy", "docker", "README.cloudflare-local-preview.md");
        var start = Read("tools", "Start-CloudflareLocalPreview.ps1");

        Assert.DoesNotContain("AllowedHosts=*", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowedHosts: \"*\"", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnyOrigin", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("0.0.0.0/0", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("ASPNETCORE_FORWARDEDHEADERS_ENABLED", overlay, StringComparison.Ordinal);
        Assert.Contains("app.exitsapps.com;my.exitsapps.com;admin.exitsapps.com;pos.exitsapps.com", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("ForwardedHeaders", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("personal-web:", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("KnownNetworks", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("PROXY_CIDR", overlay, StringComparison.Ordinal);
        Assert.Contains("https://admin.exitsapps.com", overlay, StringComparison.Ordinal);
        Assert.Contains("https://app.exitsapps.com", overlay, StringComparison.Ordinal);
        Assert.Contains("https://my.exitsapps.com", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("personal-web", start, StringComparison.Ordinal);
        Assert.DoesNotContain("PROXY_CIDR", start, StringComparison.Ordinal);
        Assert.DoesNotContain("KnownNetworks", start, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(FindRepoRoot(), "src", "Shared", "ExItS.Web.UI", "ExItSWebForwardedHeaders.cs")));
        Assert.DoesNotContain("AllowAnyOrigin", guide, StringComparison.Ordinal);
        Assert.Contains("not `*`", guide, StringComparison.Ordinal);
    }

    [Fact]
    public void Tracked_preview_files_do_not_contain_a_tunnel_token()
    {
        var example = Read("deploy", "docker", ".env.local-validation.example");
        var gitignore = Read(".gitignore");
        var overlay = Read("deploy", "docker", "compose.cloudflare-preview.yaml");
        var start = Read("tools", "Start-CloudflareLocalPreview.ps1");

        Assert.Contains("LOCAL_VALIDATION_CLOUDFLARE_TUNNEL_TOKEN=REPLACE_DO_NOT_COMMIT_REAL_TOKEN", example, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJ", example, StringComparison.Ordinal);
        Assert.Contains("**/.env.*", gitignore, StringComparison.Ordinal);
        Assert.Contains("!**/.env.local-validation.example", gitignore, StringComparison.Ordinal);
        Assert.DoesNotContain("eyJ", overlay, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Host $env:LOCAL_VALIDATION_CLOUDFLARE_TUNNEL_TOKEN", start, StringComparison.Ordinal);
        Assert.DoesNotContain("Write-Host .*token", start, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Canonical_domain_doc_matches_preview_hosts_and_keeps_unbuilt_products_planned()
    {
        var domain = Read("docs", "architecture", "domain-and-subdomain-strategy.md");

        Assert.Contains("app.exitsapps.com", domain, StringComparison.Ordinal);
        Assert.Contains("my.exitsapps.com", domain, StringComparison.Ordinal);
        Assert.Contains("admin.exitsapps.com", domain, StringComparison.Ordinal);
        Assert.Contains("pos.exitsapps.com", domain, StringComparison.Ordinal);
        Assert.Contains("bnpl.exitsapps.com", domain, StringComparison.Ordinal);
        Assert.Contains("loan.exitsapps.com", domain, StringComparison.Ordinal);
        Assert.Contains("pawn.exitsapps.com", domain, StringComparison.Ordinal);
        Assert.Contains("service.exitsapps.com", domain, StringComparison.Ordinal);
        Assert.Contains("local validation", domain, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not a production deployment", domain, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("does not publish them", domain, StringComparison.Ordinal);
    }

    private static string Read(params string[] parts)
    {
        var path = Path.Combine(new[] { FindRepoRoot() }.Concat(parts).ToArray());
        Assert.True(File.Exists(path), path);
        return File.ReadAllText(path);
    }

    private static string FindRepoRoot()
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

        throw new InvalidOperationException("Could not locate ExItS.slnx.");
    }
}
