namespace ExItS.ArchitectureTests;

public sealed class LocalValidationPackagingArchitectureTests
{
    [Fact]
    public void Local_validation_compose_is_separate_project_with_admin_and_distinct_ports()
    {
        var root = FindRepoRoot();
        var livePath = Path.Combine(root, "deploy", "docker", "compose.local-validation.yaml");
        var packagingPath = Path.Combine(root, "deploy", "docker", "compose.yaml");
        Assert.True(File.Exists(livePath));
        Assert.True(File.Exists(packagingPath));

        var live = File.ReadAllText(livePath);
        var packaging = File.ReadAllText(packagingPath);

        Assert.Contains("name: exits-local-validation", live, StringComparison.Ordinal);
        Assert.Contains("name: exits-packaging", packaging, StringComparison.Ordinal);
        Assert.Contains("admin-web:", live, StringComparison.Ordinal);
        Assert.Contains("exits-local-validation-admin-web", live, StringComparison.Ordinal);
        Assert.Contains("org-web:", live, StringComparison.Ordinal);
        Assert.Contains("exits-local-validation-org-web", live, StringComparison.Ordinal);
        Assert.Contains("personal-web:", live, StringComparison.Ordinal);
        Assert.Contains("exits-local-validation-personal-web", live, StringComparison.Ordinal);
        Assert.Contains("profiles: [\"apps\"]", live, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_platform_db_data", live, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_pos_db_data", live, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_platform_api_dataprotection_keys", live, StringComparison.Ordinal);
        Assert.Contains("DataProtection__KeysPath", live, StringComparison.Ordinal);
        Assert.Contains("production-equivalent", live, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASPNETCORE_ENVIRONMENT: Staging", live, StringComparison.Ordinal);
        Assert.Contains("LocalValidation__Enabled", live, StringComparison.Ordinal);
        Assert.DoesNotContain("live-preview", live, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("LivePreview", live, StringComparison.Ordinal);
        Assert.DoesNotContain(
            PortfolioIndependenceTokens.ForbiddenToken,
            live,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".Migrate(", live, StringComparison.Ordinal);

        Assert.Contains("mailpit", live, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("exits-local-validation-mailpit", live, StringComparison.Ordinal);
        Assert.Contains("8025", live, StringComparison.Ordinal);
        Assert.Contains("1025", live, StringComparison.Ordinal);

        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "README.local-validation.md")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "README.local-validation-workflow.md")));
        Assert.True(File.Exists(Path.Combine(root, "tools", "Start-LocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "tools", "Stop-LocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "tools", "Start-DockerLocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "tools", "Stop-DockerLocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "tools", "Reset-LocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Dockerfile.organization-web")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Dockerfile.personal-web")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Start-LocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Stop-LocalValidation.ps1")));
        Assert.True(File.Exists(Path.Combine(root, "deploy", "docker", "Reset-LocalValidation.ps1")));

        var startScript = File.ReadAllText(Path.Combine(root, "tools", "Start-LocalValidation.ps1"));
        var dockerStartScript = File.ReadAllText(Path.Combine(root, "tools", "Start-DockerLocalValidation.ps1"));
        var dockerStopScript = File.ReadAllText(Path.Combine(root, "tools", "Stop-DockerLocalValidation.ps1"));
        var stackScript = File.ReadAllText(Path.Combine(root, "tools", "LocalValidation.stack.ps1"));
        Assert.Contains("Mailpit", startScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("PlatformEmail__SmtpHost", startScript, StringComparison.Ordinal);
        Assert.Contains("PlatformEmail__PinoyLoanManagerPublicBaseUrl", startScript, StringComparison.Ordinal);
        Assert.Contains("PlatformEmail__AllowHttpLoopbackPublicUrls", startScript, StringComparison.Ordinal);
        Assert.Contains("PlatformEmail__PinoyLoanManagerPublicBaseUrl", live, StringComparison.Ordinal);
        Assert.Contains("dotnet watch", startScript, StringComparison.Ordinal);
        Assert.Contains("DataProtectionKeys", startScript, StringComparison.Ordinal);
        Assert.Contains("exits-local-validation-platform-db", stackScript, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_platform_db_data", stackScript, StringComparison.Ordinal);
        Assert.Contains("volumes preserved", startScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("http://0.0.0.0:", startScript, StringComparison.Ordinal);
        Assert.Contains("PublicHost", startScript, StringComparison.Ordinal);
        Assert.Contains("Resolve-EffectivePublicHost", startScript, StringComparison.Ordinal);
        Assert.Contains("LOCAL_VALIDATION_PUBLIC_HOST", startScript, StringComparison.Ordinal);
        Assert.Contains("PlatformAdministratorsOnly", startScript, StringComparison.Ordinal);
        Assert.Contains("PurgeTransactional", startScript, StringComparison.Ordinal);
        Assert.Contains("LocalValidation.stack.ps1", startScript, StringComparison.Ordinal);
        Assert.Contains("exits-local-validation", startScript, StringComparison.Ordinal);
        Assert.Contains("Cors__AllowedOrigins__", startScript, StringComparison.Ordinal);
        Assert.Contains("PlatformAuthentication__Password__MinimumLength", startScript, StringComparison.Ordinal);
        Assert.Contains("PlatformAuthentication__Password__RequireUppercase", startScript, StringComparison.Ordinal);
        Assert.Contains("New-NetFirewallRule", startScript, StringComparison.Ordinal);
        Assert.Contains("LocalPort 8090", startScript, StringComparison.Ordinal);
        Assert.Contains("LocalPort 8091", startScript, StringComparison.Ordinal);
        Assert.Contains("LocalPort 8092", startScript, StringComparison.Ordinal);
        Assert.Contains("LocalPort 8093", startScript, StringComparison.Ordinal);
        Assert.Contains("LocalPort 8094", startScript, StringComparison.Ordinal);
        Assert.Contains("LocalPort 8095", startScript, StringComparison.Ordinal);
        Assert.Contains("Profile Private", startScript, StringComparison.Ordinal);
        Assert.Contains("PLATFORM_API_SAME_ORIGIN", startScript, StringComparison.Ordinal);
        Assert.Contains("PLATFORM_API_PROXY_TARGET", startScript, StringComparison.Ordinal);
        Assert.Contains("Write-LocalValidationReactAdminBanner", startScript, StringComparison.Ordinal);
        Assert.Contains("Write-LocalValidationMailpitBanner", startScript, StringComparison.Ordinal);
        Assert.Contains("PlatformEmail__AdminPublicBaseUrl = $publicAdminWebReactUrl", startScript, StringComparison.Ordinal);
        Assert.Contains("Email links:", stackScript, StringComparison.Ordinal);
        Assert.Contains("LocalPort 8025", startScript, StringComparison.Ordinal);
        Assert.DoesNotContain("LocalPort 15533", startScript, StringComparison.Ordinal);
        Assert.DoesNotContain("LocalPort 15534", startScript, StringComparison.Ordinal);
        Assert.DoesNotContain("docker compose down -v", startScript, StringComparison.Ordinal);
        Assert.DoesNotContain("down -v", dockerStartScript, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("down -v", dockerStopScript, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Mode = 'DockerApps'", dockerStartScript, StringComparison.Ordinal);
        Assert.Contains("Write-LocalValidationReactAdminBanner", dockerStartScript, StringComparison.Ordinal);
        Assert.Contains("Write-LocalValidationMailpitBanner", dockerStartScript, StringComparison.Ordinal);
        Assert.Contains("PLATFORM_API_SAME_ORIGIN", dockerStartScript, StringComparison.Ordinal);
        Assert.Contains("Stop-LocalValidationDockerAppServices", dockerStopScript, StringComparison.Ordinal);

        var platformLaunch = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Properties", "launchSettings.json"));
        Assert.Contains("http://0.0.0.0:8091", platformLaunch, StringComparison.Ordinal);
        var posLaunch = File.ReadAllText(Path.Combine(
            root, "src", "Products", "PinoyBusinessPOS", "ExItS.PinoyBusinessPOS.Api", "Properties", "launchSettings.json"));
        Assert.Contains("http://0.0.0.0:8092", posLaunch, StringComparison.Ordinal);
        // Admin LV bind comes from Start-LocalValidation.ps1 ASPNETCORE_URLS (no LocalValidation launch profile).
        var adminLaunch = File.ReadAllText(Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Admin", "Properties", "launchSettings.json"));
        Assert.DoesNotContain("LocalValidation", adminLaunch, StringComparison.OrdinalIgnoreCase);

        var resetScript = File.ReadAllText(Path.Combine(root, "tools", "Reset-LocalValidation.ps1"));
        Assert.Contains("ConfirmReset", resetScript, StringComparison.Ordinal);
        Assert.Contains("PlatformDbVolume", resetScript, StringComparison.Ordinal);
        Assert.Contains("PosDbVolume", resetScript, StringComparison.Ordinal);
        Assert.Contains("PurgeTransactional", resetScript, StringComparison.Ordinal);
        Assert.Contains("LocalValidation.stack.ps1", resetScript, StringComparison.Ordinal);
        Assert.Contains("Get-ExItSRepositoryWorktrees", stackScript, StringComparison.Ordinal);
        Assert.Contains("Write-LocalValidationRuntimeProvenanceTable", stackScript, StringComparison.Ordinal);
        Assert.Contains("Stop-LocalValidationCrossWorktreeHostApps", stackScript, StringComparison.Ordinal);
        Assert.Contains("Assert-LocalValidationPortsOwnedByExpectedWorktree", stackScript, StringComparison.Ordinal);
        Assert.Contains("Write-LocalValidationRuntimeProvenanceTable", startScript, StringComparison.Ordinal);
        Assert.Contains("Write-LocalValidationRuntimeSummary", startScript, StringComparison.Ordinal);
        Assert.Contains("Production", resetScript, StringComparison.Ordinal);
        Assert.DoesNotContain("docker compose down -v", resetScript, StringComparison.Ordinal);
        Assert.Contains("'volume', 'rm'", resetScript, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_platform_db_data", stackScript, StringComparison.Ordinal);
        Assert.Contains("exits_local_validation_pos_db_data", stackScript, StringComparison.Ordinal);

        Assert.Contains("${LOCAL_VALIDATION_ADMIN_HOST_PORT:-8090}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_PLATFORM_API_HOST_PORT:-8091}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_POS_API_HOST_PORT:-8092}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_ORG_WEB_HOST_PORT:-8093}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_PERSONAL_WEB_HOST_PORT:-8094}:8080", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_ADMIN_WEB_REACT_HOST_PORT:-8095}:8080", live, StringComparison.Ordinal);
        Assert.Contains("PLATFORM_API_SAME_ORIGIN", live, StringComparison.Ordinal);
        Assert.Contains("PLATFORM_API_PROXY_TARGET", live, StringComparison.Ordinal);
        Assert.Contains("http://localhost:8095", live, StringComparison.Ordinal);
        Assert.Contains("http://127.0.0.1:8095", live, StringComparison.Ordinal);
        Assert.Contains(
            "PlatformEmail__AdminPublicBaseUrl: ${LOCAL_VALIDATION_ADMIN_WEB_REACT_ORIGIN:-http://localhost:8095}",
            live,
            StringComparison.Ordinal);
        Assert.DoesNotContain("100.120.79.81", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_PLATFORM_DB_HOST_PORT:-15533}:5432", live, StringComparison.Ordinal);
        Assert.Contains("${LOCAL_VALIDATION_POS_DB_HOST_PORT:-15534}:5432", live, StringComparison.Ordinal);
        Assert.DoesNotContain("PLATFORM_API_HOST_PORT:-8081", live, StringComparison.Ordinal);
        Assert.DoesNotContain("POS_API_HOST_PORT:-8082", live, StringComparison.Ordinal);
        Assert.DoesNotContain("PLATFORM_DB_HOST_PORT:-15433", live, StringComparison.Ordinal);
        Assert.DoesNotContain("POS_DB_HOST_PORT:-15434", live, StringComparison.Ordinal);

        Assert.Contains("${PLATFORM_API_HOST_PORT:-8081}:8080", packaging, StringComparison.Ordinal);
        Assert.Contains("${POS_API_HOST_PORT:-8082}:8080", packaging, StringComparison.Ordinal);
        Assert.DoesNotContain("admin-web:", packaging, StringComparison.Ordinal);
    }

    [Fact]
    public void Local_validation_env_example_exists_without_secrets_and_is_not_packaging_env()
    {
        var root = FindRepoRoot();
        var liveEnv = Path.Combine(root, "deploy", "docker", ".env.local-validation.example");
        var packagingEnv = Path.Combine(root, "deploy", "docker", ".env.example");
        Assert.True(File.Exists(liveEnv));
        Assert.True(File.Exists(packagingEnv));

        var text = File.ReadAllText(liveEnv);
        Assert.Contains("REPLACE_LOCAL_VALIDATION_PLATFORM_DB_PASSWORD", text, StringComparison.Ordinal);
        Assert.Contains("8090", text, StringComparison.Ordinal);
        Assert.DoesNotContain("exits_platform_dev_only", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLATFORM_API_HOST_PORT=8081", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Platform_api_keeps_production_secure_cookies_and_allows_http_only_for_local_validation()
    {
        var root = FindRepoRoot();
        var policyPath = Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Common", "PlatformAuthCookiePolicy.cs");
        var authPath = Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Identity", "AuthEndpoints.cs");
        var antiforgeryPath = Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Common", "PlatformBrowserAntiforgeryExtensions.cs");
        Assert.True(File.Exists(policyPath));
        var policy = File.ReadAllText(policyPath);
        var auth = File.ReadAllText(authPath);
        var antiforgery = File.ReadAllText(antiforgeryPath);
        Assert.Contains("LocalValidation:Enabled", policy, StringComparison.Ordinal);
        Assert.Contains("!environment.IsProduction()", policy, StringComparison.Ordinal);
        Assert.Contains("CookieSecurePolicy.Always", policy, StringComparison.Ordinal);
        Assert.Contains("HttpOnly = true", auth, StringComparison.Ordinal);
        Assert.Contains("PlatformAuthCookiePolicy.SessionCookieSecure", auth, StringComparison.Ordinal);
        Assert.Contains("PlatformAuthCookiePolicy.SecurePolicy", antiforgery, StringComparison.Ordinal);
        Assert.Contains("HttpOnly = true", antiforgery, StringComparison.Ordinal);
        var browserAntiforgeryPath = Path.Combine(
            root, "src", "Platform", "ExItS.Platform.Api", "Common", "PlatformBrowserAntiforgeryMiddleware.cs");
        var browserAntiforgery = File.ReadAllText(browserAntiforgeryPath);
        Assert.Contains("/api/v1/platform/auth/activate-account", browserAntiforgery, StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/auth/reset-password", browserAntiforgery, StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/auth/register", browserAntiforgery, StringComparison.Ordinal);
        Assert.Contains("/api/v1/platform/auth/forgot-password", browserAntiforgery, StringComparison.Ordinal);
    }

    [Fact]
    public void Payment_provider_auto_enables_LocalValidation_for_Staging_hosts()
    {
        // Local Validation API runs ASPNETCORE_ENVIRONMENT=Staging. Subscribe/PayNow must not
        // fall through to NullPaymentProvider just because the host is not Development/Testing.
        var root = FindRepoRoot();
        var path = Path.Combine(
            root,
            "src",
            "Platform",
            "ExItS.Platform.Infrastructure",
            "Payments",
            "PaymentProviderServiceCollectionExtensions.cs");
        Assert.True(File.Exists(path));
        var source = File.ReadAllText(path);
        Assert.Contains("LocalValidation:Enabled", source, StringComparison.Ordinal);
        Assert.Contains("localValidationEnabled && !environment.IsProduction()", source, StringComparison.Ordinal);
        Assert.Contains("PaymentProviderNames.LocalValidation", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IsDevelopment()", source, StringComparison.Ordinal);
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
