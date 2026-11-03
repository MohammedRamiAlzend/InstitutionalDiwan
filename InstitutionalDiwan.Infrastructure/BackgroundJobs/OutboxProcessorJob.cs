using System.Text.Json;
using InstitutionalDiwan.Application.Abstractions.Notifications;
using InstitutionalDiwan.Domain.Common;
using InstitutionalDiwan.Domain.Entities.Notifications;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Infrastructure.BackgroundJobs;

/// <summary>
/// Delivers pending notification outbox rows. An IN_APP row fans the stored payload out to every
/// recipient through <see cref="IRealtimeNotifier"/>; a failure backs off exponentially and ends as
/// a dead letter after the configured number of attempts. The inbox rows are already committed, so a
/// failed push never loses a notification.
/// </summary>
internal sealed class OutboxProcessorJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<NotificationJobsOptions> _options;
    private readonly ILogger<OutboxProcessorJob> _logger;

    public OutboxProcessorJob(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationJobsOptions> options,
        ILogger<OutboxProcessorJob> logger)
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
            _logger.LogInformation("OutboxProcessorJob is disabled by configuration.");
            return;
        }

        try
        {
            await Task.Delay(TimeSpan.FromSeconds(Math.Max(0, options.StartupDelaySeconds)), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(Math.Max(1, options.OutboxPollSeconds)));

        do
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                await ProcessBatchAsync(scope.ServiceProvider, options, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "OutboxProcessorJob poll failed; the next poll will retry.");
            }
        }
        while (await WaitAsync(timer, stoppingToken));
    }

    private static async Task<bool> WaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }

    internal async Task ProcessBatchAsync(
        IServiceProvider services,
        NotificationJobsOptions options,
        CancellationToken cancellationToken)
    {
        var repository = services.GetRequiredService<INotificationRepository>();
        var unitOfWork = services.GetRequiredService<IUnitOfWork>();
        var notifier = services.GetRequiredService<IRealtimeNotifier>();

        var due = await repository.GetDueOutboxAsync(unitOfWork.UtcNow, Math.Max(1, options.OutboxBatchSize), cancellationToken);

        foreach (var outbox in due)
        {
            try
            {
                if (outbox.ChannelId != NotificationChannel.InAppId)
                {
                    // Only IN_APP is delivered today; an unsupported channel must not retry forever.
                    outbox.RecordFailure($"Channel {outbox.ChannelId} is not supported.", null, maxRetries: 1);
                }
                else
                {
                    await DeliverInAppAsync(repository, notifier, outbox, cancellationToken);
                    outbox.MarkProcessed(unitOfWork.UtcNow);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                var attempt = outbox.RetryCount + 1;
                var delay = TimeSpan.FromSeconds(options.RetryBaseSeconds * Math.Pow(2, Math.Min(attempt - 1, 10)));
                var error = exception.Message.Length > 900 ? exception.Message[..900] : exception.Message;
                outbox.RecordFailure(error, unitOfWork.UtcNow.Add(delay), options.MaxRetries);
                _logger.LogWarning(exception, "Outbox row {OutboxId} failed (attempt {Attempt}).", outbox.OutboxId, attempt);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    // The publisher writes camelCase JSON.
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web);

    private static async Task DeliverInAppAsync(
        INotificationRepository repository,
        IRealtimeNotifier notifier,
        NotificationOutbox outbox,
        CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<OutboxPayload>(outbox.PayloadJson, PayloadOptions)
            ?? throw new InvalidOperationException("The outbox payload is empty.");

        var recipients = await repository.GetRecipientsAsync(outbox.NotificationId, cancellationToken);

        foreach (var recipient in recipients.Where(recipient => !recipient.IsDismissed))
        {
            await notifier.NotifyUserAsync(
                recipient.UserId,
                new NotificationPush(
                    recipient.RecipientId,
                    outbox.NotificationId,
                    payload.TypeId,
                    payload.TitleAr,
                    payload.BodyAr,
                    payload.PriorityId,
                    payload.ActionUrl,
                    payload.CreatedAt),
                cancellationToken);
        }
    }

    /// <summary>The JSON the publisher stores in <c>PayloadJson</c> for an IN_APP row.</summary>
    internal sealed record OutboxPayload(
        int TypeId,
        string TitleAr,
        string BodyAr,
        int PriorityId,
        string? ActionUrl,
        DateTime CreatedAt);
}
