using ExItS.Platform.Application.LivePreview;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.LivePreview;

/// <summary>
/// Applies Platform migrations and seeds live-preview identities when LivePreview:Enabled.
/// Explicit preview-only path — not a Production startup Migrate().
/// </summary>
public sealed class LivePreviewHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<LivePreviewOptions> options,
    IHostEnvironment environment,
    ILogger<LivePreviewHostedService> logger) : IHostedService
{
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

        logger.LogInformation("LivePreview hosted initialization beginning (non-Production).");

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var initializer = scope.ServiceProvider.GetRequiredService<InitializeLivePreviewDataset>();
        await initializer.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("LivePreview hosted initialization finished.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
