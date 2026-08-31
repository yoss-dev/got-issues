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
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder("postgres:18-alpine")
            // The suite creates a database per test and **never releases the
            // connection**, so usage grows linearly and is never reclaimed:
            // `claude-qa-8f52` sampled `pg_stat_activity` 100 times during a run and
            // the count never decreased once, ending at 104 connections — 103 of them
            // idle — across 92 databases. Past PostgreSQL's default ceiling of 100 it
            // answers `53300: sorry, too many clients already`, in whichever test
            // happens to run at the limit.
            //
            // **This raises the ceiling; it does not fix the leak.** At ~1.09
            // connections per database the same failure returns at roughly 455 tests.
            // T-0023 owns the leak, and its AC2 requires the suite to pass at
            // `max_connections=100` so that raising a ceiling again cannot satisfy it.
            .WithCommand("-c", "max_connections=500")
            .Build();

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

        return new NpgsqlConnectionStringBuilder(AdminConnectionString)
        {
            Database = name,

            // A belt-and-braces bound, and an honest note about what it is worth:
            // **measurement says this binds nothing today.** Actual usage is 1.09
            // connections per database against this cap of 10, because the growth is
            // one leaked connection per database rather than a pool filling up.
            //
            // The original comment here claimed pools multiply because "xUnit runs
            // classes in parallel". That is false — all nine integration classes share
            // `[Collection(PostgresFixtureDefinition.Name)]`, so xUnit runs them
            // sequentially, and parallel growth is impossible. Corrected rather than
            // deleted: a plausible wrong mechanism sitting beside a fix is worse than
            // no comment, because the next person debugging this would start from it.
            //
            // Kept because it costs nothing and bounds the case the leak fix (T-0023)
            // might not cover: a single test that genuinely opens many connections.
            MaxPoolSize = 10,
        }.ConnectionString;
    }
}

[CollectionDefinition(Name)]
public sealed class PostgresFixtureDefinition : ICollectionFixture<PostgresContainerFixture>
{
    public const string Name = "postgres";
}
