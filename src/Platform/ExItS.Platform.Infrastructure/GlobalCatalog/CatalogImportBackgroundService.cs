using ExItS.Platform.Application.GlobalCatalog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.Infrastructure.GlobalCatalog;

/// <summary>
/// PostgreSQL-backed worker that processes Queued global-catalog import jobs in chunks.
/// No Redis. Restart-safe via Pending item status and heartbeat reclaim.
/// </summary>
public sealed class CatalogImportBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<CatalogImportBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan BusyDelay = TimeSpan.FromMilliseconds(250);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Catalog import background worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            var worked = false;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<ProcessCatalogImportChunk>();
                worked = await processor.ExecuteOnceAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Catalog import worker iteration failed.");
            }

            try
            {
                await Task.Delay(worked ? BusyDelay : IdleDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Catalog import background worker stopped.");
    }
}
