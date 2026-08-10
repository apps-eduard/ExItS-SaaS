using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace ExItS.PinoyBusinessPOS.Api.Common;

/// <summary>POS API security pipeline: HTTPS, CORS, rate limits, headers, safe exceptions.</summary>
internal static class PosSecurityPipeline
{
    public const string SensitivePolicyName = "pos-sensitive";
    public const string CorsPolicyName = "pos-cors";

    public static void AddPosSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
                if (origins.Length == 0)
                {
                    // Deny-by-default: no browser origins. MAUI/native clients are not CORS-bound.
                    policy.SetIsOriginAllowed(_ => false);
                    return;
                }

                policy.WithOrigins(origins)
                    .AllowAnyHeader()
                    .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS");
                // Never AllowCredentials with wildcard — origins are explicit allowlist only.
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
                        errorCode = "pos.rate_limit.exceeded"
                    },
                    cancellationToken).ConfigureAwait(false);
            };

            // Partition by organization header when present, else by remote IP — one tenant cannot starve all others.
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var partitionKey = ResolvePartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 240,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });

            options.AddPolicy(SensitivePolicyName, httpContext =>
            {
                var partitionKey = "sens:" + ResolvePartitionKey(httpContext);
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey,
                    _ => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    });
            });
        });

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxRequestBodySize = 1_048_576; // 1 MiB
        });
    }

    public static void UsePosSecurity(this WebApplication app)
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
                    errorCode = "pos.internal_error",
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

    public static bool IsSensitivePath(PathString path)
    {
        var value = path.Value ?? string.Empty;
        return value.Contains("/sales", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/repayments", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/credit-entries", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/void", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/reverse", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/reports", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/dashboard", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/by-barcode", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/by-sku", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/dev/", StringComparison.OrdinalIgnoreCase)
               || value.Contains("/expenses", StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolvePartitionKey(HttpContext httpContext)
    {
        if (httpContext.Request.Headers.TryGetValue(PosOrganizationHeaders.OrganizationHeaderName, out var org)
            && !string.IsNullOrWhiteSpace(org.FirstOrDefault()))
        {
            return "org:" + org.First()!.Trim().ToLowerInvariant();
        }

        return "ip:" + (httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
    }
}
