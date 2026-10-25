using InstitutionalDiwan.Application.Features.Audit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InstitutionalDiwan.Infrastructure.BackgroundJobs;

/// <summary>
/// Periodically walks the whole audit chain, recomputes every SHA-256 event hash, and stores a pass
/// or fail report. It runs outside a request, so it appends an explicit self-audit entry for the run.
/// </summary>
internal sealed class AuditChainIntegrityVerificationJob : PeriodicAuditJob
{
    public AuditChainIntegrityVerificationJob(
        IServiceScopeFactory scopeFactory,
        IOptions<AuditJobsOptions> options,
        ILogger<AuditChainIntegrityVerificationJob> logger)
        : base(scopeFactory, options, logger)
    {
    }

    protected override string JobName => nameof(AuditChainIntegrityVerificationJob);

    protected override TimeSpan GetInterval(AuditJobsOptions options) =>
        TimeSpan.FromHours(Math.Max(1, options.IntegrityCheckIntervalHours));

    protected override async Task RunOnceAsync(
        IServiceProvider services,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var result = await services
            .GetRequiredService<ISender>()
            .Send(new RunAuditIntegrityCheckCommand(actorUserId), cancellationToken);

        LogOutcome("integrity verification", result);
    }
}
