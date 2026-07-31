using System.Net.Http.Json;
using System.Text.Json;
using ExItS.PinoyBusinessPOS.Application.LivePreview;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.PinoyBusinessPOS.Infrastructure.LivePreview;

/// <summary>
/// Applies POS migrations and seeds POS-local roles for live-preview identities.
/// Explicit preview-only path — not a Production startup Migrate().
/// </summary>
public sealed class PosLivePreviewHostedService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<PosLivePreviewOptions> options,
    IHostEnvironment environment,
    ILogger<PosLivePreviewHostedService> logger) : IHostedService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            if (options.Value.Enabled)
            {
                throw new InvalidOperationException("LivePreview:Enabled=true is forbidden in Production.");
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
                "LivePreview:PlatformApiBaseUrl must be configured when LivePreview:Enabled=true for POS.");
        }

        logger.LogInformation("POS LivePreview hosted initialization beginning (non-Production).");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var identities = await WaitForIdentitiesAsync(cancellationToken).ConfigureAwait(false);
        var initializer = scope.ServiceProvider.GetRequiredService<InitializePosLivePreviewRoles>();
        await initializer.ExecuteAsync(identities, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("POS LivePreview hosted initialization finished.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task<IReadOnlyList<PlatformLivePreviewIdentityDto>> WaitForIdentitiesAsync(CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("LivePreviewPlatformApi");
        Exception? last = null;
        for (var attempt = 1; attempt <= 60; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                using var response = await client.GetAsync("/api/v1/platform/live-preview/identities", ct)
                    .ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var list = await response.Content
                        .ReadFromJsonAsync<List<PlatformLivePreviewIdentityDto>>(JsonOptions, ct)
                        .ConfigureAwait(false);
                    if (list is { Count: > 0 })
                    {
                        return list;
                    }
                }
                else
                {
                    logger.LogWarning(
                        "Waiting for Platform live-preview identities (HTTP {Status}) attempt {Attempt}.",
                        (int)response.StatusCode,
                        attempt);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                last = ex;
                logger.LogWarning(ex, "Waiting for Platform live-preview identities attempt {Attempt}.", attempt);
            }

            await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
        }

        throw new InvalidOperationException(
            "Timed out waiting for Platform live-preview identities.", last);
    }
}
