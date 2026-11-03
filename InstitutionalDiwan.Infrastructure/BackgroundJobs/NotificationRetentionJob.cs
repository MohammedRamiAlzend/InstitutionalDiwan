using InstitutionalDiwan.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Infrastructure.BackgroundJobs;

/// <summary>
/// Keeps the notification tables bounded: deletes read/dismissed inbox rows and sent outbox rows past
/// their retention window. Dead letters and unread rows are never touched.
/// </summary>
internal sealed class NotificationRetentionJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<NotificationJobsOptions> _options;
    private readonly ILogger<NotificationRetentionJob> _logger;

    public NotificationRetentionJob(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationJobsOptions> options,
        ILogger<NotificationRetentionJob> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogInformation("NotificationRetentionJob is disabled by configuration.");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, options.StartupDelaySeconds) + 30), stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(Math.Max(1, options.RetentionIntervalHours)));

            do
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var repository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                    var now = DateTime.UtcNow;

                    var recipients = await repository.PurgeRecipientsAsync(
                        now.AddDays(-Math.Max(1, options.RecipientRetentionDays)), stoppingToken);
                    var outbox = await repository.PurgeSentOutboxAsync(
                        now.AddDays(-Math.Max(1, options.SentOutboxRetentionDays)), stoppingToken);

                    _logger.LogInformation(
                        "Notification retention removed {Recipients} inbox rows and {Outbox} sent outbox rows.",
                        recipients,
                        outbox);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "NotificationRetentionJob failed; the next run will retry.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
        }
    }
}
