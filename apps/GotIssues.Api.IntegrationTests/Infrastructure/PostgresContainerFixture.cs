using Npgsql;
using Testcontainers.PostgreSql;

namespace GotIssues.Api.IntegrationTests.Infrastructure;

/// <summary>
/// One PostgreSQL container for the whole test run; a fresh database per test.
///
/// Refinement planned a database per test *class*, but xUnit constructs the test
/// class once per test method, so <c>IAsyncLifetime.InitializeAsync</c> runs per
/// method and each test gets its own database. That is stronger isolation than
/// planned at a measured cost of about a second across the suite, so it is kept
/// deliberately — and documented as what it is, rather than as what was planned.
///
/// A fresh container per test was never on the table: container startup dominates
/// the runtime, and a slow suite stops being run habitually (TESTING.md).
/// Transaction-rollback per test was rejected during refinement — integration tests
/// cross a real HTTP boundary and the application opens its own connections, so a
/// test-side rollback cannot wrap the app's writes.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    public string AdminConnectionString => _container.GetConnectionString();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();

    /// <summary>Creates an empty database and returns a connection string for it.</summary>
    public async Task<string> CreateDatabaseAsync()
    {
        var name = $"test_{Guid.NewGuid():N}";

        await using (var connection = new NpgsqlConnection(AdminConnectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            // The name is a generated GUID, never external input.
            command.CommandText = $"CREATE DATABASE \"{name}\"";
            await command.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        return new NpgsqlConnectionStringBuilder(AdminConnectionString) { Database = name }
            .ConnectionString;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresFixtureDefinition : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres";
}
