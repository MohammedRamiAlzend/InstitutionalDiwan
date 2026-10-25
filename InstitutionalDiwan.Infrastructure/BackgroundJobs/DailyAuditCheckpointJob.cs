using InstitutionalDiwan.Application.Features.Audit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically seals every audit entry added since the previous checkpoint into one anchor. Running
/// it when no new entries exist is a no-op, so the job is safe to retry.
/// </summary>
internal sealed class DailyAuditCheckpointJob : PeriodicAuditJob
{
    public DailyAuditCheckpointJob(
        IServiceScopeFactory scopeFactory,
        IOptions<AuditJobsOptions> options,
        ILogger<DailyAuditCheckpointJob> logger)
        : base(scopeFactory, options, logger)
    {
    }

    protected override string JobName => nameof(DailyAuditCheckpointJob);

    protected override TimeSpan GetInterval(AuditJobsOptions options) =>
        TimeSpan.FromHours(Math.Max(1, options.CheckpointIntervalHours));

    protected override TimeSpan GetInitialDelay(AuditJobsOptions options) =>
        base.GetInitialDelay(options) +
        TimeSpan.FromMinutes(Math.Clamp(options.CheckpointOffsetMinutes, 0, 1440));

    protected override async Task RunOnceAsync(
        IServiceProvider services,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var result = await services
            .GetRequiredService<ISender>()
            .Send(new CreateAuditCheckpointCommand(actorUserId), cancellationToken);

        LogOutcome("audit checkpoint", result);
    }
}
