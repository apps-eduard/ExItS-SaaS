using ExItS.Platform.Application.Personal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ExItS.Platform.Infrastructure.Personal;

/// <summary>
/// Delivers due Personal Utang reminders and Personal To-do reminders as in-app notifications.
/// Push remains optional via <see cref="IPersonalPushNotificationSink"/> (null sink skips vendor push).
/// </summary>
public sealed class PersonalReminderDeliveryBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<PersonalReminderDeliveryBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan BusyDelay = TimeSpan.FromSeconds(5);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Personal reminder delivery worker started.");
        while (!stoppingToken.IsCancellationRequested)
        {
            var delivered = 0;
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider.GetRequiredService<ProcessDuePersonalReminders>();
                delivered = await processor.ExecuteOnceAsync(take: 50, stoppingToken).ConfigureAwait(false);
                if (delivered > 0)
                {
                    logger.LogInformation("Personal reminder worker delivered {Count} reminder(s).", delivered);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Personal reminder worker iteration failed.");
            }

            try
            {
                await Task.Delay(delivered > 0 ? BusyDelay : IdleDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Personal reminder delivery worker stopped.");
    }
}
