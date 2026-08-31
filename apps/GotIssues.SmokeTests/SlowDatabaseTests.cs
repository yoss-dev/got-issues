using GotIssues.SmokeTests.Infrastructure;

namespace GotIssues.SmokeTests;

/// <summary>AC3 — an absent database delays start-up rather than crashing the API.</summary>
[Collection(SerialExecution.Name)]
public sealed class SlowDatabaseTests
{
    [Fact]
    public async Task AC3_the_api_waits_for_an_absent_database_and_recovers_when_it_arrives()
    {
        await using var stack = new ComposeProject(ComposeProject.UniqueName("slowdb"));

        (await stack.BuildAsync()).EnsureSucceeded("docker compose build");

        // --no-deps is what makes this deterministic. "Slow database" is a race by
        // construction; starting the API with nothing to wait for turns it into a fact:
        // there is no database at all, and the API must still be running.
        (await stack.UpAsync(services: ["api"], wait: false, noDeps: true))
            .EnsureSucceeded("docker compose up api --no-deps");

        // Long enough that a process which exits on a failed connection has done so.
        await Task.Delay(TimeSpan.FromSeconds(20));

        var withoutDatabase = await stack.ServiceAsync("api");
        Assert.True(
            withoutDatabase.IsRunning,
            $"The API is '{withoutDatabase.State}' (exit {withoutDatabase.ExitCode}) with no database. "
            + "It must wait, not exit — a crash-looping API is what the explicit dependency conditions exist to prevent.");
        Assert.False(
            withoutDatabase.IsHealthy,
            "The API reports healthy with no database at all, so /health is not reporting the database's real state.");

        // The database arrives; the API must recover without being restarted.
        // No --wait here: it waits for a container to be running or healthy, and this
        // one exits by design, so --wait reports failure on the success case.
        (await stack.UpAsync(services: ["migrator"], wait: false))
            .EnsureSucceeded("docker compose up migrator (database arrives)");

        await StackCheck.WaitForCleanExitAsync(stack, "migrator");

        await StackCheck.WaitForHealthyAsync(stack, "api");

        var recovered = await stack.ServiceAsync("api");
        Assert.True(recovered.IsHealthy, $"The API never became healthy after the database arrived: '{recovered.Health}'.");
    }
}
