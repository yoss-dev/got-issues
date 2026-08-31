using Duende.IdentityServer.EntityFramework.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GotIssues.IdentityHost.Configuration;

/// <summary>
/// Probes the configuration store, not just the process.
///
/// A liveness-only check reported <c>Healthy</c> on a host whose schema had been
/// dropped, while discovery returned 500 and no token could be issued — found during
/// T-0010's review. Because the API gates on
/// <c>depends_on: identity: service_healthy</c>, that check passing on a host that
/// cannot issue tokens is exactly the "looked healthy" failure that hid another
/// defect on this same ticket.
/// </summary>
public sealed class ConfigurationStoreHealthCheck(ConfigurationDbContext store) : IHealthCheck
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(3);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(ProbeTimeout);

        try
        {
            // Reading a client proves the schema exists and is queryable, which a
            // connection test alone does not.
            _ = await store.Clients.AsNoTracking()
                .Select(c => c.ClientId)
                .FirstOrDefaultAsync(timeout.Token)
                .ConfigureAwait(false);

            return HealthCheckResult.Healthy("configuration store reachable");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                $"configuration store probe timed out after {ProbeTimeout.TotalSeconds:0}s");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("configuration store not reachable", ex);
        }
    }
}
