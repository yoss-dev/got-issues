using GotIssues.Api.Data;
using GotIssues.Api.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace GotIssues.Api.UnitTests;

/// <summary>
/// The health check's error handling, without a container. It connects to a port
/// nothing listens on, so the attempt is refused immediately rather than timing out —
/// deterministic and fast on any machine.
/// </summary>
public sealed class DatabaseHealthCheckTests
{
    private static GotIssuesDbContext ContextPointingAt(string connectionString)
    {
        var options = new DbContextOptionsBuilder<GotIssuesDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new GotIssuesDbContext(options);
    }

    [Fact]
    public async Task Reports_unhealthy_rather_than_throwing_when_the_database_is_unreachable()
    {
        using var context = ContextPointingAt("Host=127.0.0.1;Port=1;Database=nope;Timeout=1");
        var check = new DatabaseHealthCheck(context);

        var result = await check.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.NotNull(result.Description);
    }

    [Fact]
    public async Task Honours_a_cancelled_caller_token()
    {
        using var context = ContextPointingAt("Host=127.0.0.1;Port=1;Database=nope;Timeout=1");
        var check = new DatabaseHealthCheck(context);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        // A caller's cancellation must propagate, not be swallowed as "unhealthy" —
        // the check distinguishes its own probe timeout from the caller giving up.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => check.CheckHealthAsync(new HealthCheckContext(), cancelled.Token));
    }
}
