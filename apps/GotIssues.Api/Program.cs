using System.Text.Json;
using GotIssues.Api;
using GotIssues.Api.Authentication;
using GotIssues.Api.Authorization;
using GotIssues.Api.Data;
using GotIssues.Api.Health;
using GotIssues.Api.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
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

            // Keep claims as the token wrote them. By default JwtBearer remaps
            // well-known short names to long WS-Federation URIs, so the identity
            // host's `role` claim would arrive as
            // http://schemas.microsoft.com/ws/2008/06/identity/claims/role and the
            // policies — which look for `role` — would match nothing and refuse
            // every caller, including a genuine admin. It fails closed, so no test
            // of a permitted path would notice unless it used a real token.
            options.MapInboundClaims = false;

            // With mapping off, the role claim is named `role`, but RoleClaimType
            // still defaults to the WS-Federation URI — so User.IsInRole("admin")
            // would return false for a genuine admin. Nothing uses it today, and it
            // is the idiomatic API the next person will reach for; pointing it at the
            // real claim removes a silent negative of exactly the family that made
            // the policies refuse everyone.
            options.TokenValidationParameters.RoleClaimType = "role";

            // Same reasoning for the name. Claims arrive verbatim, so leaving this at
            // its default would make User.Identity.Name null for a token that plainly
            // carries `name` — latent today because client-credentials tokens carry
            // none, and live the moment T-0010's provisioning produces user tokens.
            options.TokenValidationParameters.NameClaimType = "name";
        });

    builder.Services.AddAuthorization(options => options.AddGotIssuesPolicies());
}

// Every failure returns application/problem+json, including the ones the framework
// produces without reaching a controller — an unauthenticated 401 is the most common
// failure a client meets, and it was returning an empty body while the specification
// declared a problem document for it. UseStatusCodePages fills in status-only
// responses; AddProblemDetails gives them the RFC 9457 shape.
builder.Services.AddProblemDetails();

builder.Services.AddControllers()
    // The generated contract's enums declare their wire values with [EnumMember], which
    // System.Text.Json does not read. Without this the API answers `"status": 2` where
    // the specification declares `enum: [open, in_progress, done]`.
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new EnumMemberJsonConverter()));
builder.Services.AddDbContext<GotIssuesDbContext>(options =>
    options.UseNpgsql(connectionString));
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

var app = builder.Build();

// An unhandled exception must still answer with the shape the contract declares.
// UseStatusCodePages below only fills in responses that have a status and no body;
// an exception escaping a controller produces neither, so the caller received a
// 500 with a zero-length body and no Content-Type — a response the specification
// never declared, found by acceptance on a project name containing U+0000
// (PostgreSQL SQLSTATE 22021, which is not a unique violation and so correctly
// escaped the controller's narrow catch).
//
// The narrow catch is right. What was missing is a destination for everything it
// deliberately does not catch.
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/problem+json";

    // Serialised explicitly rather than with WriteAsJsonAsync, which overwrites
    // Content-Type with `application/json` — the exact defect this handler exists to
    // end, committed by the handler itself. Caught by the smoke check below, and only
    // after that check was fixed to reach its own assertion.
    //
    // Nothing from the exception reaches the caller: the message can carry the
    // offending value, and a project name is user-supplied text (SECURITY.md).
    var problem = JsonSerializer.Serialize(
        new ProblemDetails
        {
            Type = "https://httpstatuses.io/500",
            Title = "An unexpected error occurred.",
            Status = StatusCodes.Status500InternalServerError,
        },
        new JsonSerializerOptions(JsonSerializerDefaults.Web));

    await context.Response.WriteAsync(problem).ConfigureAwait(false);
}));

app.UseStatusCodePages();

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
    // One definition of the order, shared with the integration test host so the two
    // cannot drift — see AuthenticationPipeline.
    app.UseGotIssuesAuthentication();

    // Operational endpoint proving the token round trip end to end, outside the API
    // contract by ADR-0005.
    //
    // It was originally justified by there being no product endpoint to authenticate
    // against. That stopped being true when T-0004 shipped /projects, and the
    // justification is kept for a different reason: this endpoint asserts the round
    // trip *without* a database, a schema or a role, so it still fails for exactly one
    // reason. A product endpoint used in its place would fail for several, and the four
    // smoke cases that reach this endpoint — issued, expired, wrong-audience,
    // unknown-key — exist precisely to tell those apart.
    //
    // **The precondition, so the next reader can tell when this expires.** It is
    // database-free only because the tokens this system issues carry no `sub`, so
    // UserProjectionMiddleware finds no subject and never writes. T-0018 makes tokens
    // carry a subject; on the day it lands, this endpoint starts touching the database
    // and this justification becomes false — in the same quiet way the one it replaced
    // did. Whoever implements T-0018 should either re-establish the property or move
    // those four smoke cases somewhere that still has it.
    app.MapGet("/health/authenticated", () => Results.Ok(new { status = "authenticated" }))
        .RequireAuthorization();
}

app.MapControllers();

await app.RunAsync().ConfigureAwait(false);

// Exposed so WebApplicationFactory<Program> can locate the entry point. The API
// uses top-level statements, whose generated Program class is internal; this makes
// it public without changing behaviour. Driven by T-0003's integration tier.
public partial class Program;
