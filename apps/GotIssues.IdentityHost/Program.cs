using System.Reflection;
using GotIssues.IdentityHost;
using GotIssues.IdentityHost.Configuration;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Identity")
    ?? throw new InvalidOperationException(
        "Connection string 'Identity' is not configured. It is supplied by the "
        + "environment (see .env.example); the application never embeds credentials.");

var seed = new SeedConfiguration();
builder.Configuration.GetSection(SeedConfiguration.SectionName).Bind(seed);

var migrationsAssembly = Assembly.GetExecutingAssembly().GetName().Name;
var store = IdentityStore.Npgsql(connectionString, migrationsAssembly);

builder.Services.AddIdentityServer(options =>
    {
        // Fixed issuer. Without it the issuer is derived from the request host, so a
        // token fetched from the host and one fetched inside the Compose network would
        // carry different issuers and the API would reject one of them.
        options.IssuerUri = builder.Configuration["Identity:IssuerUri"];
    })
    // Clients, scopes and resources live in the identity schema, not in code, so
    // seeding is a real persisted action and AC9's idempotence is a real property.
    .AddConfigurationStore(options =>
    {
        options.ConfigureDbContext = store;
        options.DefaultSchema = IdentityStore.Schema;
    })
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = store;
        options.DefaultSchema = IdentityStore.Schema;
    })
    // Developer signing credential persisted to a mounted path, so restarting the host
    // does not invalidate tokens issued moments earlier. Never committed: the path is a
    // Docker volume (T-0010 AC7).
    .AddDeveloperSigningCredential(
        persistKey: true,
        filename: builder.Configuration["Identity:SigningKeyPath"] ?? "/app/keys/tempkey.jwk");

builder.Services.AddHealthChecks()
    .AddCheck<ConfigurationStoreHealthCheck>("configuration-store");

var app = builder.Build();

// --- Explicit migration and seeding step ------------------------------------
// ADR-0003: schema changes are a deliberate, observable action, run as their own
// short-lived Compose service. Ordinary startup below reaches neither migration nor
// seeding — the same shape T-0001 established for the API.
if (args.Contains("--migrate", StringComparer.Ordinal))
{
    IdentityHostLogging.MigratingAndSeeding(app.Logger);
    await DatabaseSeeder.MigrateAndSeedAsync(app.Services, seed).ConfigureAwait(false);
    IdentityHostLogging.MigratedAndSeeded(app.Logger);
    return;
}

// Operational endpoint, outside the API contract (ADR-0005).
app.MapHealthChecks("/health");
app.UseIdentityServer();

await app.RunAsync().ConfigureAwait(false);
