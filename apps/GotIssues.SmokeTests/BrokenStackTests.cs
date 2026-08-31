using GotIssues.SmokeTests.Infrastructure;
using Xunit.Sdk;

namespace GotIssues.SmokeTests;

/// <summary>
/// AC4 — the criterion that makes the other seven worth anything.
///
/// A stack check that has only ever been seen green proves nothing: it might assert
/// nothing at all. These tests break the stack deliberately and require the same check
/// the other tests rely on to fail. The mutations arrive as Compose override files, so
/// the base is still the real <c>compose.yaml</c> — mutating a copy would prove only
/// that the copy was broken.
/// </summary>
[Collection(SerialExecution.Name)]
public sealed class BrokenStackTests
{
    /// <summary>
    /// The migration step neutered: it exits 0 without applying anything, so the API
    /// starts against a schema that was never created. This is ADR-0003's explicit
    /// migration step removed in the only way an override can remove it.
    /// </summary>
    private const string MigrationStepRemoved =
        """
        services:
          migrator:
            entrypoint: ["/bin/sh", "-c", "exit 0"]
        """;

    /// <summary>The API's health condition dropped — AC4's other named example.</summary>
    private const string HealthConditionDropped =
        """
        services:
          api:
            healthcheck:
              disable: true
        """;

    [Fact]
    public async Task AC4_the_check_fails_when_the_migration_step_is_removed()
    {
        var failure = await RunCheckAgainstAsync(MigrationStepRemoved, "migration-step-removed");

        Assert.True(
            failure is not null,
            "The check passed against a stack whose migration step does nothing. It would not notice a "
            + "migration regression, so every other criterion it reports is unevidenced.");

        // Asserting only that *something* failed would let an unrelated fault — a failed
        // image build, a stack torn down underneath — read as the mutation being caught.
        // The failure has to be the one this mutation causes.
        Assert.Contains("produced no schema at all", failure!.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AC4_the_check_fails_when_the_api_health_condition_is_dropped()
    {
        // This one specifically proves the `compose ps` assertions carry weight rather
        // than `up --wait` doing all the work: with no healthcheck, `--wait` has nothing
        // to wait for and returns zero. Only the explicit state assertion catches it.
        var failure = await RunCheckAgainstAsync(HealthConditionDropped, "health-condition-dropped");

        Assert.True(
            failure is not null,
            "The check passed against a stack whose API declares no health condition, so 'every service "
            + "reaches a healthy state' was never actually being asserted.");

        // Not "health": docker's own output says "container … is unhealthy" when an
        // unrelated dependency fails to start, so that substring reported the mutant
        // killed while no assertion in the check had run. The marker has to be text only
        // this assertion produces.
        Assert.Contains(
            "Every service must either be running and healthy", failure!.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Runs exactly what AC1 runs — <c>up --wait</c> followed by the shared assertions —
    /// and reports the failure rather than throwing it. Re-implementing the check here
    /// would prove only that the re-implementation fails.
    /// </summary>
    private static async Task<Exception?> RunCheckAgainstAsync(string overrideYaml, string label)
    {
        var overrideFile = Path.Combine(Path.GetTempPath(), $"gotissues-smoke-{label}-{Guid.NewGuid():N}.yaml");
        await File.WriteAllTextAsync(overrideFile, overrideYaml);

        try
        {
            // The override file must outlive the stack: tearing down needs every compose
            // file the stack was created with, and deleting it first leaves `down`
            // failing against a path that no longer exists — which leaked both mutation
            // stacks and their volumes on every run, silently, because disposal is the
            // one place a command result was discarded.
            await using var stack = new ComposeProject(ComposeProject.UniqueName(label), overrideFile);

            // Building is setup, not part of the check. Inside the catch below, a build
            // failure would have *satisfied* these tests — they assert only that
            // something went wrong, so anything going wrong would have proved the point.
            (await stack.BuildAsync()).EnsureSucceeded("docker compose build");

            // `up --wait` is setup too. Both mutations produce a stack that starts
            // cleanly — the breakage is what the assertions find afterwards — so a
            // failure to start is a harness fault and must fail this test rather than
            // be counted as the mutant being caught.
            (await stack.UpAsync()).EnsureSucceeded("docker compose up --wait");

            try
            {
                await StackCheck.AssertStackHealthyAsync(stack);
                await StackCheck.AssertSchemaMigratedAsync(stack);
                return null;
            }
            catch (XunitException failure)
            {
                // Only an assertion counts as "the check failed". A Docker CLI that is
                // missing, or any other harness fault, must fail this test rather than
                // masquerade as evidence that the check works.
                return failure;
            }
        }
        finally
        {
            File.Delete(overrideFile);
        }
    }
}
