namespace ExItS.ArchitectureTests;

public sealed class ProductionProxyTlsArchitectureTests
{
    [Fact]
    public void Production_compose_exposes_only_reverse_proxy_ports()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "deploy", "docker", "compose.production.yaml");
        Assert.True(File.Exists(path));
        var compose = File.ReadAllText(path);

        Assert.Contains("name: exits-production", compose, StringComparison.Ordinal);
        Assert.Contains("P14-WP03", compose, StringComparison.Ordinal);
        Assert.Contains("reverse-proxy:", compose, StringComparison.Ordinal);
        Assert.Contains("nginx:1.27-alpine", compose, StringComparison.Ordinal);
        Assert.Contains("PRODUCTION_HTTP_PORT", compose, StringComparison.Ordinal);
        Assert.Contains("PRODUCTION_HTTPS_PORT", compose, StringComparison.Ordinal);
        Assert.Contains("exits-production-internal", compose, StringComparison.Ordinal);
        Assert.Contains("internal: true", compose, StringComparison.Ordinal);
        Assert.Contains("platform-db:", compose, StringComparison.Ordinal);
        Assert.Contains("pos-db:", compose, StringComparison.Ordinal);
        Assert.Contains("platform-admin:", compose, StringComparison.Ordinal);
        Assert.Contains("ForwardedHeaders__Enabled", compose, StringComparison.Ordinal);
        Assert.Contains("Security__EnforceHttps: \"true\"", compose, StringComparison.Ordinal);
        Assert.Contains("DOES NOT claim", compose, StringComparison.OrdinalIgnoreCase);

        // Backend APIs and databases must not publish host ports in production template.
        Assert.DoesNotContain("PLATFORM_API_HOST_PORT", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("POS_API_HOST_PORT", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("PLATFORM_DB_HOST_PORT", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("POS_DB_HOST_PORT", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("15433", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("LocalValidation__Enabled", compose, StringComparison.Ordinal);
        Assert.DoesNotContain(".Migrate(", compose, StringComparison.Ordinal);
        Assert.DoesNotContain("HealthCare/", compose, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("8090–8092", compose, StringComparison.Ordinal); // Local Validation ports cited as preserved, not published
    }

    [Fact]
    public void Production_nginx_terminates_tls_and_routes_explicitly()
    {
        var root = FindRepoRoot();
        var path = Path.Combine(root, "deploy", "docker", "nginx", "production.conf");
        Assert.True(File.Exists(path));
        var conf = File.ReadAllText(path);

        Assert.Contains("return 301 https://$host$request_uri", conf, StringComparison.Ordinal);
        Assert.Contains("ssl_protocols       TLSv1.2 TLSv1.3", conf, StringComparison.Ordinal);
        Assert.Contains("Strict-Transport-Security", conf, StringComparison.Ordinal);
        Assert.Contains("X-Forwarded-Proto https", conf, StringComparison.Ordinal);
        Assert.Contains("location /platform/", conf, StringComparison.Ordinal);
        Assert.Contains("location /pos/", conf, StringComparison.Ordinal);
        Assert.Contains("location /admin/", conf, StringComparison.Ordinal);
        Assert.Contains("location /admin/_blazor", conf, StringComparison.Ordinal);
        Assert.Contains("client_max_body_size 1m", conf, StringComparison.Ordinal);
        Assert.Contains("server_tokens off", conf, StringComparison.Ordinal);
        Assert.DoesNotContain("ssl_certificate     /etc/nginx/certs/fullchain.pem;\n    ssl_certificate_key     /committed", conf, StringComparison.Ordinal);
    }

    [Fact]
    public void Production_env_and_cert_docs_exist_without_real_secrets()
    {
        var root = FindRepoRoot();
        var env = File.ReadAllText(Path.Combine(root, "deploy", "docker", ".env.production.example"));
        Assert.Contains("REPLACE_PLATFORM_DB_PASSWORD", env, StringComparison.Ordinal);
        Assert.Contains("PRODUCTION_TLS_CERT_DIR", env, StringComparison.Ordinal);
        Assert.Contains("PRODUCTION_ADMIN_ORIGIN=https://", env, StringComparison.Ordinal);
        Assert.DoesNotContain("exits_platform_dev_only", env, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("BEGIN PRIVATE KEY", env, StringComparison.Ordinal);
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "certs", "README.md")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "nginx", "pilot.conf")));
    }

    [Fact]
    public void Apps_configure_forwarded_headers_helpers_and_disabled_defaults()
    {
        var root = FindRepoRoot();
        var apiHelper = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "Common", "PlatformForwardedHeaders.cs"));
        Assert.Contains("KnownIPNetworks.Clear()", apiHelper, StringComparison.Ordinal);
        Assert.Contains("KnownProxies.Clear()", apiHelper, StringComparison.Ordinal);
        Assert.Contains("ForwardedHeaders.XForwardedProto", apiHelper, StringComparison.Ordinal);

        var apiSettings = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Api", "appsettings.json"));
        Assert.Contains("\"ForwardedHeaders\"", apiSettings, StringComparison.Ordinal);
        Assert.Contains("\"Enabled\": false", apiSettings, StringComparison.Ordinal);

        var adminProgram = File.ReadAllText(Path.Combine(root, "src", "Platform", "ExItS.Platform.Admin", "Program.cs"));
        Assert.Contains("UseAdminForwardedHeaders", adminProgram, StringComparison.Ordinal);
    }

    [Fact]
    public void Live_preview_ports_remain_documented_and_unchanged_by_production_compose()
    {
        var root = FindRepoRoot();
        var live = File.ReadAllText(Path.Combine(root, "deploy", "docker", "compose.local-validation.yaml"));
        Assert.Contains("${LOCAL_VALIDATION_ADMIN_HOST_PORT:-8090}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_PLATFORM_API_HOST_PORT:-8091}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_POS_API_HOST_PORT:-8092}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT:-15533}:5432", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_POS_DB_HOST_PORT:-15534}:5432", live, StringComparison.Ordinal);
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

        throw new InvalidOperationException("Repository root not found.");
    }
}
