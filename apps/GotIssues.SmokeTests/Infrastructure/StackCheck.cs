using System.Net;

namespace GotIssues.SmokeTests.Infrastructure;

/// <summary>
/// The assertions that make up "the check", in one place so AC4 can falsify the very
/// code AC1–AC3 rely on. A mutation test that re-implements the assertions proves only
/// that the re-implementation fails.
/// </summary>
public static class StackCheck
{
    /// <summary>Services expected to run and stay healthy.</summary>
    public static readonly string[] LongRunningServices = ["postgres", "identity", "api"];

    /// <summary>Services expected to run once and exit zero (ADR-0003's explicit migration step).</summary>
    public static readonly string[] OneShotServices = ["migrator", "identity-migrator"];

    /// <summary>
    /// Every long-running service healthy, every one-shot service exited zero.
    ///
    /// Deliberately asserted from <c>compose ps</c> and not inferred from <c>up --wait</c>
    /// alone: <c>--wait</c> is the mechanism under test in AC4, and a check whose only
    /// evidence is the mechanism it is testing cannot fail when that mechanism is removed.
    /// </summary>
    public static async Task AssertStackHealthyAsync(ComposeProject stack)
    {
        var services = await stack.ServicesAsync().ConfigureAwait(false);

        foreach (var name in LongRunningServices)
        {
            var service = services.SingleOrDefault(s => s.Service == name);
            Assert.True(service is not null, $"Service '{name}' is absent from the stack. Present: {Names(services)}");
            Assert.True(service!.IsRunning, $"Service '{name}' is '{service.State}', expected running.");
            Assert.True(service.IsHealthy, $"Service '{name}' reports health '{service.Health}', expected healthy.");
        }

        foreach (var name in OneShotServices)
        {
            var service = services.SingleOrDefault(s => s.Service == name);
            Assert.True(service is not null, $"Service '{name}' is absent from the stack. Present: {Names(services)}");
            Assert.True(
                service!.ExitedCleanly,
                $"Service '{name}' is '{service.State}' with exit code {service.ExitCode}; expected exited 0.");
        }
    }

    /// <summary>
    /// The migration step's *effect*: the schema exists and the history records it.
    ///
    /// Added because AC4 caught the check without it. `/health` probes connectivity —
    /// correctly, that is its stated job (T-0001 AC3) — so it answers healthy against a
    /// database with no tables at all. A stack whose migration step had been neutered
    /// therefore passed the whole check. Service health cannot stand in for migrations
    /// having run; nothing but the schema can speak for the schema.
    /// </summary>
    public static async Task AssertSchemaMigratedAsync(ComposeProject stack)
    {
        // Qualified by schema on purpose: the identity host keeps a history table of the
        // same name in the `identity` schema, so an unqualified count is 2 on a healthy
        // stack and 1 when the API's migration step has done nothing at all. Counting
        // both would have made this assertion pass in exactly the case it exists to catch.
        var historyExists = await stack.QueryAsync(
            "select count(*) from information_schema.tables "
            + "where table_schema = 'public' and table_name = '__EFMigrationsHistory'");
        Assert.True(
            historyExists == "1",
            "The migrations history table does not exist, so the migration step never ran against this database.");

        var applied = await stack.QueryAsync("select count(*) from public.\"__EFMigrationsHistory\"");
        Assert.True(
            applied != "0",
            "The migrations history is empty: the migration step reported success without applying anything.");

        var users = await stack.QueryAsync(
            "select count(*) from information_schema.tables "
            + "where table_schema = 'public' and table_name = 'users'");
        Assert.True(
            users == "1",
            "The 'users' table is absent, so the schema the API depends on was never created.");
    }

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
