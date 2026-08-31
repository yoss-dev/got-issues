using System.Net;
using System.Text.Json;
using GotIssues.Api.Data;
using GotIssues.Api.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace GotIssues.Api.IntegrationTests;

/// <summary>
/// Covers T-0001's stack behaviour, which shipped with manual verification only
/// because this harness depends on it (T-0003 AC8). These are the tests that close
/// T-0001's recorded Definition of Done deviation.
/// </summary>
[Collection(PostgresFixtureDefinition.Name)]
public sealed class StackBehaviourTests(PostgresContainerFixture postgres) : IAsyncLifetime
{
    private string _connectionString = string.Empty;
    private ApiFactory _factory = null!;

    public async Task InitializeAsync()
    {
        _connectionString = await postgres.CreateDatabaseAsync().ConfigureAwait(false);
        _factory = new ApiFactory(_connectionString);
        await _factory.ApplyMigrationsAsync().ConfigureAwait(false);
    }

    public Task DisposeAsync()
    {
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Migrations_create_the_schema()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT table_name FROM information_schema.tables WHERE table_schema = 'public'";

        var tables = new List<string>();
        await using (var reader = await command.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        Assert.Contains("projects", tables);
        Assert.Contains("__EFMigrationsHistory", tables);
    }

    [Fact]
    public async Task Health_reports_healthy_when_the_database_is_reachable()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("Healthy", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Health_reports_unhealthy_when_the_database_is_unreachable()
    {
        // Points the app at a port nothing listens on, so the probe fails fast.
        // A health check that cannot fail is worse than none — this is the assertion
        // that makes the endpoint's 200 mean something.
        var unreachable = new NpgsqlConnectionStringBuilder(_connectionString)
        {
            Host = "127.0.0.1",
            Port = 1,
            Timeout = 1,
        }.ConnectionString;

        using var factory = new ApiFactory(unreachable);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var document = JsonDocument.Parse(body);
        Assert.Equal("Unhealthy", document.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task The_api_talks_to_real_postgresql_not_an_in_memory_provider()
    {
        // TESTING.md forbids the in-memory provider: it enforces no constraints and
        // translates no real SQL, so a test passing on it proves little. This asserts
        // the harness itself honours that.
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();

        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", context.Database.ProviderName);
        Assert.True(await context.Database.CanConnectAsync());
    }

    [Fact]
    public async Task The_api_does_not_create_the_schema_on_startup()
    {
        // Covers T-0001 AC5, which shipped with manual verification only and which
        // T-0001's own ticket called "the criterion most likely to be quietly
        // violated" — Database.Migrate() at startup is the convenient path.
        //
        // Every other test in this suite migrates first, so none of them would notice
        // if the API started migrating itself. This one deliberately does not migrate.
        var unmigrated = await postgres.CreateDatabaseAsync();
        using var factory = new ApiFactory(unmigrated);

        // Starting the host is what would trigger a startup migration, if one existed.
        using var client = factory.CreateClient();
        var response = await client.GetAsync(new Uri("/health", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);   // reachable, just empty

        await using var connection = new NpgsqlConnection(unmigrated);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'";

        Assert.Equal(0L, (long)(await command.ExecuteScalarAsync())!);
    }

    [Fact]
    public async Task Data_written_in_one_test_database_is_invisible_from_another()
    {
        // Isolation asserted through the property that matters — data does not leak
        // between databases. The previous version of this test compared two generated
        // names, which only proved Guid.NewGuid() works.
        var other = await postgres.CreateDatabaseAsync();
        using var otherFactory = new ApiFactory(other);
        await otherFactory.ApplyMigrationsAsync();

        using (var scope = _factory.Services.CreateScope())
        {
            var mine = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
            mine.Projects.Add(new ProjectRecord
            {
                Id = Guid.NewGuid(),
                Key = "ISOL",
                Name = "Isolation",
                CreatedAt = DateTimeOffset.UtcNow,
            });
            await mine.SaveChangesAsync();
            Assert.Equal(1, await mine.Projects.CountAsync());
        }

        using var otherScope = otherFactory.Services.CreateScope();
        var theirs = otherScope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();
        Assert.Equal(0, await theirs.Projects.CountAsync());
    }

    [Fact]
    public async Task Writes_are_visible_through_the_real_schema()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GotIssuesDbContext>();

        context.Projects.Add(new ProjectRecord
        {
            Id = Guid.NewGuid(),
            Key = "WRITE",
            Name = "Writes are visible",
            CreatedAt = DateTimeOffset.UtcNow,
        });
        await context.SaveChangesAsync();

        Assert.Equal(1, await context.Projects.CountAsync());
    }
}
