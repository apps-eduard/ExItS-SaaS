using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.LocalValidation;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Infrastructure.LocalValidation;

/// <summary>
/// Applies POS migrations and seeds POS-local roles for local-validation identities.
/// Explicit preview-only path — not a Production startup Migrate().
/// </summary>
public sealed class PosLocalValidationHostedService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<PosLocalValidationOptions> options,
    IHostEnvironment environment,
    ILogger<PosLocalValidationHostedService> logger) : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            if (options.Value.Enabled)
            {
                throw new InvalidOperationException("LocalValidation:Enabled=true is forbidden in Production.");
            }

            return;
        }

        if (!options.Value.Enabled)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(options.Value.PlatformApiBaseUrl)
            || !Uri.TryCreate(options.Value.PlatformApiBaseUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                "LocalValidation:PlatformApiBaseUrl must be configured when LocalValidation:Enabled=true for POS.");
        }

        logger.LogInformation("POS LocalValidation hosted initialization beginning (non-Production).");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var identities = await WaitForIdentitiesAsync(cancellationToken).ConfigureAwait(false);
        var initializer = scope.ServiceProvider.GetRequiredService<InitializePosLocalValidationRoles>();
        await initializer.ExecuteAsync(identities, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("POS LocalValidation hosted initialization finished.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<IReadOnlyList<PlatformLocalValidationIdentityDto>> WaitForIdentitiesAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("LocalValidationPlatformApi");
        Exception? last = null;
        for (var attempt = 1; attempt <= 60; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var response = await client.GetAsync("/api/v1/platform/local-validation/seed-identities", ct)
                    .ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var list = await response.Content
                        .ReadFromJsonAsync<List<PlatformLocalValidationIdentityDto>>(JsonOptions, ct)
                        .ConfigureAwait(false);
                    if (list is { Count: > 0 })
                    {
                        return list;
                    }
                }
                else
                {
                    logger.LogWarning(
                        "Waiting for Platform local-validation identities (HTTP {Status}) attempt {Attempt}.",
                        (int)response.StatusCode,
                        attempt);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                logger.LogWarning(ex, "Waiting for Platform local-validation identities attempt {Attempt}.", attempt);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            "Timed out waiting for Platform local-validation identities.", last);
    }
}
