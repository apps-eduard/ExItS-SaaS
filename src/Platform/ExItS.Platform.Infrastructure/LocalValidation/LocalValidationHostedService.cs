using ExItS.Platform.Application.LocalValidation;
using ExItS.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ExItS.Platform.Infrastructure.LocalValidation;

/// <summary>
/// Applies Platform migrations and seeds local-validation identities when LocalValidation:Enabled.
/// Explicit local-validation-only path — not a Production startup Migrate().
/// </summary>
public sealed class LocalValidationHostedService(
    IServiceScopeFactory scopeFactory,
    IOptions<LocalValidationOptions> options,
    IHostEnvironment environment,
    ILogger<LocalValidationHostedService> logger) : IHostedService
{
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

        if (!options.Value.RunHostedSeed)
        {
            logger.LogInformation("LocalValidation RunHostedSeed=false; skipping hosted migrate/seed.");
            return;
        }

        logger.LogInformation(
            "LocalValidation hosted initialization beginning (Environment={Environment}, SeedScope={SeedScope}, PurgeTransactionalOnSeed={Purge}, ProcessId={ProcessId}).",
            environment.EnvironmentName,
            options.Value.SeedScope,
            options.Value.PurgeTransactionalOnSeed,
            Environment.ProcessId);

        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        var connection = db.Database.GetDbConnection();
        logger.LogInformation(
            "LocalValidation database target (no password): DataSource={DataSource}, Database={Database}.",
            connection.DataSource,
            connection.Database);

        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);

        var initializer = scope.ServiceProvider.GetRequiredService<InitializeLocalValidationDataset>();
        await initializer.ExecuteAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("LocalValidation hosted initialization finished.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
