using GotIssues.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GotIssues.Api.Health;

/// <summary>
/// Probes the database with a real connection attempt. A health check that cannot
/// fail is worse than none: T-0001 AC3 requires this to report unhealthy when
/// PostgreSQL is down, so the probe must actually reach it rather than inspect
/// configuration.
/// </summary>
public sealed class DatabaseHealthCheck(GotIssuesDbContext dbContext) : IHealthCheck
{
    /// <summary>
    /// A hung host (as opposed to a refused connection) can leave the driver waiting
    /// far longer than the container health probe's own timeout, so the probe is
    /// bounded here rather than relying on the default connection timeout.
    /// </summary>
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);

        try
        {
            var reachable = await dbContext.Database.CanConnectAsync(timeout.Token)
                .ConfigureAwait(false);

            return reachable
                ? HealthCheckResult.Healthy("database reachable")
                : HealthCheckResult.Unhealthy("database not reachable");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"database probe timed out after {ProbeTimeout.TotalSeconds:0}s");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // The exception type and message describe the connection failure, not
            // user data or credentials — safe to surface to an operator.
            return HealthCheckResult.Unhealthy("database not reachable", ex);
        }
    }
}
