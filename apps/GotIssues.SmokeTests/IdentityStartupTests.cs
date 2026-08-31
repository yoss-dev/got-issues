using GotIssues.SmokeTests.Infrastructure;

namespace GotIssues.SmokeTests;

/// <summary>
/// AC7 — the identity host does not migrate or seed on ordinary startup.
///
/// The analogue of T-0001 AC5 for the identity host, and unguarded by any test until now.
/// ADR-0003 puts schema changes in a deliberate, observable step; a host that quietly
/// creates its own tables has moved that boundary without anyone deciding to.
/// </summary>
[Collection(SerialExecution.Name)]
public sealed class IdentityStartupTests
{
    [Fact]
    public async Task AC7_the_identity_host_creates_no_tables_when_started_without_its_migration_step()
    {
        await using var stack = new ComposeProject(ComposeProject.UniqueName("identity"));

        (await stack.BuildAsync()).EnsureSucceeded("docker compose build");
        (await stack.UpAsync(services: ["postgres"], wait: true))
            .EnsureSucceeded("docker compose up postgres");

        var before = await IdentityTableCountAsync(stack);
        Assert.Equal("0", before);

        // --no-deps skips identity-migrator, which is the entire point: this is the host
        // starting against a schema nobody has prepared.
        (await stack.UpAsync(services: ["identity"], wait: false, noDeps: true))
            .EnsureSucceeded("docker compose up identity --no-deps");

        // Long enough for a host that migrates on startup to have done it.
        await Task.Delay(TimeSpan.FromSeconds(25));

        // Without this the criterion is unevidenced: a container that crashed on start
        // also creates no tables, and would satisfy the count below while proving
        // nothing about whether the host migrates. Health is deliberately not required —
        // this host is running against a schema nobody prepared, so it is expected to be
        // unhealthy. Running is the claim; healthy would be the wrong one.
        var identity = await stack.ServiceAsync("identity");
        Assert.True(
            identity.IsRunning,
            $"The identity host is '{identity.State}' (exit {identity.ExitCode}). It must be running for "
            + "'it created no tables' to mean it declined to migrate rather than that it never started.");

        // Stronger than the log assertion this replaces, and not brittle. In the identity
        // host's Program.cs the `--migrate` branch returns *before* the host maps health
        // checks or serves anything — so any HTTP response at all proves execution passed
        // the point where a migrate-on-startup host would have migrated. The status is
        // deliberately not asserted: unhealthy is the correct answer against a schema
        // nobody prepared.
        //
        // The assertion here previously required the container's logs to contain
        // "identity", which `docker compose logs` prefixes onto every line from the
        // service name the test itself chose. It measured the presence of its own
        // argument while its message claimed evidence of startup.
        await StackCheck.WaitForAnyResponseAsync(stack, "identity", "/health");

        Assert.Equal("0", await IdentityTableCountAsync(stack));
    }

    /// <summary>
    /// Counts tables in every schema but the system ones, so a host that migrated into
    /// `public` instead of `identity` is caught too — asserting only on the schema we
    /// expect would let the interesting failure through.
    /// </summary>
    private static Task<string> IdentityTableCountAsync(ComposeProject stack) =>
        stack.QueryAsync(
            "select count(*) from information_schema.tables "
            + "where table_schema not in ('pg_catalog', 'information_schema')");
}
