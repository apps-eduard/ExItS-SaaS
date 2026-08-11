using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.Api.Common;

/// <summary>Platform API security pipeline: HTTPS, CORS, rate limits, headers, Production config guard.</summary>
internal static class PlatformSecurityPipeline
{
    public const string CorsPolicyName = "platform-cors";
    public const string AuthBootstrapRateLimitPolicy = "auth-bootstrap";
    public const string AuthLoginRateLimitPolicy = "auth-login";
    public const string AuthPasswordResetRateLimitPolicy = "auth-password-reset";
    public const string AuthTokenOpsRateLimitPolicy = "auth-token-ops";
    public const string PublicIdResolveRateLimitPolicy = "public-id-resolve";
    public const string KnownDevelopmentPasswordMarker = "exits_platform_dev_only";

    public static void ValidateProductionConfigurationOrThrow(WebApplicationBuilder builder)
    {
        var env = builder.Environment;
        if (env.IsDevelopment() || env.IsEnvironment("Testing"))
        {
            return;
        }

        var connectionString = builder.Configuration.GetConnectionString("PlatformDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Production requires ConnectionStrings:PlatformDatabase from an approved secure configuration provider.");
        }

        if (connectionString.Contains(KnownDevelopmentPasswordMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Production must not use the documented development database password.");
        }

        var allowedHosts = builder.Configuration["AllowedHosts"];
        if (string.IsNullOrWhiteSpace(allowedHosts) || allowedHosts.Trim() == "*")
        {
            throw new InvalidOperationException(
                "Production requires an explicit AllowedHosts value (wildcard '*' is not allowed).");
        }

        var bootstrapEnabled = builder.Configuration.GetValue<bool>("PlatformAuthentication:Bootstrap:Enabled");
        if (bootstrapEnabled)
        {
            throw new InvalidOperationException(
                "Production must not enable PlatformAuthentication:Bootstrap:Enabled (first-admin bootstrap is forbidden in Production).");
        }

        var exposeDebugTokens = builder.Configuration.GetValue<bool>("PlatformAuthentication:Lifecycle:ExposeDebugTokens");
        if (exposeDebugTokens)
        {
            throw new InvalidOperationException(
                "Production must not enable PlatformAuthentication:Lifecycle:ExposeDebugTokens.");
        }

        var mfaEnrollment = builder.Configuration.GetValue<bool>("PlatformAuthentication:Mfa:EnrollmentEnabled");
        var mfaEnforcement = builder.Configuration.GetValue<bool>("PlatformAuthentication:Mfa:EnforcementEnabled");
        if (mfaEnrollment || mfaEnforcement)
        {
            throw new InvalidOperationException(
                "Production must not enable PlatformAuthentication:Mfa EnrollmentEnabled/EnforcementEnabled until an authorized MFA enrollment/challenge WP ships.");
        }

        var testingExternal = builder.Configuration.GetValue<bool>("PlatformAuthentication:External:TestingEndpointEnabled");
        if (testingExternal)
        {
            throw new InvalidOperationException(
                "Production must not enable PlatformAuthentication:External:TestingEndpointEnabled.");
        }

        ValidateExternalProviderOrThrow(builder, "Google");
        ValidateExternalProviderOrThrow(builder, "Facebook");

        var lifetimeHours = builder.Configuration.GetValue<int?>("PlatformAuthentication:AccessToken:LifetimeHours") ?? 8;
        var maxLifetimeHours = builder.Configuration.GetValue<int?>("PlatformAuthentication:AccessToken:MaxLifetimeHours") ?? 24;
        if (lifetimeHours < 1 || lifetimeHours > maxLifetimeHours || maxLifetimeHours > 168)
        {
            throw new InvalidOperationException(
                "Production requires PlatformAuthentication:AccessToken LifetimeHours between 1 and MaxLifetimeHours (MaxLifetimeHours ≤ 168).");
        }
    }

    private static void ValidateExternalProviderOrThrow(WebApplicationBuilder builder, string provider)
    {
        var enabled = builder.Configuration.GetValue<bool>($"PlatformAuthentication:External:{provider}:Enabled");
        if (!enabled)
        {
            return;
        }

        var clientId = builder.Configuration[$"PlatformAuthentication:External:{provider}:ClientId"];
        var clientSecret = builder.Configuration[$"PlatformAuthentication:External:{provider}:ClientSecret"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                $"Production requires ClientId and ClientSecret when PlatformAuthentication:External:{provider}:Enabled is true.");
        }
    }

    public static void AddPlatformSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                if (origins.Length == 0)
                {
                    policy.SetIsOriginAllowed(_ => false);
                    return;
                }

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
                    .AllowCredentials();
            });
        });

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = static async (context, cancellationToken) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                await context.HttpContext.Response.WriteAsJsonAsync(
                    new
                    {
                        title = "Too Many Requests",
                        status = StatusCodes.Status429TooManyRequests,
                        detail = "Request rate limit exceeded. Retry later.",
                        errorCode = "platform.rate_limit.exceeded"
                    },
                    cancellationToken).ConfigureAwait(false);
            };

            options.AddPolicy(AuthBootstrapRateLimitPolicy, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    "bootstrap:" + ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    });
            });

            var localValidationEnabled = builder.Configuration.GetValue<bool>("LocalValidation:Enabled")
                && !builder.Environment.IsProduction();

            options.AddPolicy(AuthLoginRateLimitPolicy, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    (localValidationEnabled ? "login-local-validation:" : "login:") + ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = localValidationEnabled ? 200 : 20,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = localValidationEnabled ? 20 : 0
                    });
            });

            options.AddPolicy(AuthPasswordResetRateLimitPolicy, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    "password-reset:" + ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(AuthTokenOpsRateLimitPolicy, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    (localValidationEnabled ? "token-ops-local-validation:" : "token-ops:") + ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        // Mobile org-select (bind + introspect) and Local Validation retries burn
                        // the Production 60/15m budget quickly and surface as "API unavailable".
                        PermitLimit = localValidationEnabled ? 2_000 : 60,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = localValidationEnabled ? 40 : 0
                    });
            });

            options.AddPolicy(PublicIdResolveRateLimitPolicy, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                var user = httpContext.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "anon";
                return RateLimitPartition.GetFixedWindowLimiter(
                    "public-id:" + user + ":" + ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
                    });
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (localValidationEnabled)
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        "local-validation-ip:" + ip,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            AutoReplenishment = true,
                            // Blazor Interactive Server shells fire many API reads on login/circuit open.
                            PermitLimit = 20_000,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 100
                        });
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    "ip:" + ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 240,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 1_048_576;
        });
    }

    public static void UsePlatformSecurity(this WebApplication app)
    {
        app.UseExceptionHandler(exceptionApp =>
        {
            exceptionApp.Run(async context =>
            {
                var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                var logger = context.RequestServices
                    .GetService<ILoggerFactory>()
                    ?.CreateLogger("ExItS.Platform.Api.UnhandledException");
                if (error is not null)
                {
                    logger?.LogError(
                        error,
                        "Unhandled platform exception. TraceId={TraceId} Path={Path}",
                        context.TraceIdentifier,
                        context.Request.Path.Value);
                }

                var env = context.RequestServices.GetService<IHostEnvironment>();
                var detail = env?.IsDevelopment() == true && error is not null
                    ? $"{error.GetType().Name}: {error.Message}"
                    : "An unexpected error occurred.";

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "An unexpected error occurred.",
                    status = StatusCodes.Status500InternalServerError,
                    detail,
                    errorCode = "platform.internal_error",
                    traceId = context.TraceIdentifier
                }).ConfigureAwait(false);
            });
        });

        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault();
            correlationId = string.IsNullOrWhiteSpace(correlationId) ? context.TraceIdentifier : correlationId.Trim();
            context.Items["CorrelationId"] = correlationId;
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
                headers["X-Correlation-Id"] = correlationId;
                headers.TryAdd("X-Content-Type-Options", "nosniff");
                headers.TryAdd("X-Frame-Options", "DENY");
                headers.TryAdd("Referrer-Policy", "no-referrer");
                headers.TryAdd("Cache-Control", "no-store");
                headers.TryAdd("Pragma", "no-cache");
                return Task.CompletedTask;
            });

            await next().ConfigureAwait(false);
        });

        var enforceHttps = app.Configuration.GetValue<bool?>("Security:EnforceHttps")
            ?? (!app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"));

        if (enforceHttps)
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseCors(CorsPolicyName);
        app.UseRateLimiter();
    }
}
