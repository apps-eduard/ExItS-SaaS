using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ExItS.Platform.Api.Common;

/// <summary>Platform API security pipeline: HTTPS, CORS, rate limits, headers, Production config guard.</summary>
internal static class PlatformSecurityPipeline
{
    public const string CorsPolicyName = "platform-cors";
    public const string AuthBootstrapRateLimitPolicy = "auth-bootstrap";
    public const string AuthLoginRateLimitPolicy = "auth-login";
    public const string AuthPasswordResetRateLimitPolicy = "auth-password-reset";
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

            options.AddPolicy(AuthLoginRateLimitPolicy, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                return RateLimitPartition.GetFixedWindowLimiter(
                    "login:" + ip,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(15),
                        QueueLimit = 0
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

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
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
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "An unexpected error occurred.",
                    status = StatusCodes.Status500InternalServerError,
                    detail = "An unexpected error occurred.",
                    errorCode = "platform.internal_error",
                    traceId = context.TraceIdentifier
                }).ConfigureAwait(false);
            });
        });

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var headers = context.Response.Headers;
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
