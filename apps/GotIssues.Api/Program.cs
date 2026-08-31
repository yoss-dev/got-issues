using System.Text.Json;
using GotIssues.Api;
using GotIssues.Api.Data;
using GotIssues.Api.Health;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("GotIssues")
    ?? throw new InvalidOperationException(
        "Connection string 'GotIssues' is not configured. It is supplied by the "
        + "environment (see .env.example); the application never embeds credentials.");

builder.Services.AddControllers();
builder.Services.AddDbContext<GotIssuesDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

// --- Explicit migration step -------------------------------------------------
// ADR-0003: migrations are applied by a deliberate, observable action, never
// silently at API startup. The compose stack runs this image once with --migrate
// as its own short-lived service; the API service starts only after it exits 0.
// Normal startup below reaches no migration code at all (T-0001 AC5).
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();

    MigrationLogging.ApplyingMigrations(app.Logger);
    await context.Database.MigrateAsync().ConfigureAwait(false);
    MigrationLogging.MigrationsApplied(app.Logger);
    return;
}

// Operational endpoint. Deliberately NOT declared in spec/openapi.yaml — ADR-0005
// puts health, readiness and metrics outside the API contract. Documented in the
// repository README instead, because operators are a different audience from the
// clients that generate against the specification.
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = static async (context, report) =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                }),
        })).ConfigureAwait(false);
    },
});

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);
