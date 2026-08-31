using System.Net;

namespace GotIssues.SmokeTests.Infrastructure;

/// <summary>
/// The assertions that make up "the check", in one place so AC4 can falsify the very
/// code AC1–AC3 rely on. A mutation test that re-implements the assertions proves only
/// that the re-implementation fails.
/// </summary>
public static class StackCheck
{
    /// <summary>
    /// Every declared service is either running and healthy, or exited zero.
    ///
    /// The services are read from the compose file rather than listed here. A hard-coded
    /// list turns "every service is healthy" into "every service someone remembered", and
    /// the service added next is exactly the one it would not cover.
    ///
    /// Asserted from <c>compose ps</c> and not inferred from <c>up --wait</c>: `--wait` is
    /// the mechanism AC4 breaks, and a check whose only evidence is the mechanism under
    /// test cannot fail when that mechanism is removed.
    /// </summary>
    public static async Task AssertStackHealthyAsync(ComposeProject stack)
    {
        var declared = await stack.DeclaredServicesAsync();
        Assert.NotEmpty(declared);

        var services = await stack.ServicesAsync();

        foreach (var name in declared)
        {
            var service = services.SingleOrDefault(s => s.Service == name);
            Assert.True(service is not null, $"Service '{name}' is declared but absent from the stack. Present: {Names(services)}");

            Assert.True(
                (service!.IsRunning && service.IsHealthy) || service.ExitedCleanly,
                $"Service '{name}' is '{service.State}' with health '{service.Health}' and exit code "
                + $"{service.ExitCode}. Every service must either be running and healthy, or have exited 0.");
        }
    }

    /// <summary>
    /// The schema matches what a clean run of the migration step produces.
    ///
    /// Rewritten after acceptance. The previous version asserted that a *fixed pair* of
    /// tables existed and that the history was non-empty, which passed against a database
    /// missing a table entirely (it was `placeholder_records` at the time), and against a partially-migrated one where
    /// a column was the wrong width — both verified by the acceptor. A named list of
    /// tables can only find what its author already thought of.
    ///
    /// This migrates a scratch database with the stack's own migration step and compares
    /// full column signatures: every table, column, type and length. A missing table, a
    /// missing column and a rolled-back width change all differ. The reference must be
    /// non-empty, or a migration step that does nothing would agree with a database where
    /// nothing was done.
    ///
    /// <para><b>What this compares, precisely.</b> The live database against <i>the
    /// migration step as it exists in this stack</i> — not against the repository's
    /// intent. So it catches the database being behind the step, and not the step being
    /// behind the repository: a step that omits a table produces a reference missing that
    /// table too, the two agree, and the check passes. The step is its own oracle here,
    /// which no live-versus-reference comparison can see by construction. The integration
    /// tier is what defends that; this one cannot. Demonstrated by `claude-qa-9b3e` with a
    /// reduced migrator, 2026-08-31.</para>
    ///
    /// <para><b>Known limits.</b> The signature is <c>information_schema.columns</c> only:
    /// no indexes, constraints, defaults or nullability, so a migration adding only an
    /// index produces an identical signature. Nor does it read <c>numeric_precision</c>,
    /// <c>numeric_scale</c> or <c>datetime_precision</c> — lengths are covered, precision
    /// is not, so <c>timestamp(0)</c> against <c>timestamp</c> passes.</para>
    /// </summary>
    public static async Task AssertSchemaMigratedAsync(ComposeProject stack)
    {
        const string reference = "smoke_schema_reference";
        const string signature =
            "select table_name || '.' || column_name || ' ' || data_type || "
            + "coalesce('(' || character_maximum_length || ')', '') "
            + "from information_schema.columns where table_schema = 'public' order by 1";

        await stack.QueryAsync($"drop database if exists {reference}", "postgres");
        await stack.QueryAsync($"create database {reference}", "postgres");

        try
        {
            var connection =
                $"Host=postgres;Port=5432;Database={reference};"
                + $"Username={ComposeProject.PostgresUser};Password={ComposeProject.PostgresPassword}";

            (await stack.ComposeAsync(
                "run", "--rm", "--no-deps", "-e", $"ConnectionStrings__GotIssues={connection}", "migrator"))
                .EnsureSucceeded("migrating the reference database");

            var expected = Lines(await stack.QueryAsync(signature, reference));
            Assert.True(
                expected.Count > 0,
                "A clean run of the migration step produced no schema at all, so it applies nothing — "
                + "and an empty reference would agree with any unmigrated database.");

            var actual = Lines(await stack.QueryAsync(signature));

            var missing = expected.Except(actual, StringComparer.Ordinal).ToList();
            var unexpected = actual.Except(expected, StringComparer.Ordinal).ToList();

            Assert.True(
                missing.Count == 0 && unexpected.Count == 0,
                "The live schema differs from what a clean migration produces.\n"
                + $"Missing: {Describe(missing)}\nUnexpected: {Describe(unexpected)}");
        }
        finally
        {
            await stack.QueryAsync($"drop database if exists {reference}", "postgres");
        }
    }

    private static List<string> Lines(string output) =>
        [.. output.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(l => l.Trim()).Where(l => l.Length > 0)];

    private static string Describe(List<string> items) =>
        items.Count == 0 ? "(none)" : string.Join(", ", items);

    /// <summary>
    /// Confirms the endpoint answering us belongs to the container under test.
    ///
    /// TESTING.md's attribution rule, and the reason for it: a `curl` to localhost can be
    /// answered by a different stack entirely while the one being tested has failed to
    /// start. This project made that mistake twice. Stopping the container and observing
    /// the endpoint stop answering is what turns a 200 into evidence.
    /// </summary>
    public static async Task AssertHealthAnswersFromThisStackAsync(ComposeProject stack, string service = "api")
    {
        var address = await stack.BaseAddressAsync(service).ConfigureAwait(false);
        using var client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(10) };

        var before = await client.GetAsync(new Uri("/health", UriKind.Relative)).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        (await stack.ComposeAsync("stop", service).ConfigureAwait(false))
            .EnsureSucceeded($"docker compose stop {service}");

        try
        {
            var answered = await AnswersAsync(client).ConfigureAwait(false);
            Assert.False(
                answered,
                $"/health still answered after '{service}' was stopped — the response came from something else, "
                + "so nothing this check observed can be attributed to the stack under test.");
        }
        finally
        {
            (await stack.ComposeAsync("start", service).ConfigureAwait(false))
                .EnsureSucceeded($"docker compose start {service}");
            await WaitForHealthyAsync(stack, service).ConfigureAwait(false);
        }
    }

    private static async Task<bool> AnswersAsync(HttpClient client)
    {
        try
        {
            using var response = await client.GetAsync(new Uri("/health", UriKind.Relative)).ConfigureAwait(false);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException)
        {
            return false;
        }
    }

    /// <summary>
    /// Waits for a service to answer HTTP at all, whatever it answers.
    ///
    /// Deliberately status-agnostic: a host running against a schema nobody prepared is
    /// *expected* to report unhealthy, so requiring 200 would assert the wrong thing.
    /// The claim is only that the process reached the point of serving requests.
    /// </summary>
    public static async Task<HttpStatusCode> WaitForAnyResponseAsync(
        ComposeProject stack, string service, string path, TimeSpan? timeout = null)
    {
        var address = await stack.BaseAddressAsync(service);
        using var client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(10) };

        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromMinutes(2));
        Exception? last = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await client.GetAsync(new Uri(path, UriKind.Relative));
                return response.StatusCode;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                last = ex;
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        Assert.Fail($"Service '{service}' never answered {path}: {last?.Message}");
        return default;
    }

    /// <summary>Polls Compose's own health state — never a sleep long enough to look right.</summary>
    public static async Task WaitForHealthyAsync(
        ComposeProject stack, string service, TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromMinutes(2));
        ServiceState? last = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            last = await stack.ServiceAsync(service).ConfigureAwait(false);

            if (last.IsRunning && last.IsHealthy)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(2)).ConfigureAwait(false);
        }

        Assert.Fail($"Service '{service}' never became healthy: state '{last?.State}', health '{last?.Health}'.");
    }

    /// <summary>Waits for a one-shot service to finish, rather than for it to be running.</summary>
    public static async Task WaitForCleanExitAsync(
        ComposeProject stack, string service, TimeSpan? timeout = null)
    {
        var deadline = DateTimeOffset.UtcNow + (timeout ?? TimeSpan.FromMinutes(2));
        ServiceState? last = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            last = await stack.ServiceAsync(service);

            if (last.ExitedCleanly)
            {
                return;
            }

            Assert.True(
                last.ExitCode == 0,
                $"Service '{service}' exited {last.ExitCode}.");

            await Task.Delay(TimeSpan.FromSeconds(2));
        }

        Assert.Fail($"Service '{service}' never exited cleanly: state '{last?.State}', exit code {last?.ExitCode}.");
    }

    private static string Names(IReadOnlyList<ServiceState> services) =>
        services.Count == 0 ? "(none)" : string.Join(", ", services.Select(s => s.Service));
}
