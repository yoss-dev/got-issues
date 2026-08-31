using System.Globalization;
using GotIssues.SmokeTests.Infrastructure;

namespace GotIssues.SmokeTests;

/// <summary>AC2 — a restart against an existing volume is non-destructive.</summary>
[Collection(SerialExecution.Name)]
public sealed class RestartTests
{
    [Fact]
    public async Task AC2_a_restart_keeps_the_data_and_applies_no_migrations()
    {
        await using var stack = new ComposeProject(ComposeProject.UniqueName("restart"));

        (await stack.BuildAsync()).EnsureSucceeded("docker compose build");
        (await stack.UpAsync()).EnsureSucceeded("docker compose up --wait (first start)");

        // Raw SQL through psql in the container, not EF: postgres publishes no host port
        // (deliberately — only api and identity do), and a black-box check of the stack
        // should not link against the application's own data layer to inspect it.
        //
        // A row written by hand rather than through the API: client-credentials tokens
        // carry no subject, so no request this stack can make creates a user projection
        // (the gap T-0009 recorded and AC8 below inherits).
        var subject = $"smoke-{Guid.NewGuid():N}";
        await stack.QueryAsync(
            $"insert into users (\"Subject\", \"DisplayName\", \"FirstSeenAt\", \"LastSeenAt\") "
            + $"values ('{subject}', 'Smoke Restart', now(), now())");

        var migrationsBefore = await MigrationCountAsync(stack);
        Assert.True(migrationsBefore > 0, "No migrations were recorded before the restart; the first start did not migrate.");

        // Volumes deliberately kept — that is the whole subject of this criterion.
        (await stack.DownAsync(removeVolumes: false))
            .EnsureSucceeded("docker compose down (volumes kept)");

        (await stack.UpAsync()).EnsureSucceeded("docker compose up --wait (restart)");
        await StackCheck.AssertStackHealthyAsync(stack);

        var survivors = await stack.QueryAsync(
            $"select count(*) from users where \"Subject\" = '{subject}'");
        Assert.Equal("1", survivors);

        // "Applied nothing" asserted from the migrations history rather than from log
        // text: the migrator logs that it is applying migrations whether or not any are
        // outstanding, so its output cannot distinguish the two.
        Assert.Equal(migrationsBefore, await MigrationCountAsync(stack));

        var migrator = await stack.ServiceAsync("migrator");
        Assert.True(
            migrator.ExitedCleanly,
            $"The migration step is '{migrator.State}' with exit code {migrator.ExitCode} after a restart; expected exited 0.");
    }

    private static async Task<int> MigrationCountAsync(ComposeProject stack)
    {
        var value = await stack.QueryAsync("select count(*) from \"__EFMigrationsHistory\"");
        return int.Parse(value, CultureInfo.InvariantCulture);
    }
}
