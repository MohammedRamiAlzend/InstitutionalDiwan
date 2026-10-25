using InstitutionalDiwan.Domain.Entities.Audit;
using InstitutionalDiwan.Domain.Errors;
using InstitutionalDiwan.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Infrastructure.BackgroundJobs;

/// <summary>
/// Shared scheduling loop for the audit maintenance jobs. It waits for startup to finish, resolves
/// an actor account per run, and never lets one failed run stop the timer.
/// </summary>
internal abstract class PeriodicAuditJob : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<AuditJobsOptions> _options;
    private readonly ILogger _logger;

    protected PeriodicAuditJob(
        IServiceScopeFactory scopeFactory,
        IOptions<AuditJobsOptions> options,
        ILogger logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected abstract TimeSpan GetInterval(AuditJobsOptions options);

    protected virtual TimeSpan GetInitialDelay(AuditJobsOptions options) =>
        TimeSpan.FromSeconds(Math.Max(0, options.StartupDelaySeconds));

    protected abstract string JobName { get; }

    protected abstract Task RunOnceAsync(
        IServiceProvider services,
        Guid actorUserId,
        CancellationToken cancellationToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = _options.Value;

        if (!options.Enabled)
        {
            _logger.LogInformation("{JobName} is disabled by configuration.", JobName);
            return;
        }

        try
        {
            await Task.Delay(GetInitialDelay(options), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        using var timer = new PeriodicTimer(GetInterval(options));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var actorUserId = await ResolveActorAsync(scope.ServiceProvider, options, stoppingToken);

                if (actorUserId is null)
                {
                    _logger.LogWarning(
                        "{JobName} skipped: no active user is available to attribute the run to.",
                        JobName);
                }
                else
                {
                    await RunOnceAsync(scope.ServiceProvider, actorUserId.Value, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "{JobName} failed; the next scheduled run will retry.", JobName);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                    break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    protected void LogOutcome(string operation, Domain.Common.Result result)
    {
        if (result.IsSuccess)
        {
            _logger.LogInformation("{JobName} completed {Operation}.", JobName, operation);
            return;
        }

        if (result.Error.Code == DomainErrors.Audit.NoAuditLogs.Code ||
            result.Error.Code == DomainErrors.Audit.NothingToCheckpoint.Code)
        {
            _logger.LogDebug(
                "{JobName} skipped {Operation}: {ErrorCode}.",
                JobName,
                operation,
                result.Error.Code);
            return;
        }

        _logger.LogWarning(
            "{JobName} could not complete {Operation}: {ErrorCode} - {ErrorDescription}.",
            JobName,
            operation,
            result.Error.Code,
            result.Error.Description);
    }

    private static async Task<Guid?> ResolveActorAsync(
        IServiceProvider services,
        AuditJobsOptions options,
        CancellationToken cancellationToken)
    {
        if (options.ActorUserId is { } configuredActorId)
            return configuredActorId;

        var users = await services
            .GetRequiredService<IUserRepository>()
            .ListAllAsync(cancellationToken);

        return users.FirstOrDefault(user => user.IsActive)?.UserId;
    }
}
