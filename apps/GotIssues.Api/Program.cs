using System.Text.Json;
using GotIssues.Api;
using GotIssues.Api.Data;
using GotIssues.Api.Health;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("GotIssues")
    ?? throw new InvalidOperationException(
        "Connection string 'GotIssues' is not configured. It is supplied by the "
        + "environment (see .env.example); the application never embeds credentials.");

// Resource server only: the API validates tokens the identity host issued and never
// handles credentials (ADR-0003). Authentication is optional at this stage so the API
// still runs standalone — T-0009 turns the role claim into policies.
var authority = builder.Configuration["Authentication:Authority"];
if (!string.IsNullOrWhiteSpace(authority))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Metadata is fetched over the Compose network, but tokens carry the
            // identity host's fixed IssuerUri — so the two addresses differ by design
            // and the issuer is validated against the configured value, not the
            // address metadata came from.
            options.Authority = authority;
            options.MetadataAddress = builder.Configuration["Authentication:MetadataAddress"]
                ?? $"{authority.TrimEnd('/')}/.well-known/openid-configuration";
            options.Audience = builder.Configuration["Authentication:Audience"];
            options.RequireHttpsMetadata =
                builder.Configuration.GetValue("Authentication:RequireHttpsMetadata", true);
            options.TokenValidationParameters.ValidIssuer = authority;
        });

    builder.Services.AddAuthorization();
}

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

if (!string.IsNullOrWhiteSpace(authority))
{
    app.UseAuthentication();
    app.UseAuthorization();

    // Operational endpoint proving the token round trip end to end. Outside the API
    // contract by ADR-0005 — no product endpoint exists yet, and inventing one to be
    // authenticated against would be product surface built only for a test.
    app.MapGet("/health/authenticated", () => Results.Ok(new { status = "authenticated" }))
        .RequireAuthorization();
}

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);

// Exposed so WebApplicationFactory<Program> can locate the entry point. The API
// uses top-level statements, whose generated Program class is internal; this makes
// it public without changing behaviour. Driven by T-0003's integration tier.
public partial class Program;
