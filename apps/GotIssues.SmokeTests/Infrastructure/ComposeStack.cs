using System.Globalization;
using System.Text.Json;

namespace GotIssues.SmokeTests.Infrastructure;

/// <summary>
/// Drives the repository's real <c>compose.yaml</c> under a project name of its own.
///
/// Two rules from TESTING.md shape this type, both of them paid for:
///
/// 1. <b>Its own project name.</b> A developer's stack running from the same file must
///    not be touched, and must not answer for it.
/// 2. <b>Its own ports.</b> A distinct project name does not prevent a host port
///    collision — <c>compose.yaml</c> publishes 8080 and 8081 by default, so two stacks
///    fight over them and the loser's endpoint is answered by the winner. This project
///    has produced that exact false pass twice. Ports are therefore published
///    ephemerally (<c>0</c>) and discovered with <c>docker compose port</c>; no test
///    ever hard-codes one.
///
/// Environment comes from an explicit file this class writes, not from the developer's
/// <c>.env</c>: the check must behave identically on a machine that has never run the
/// stack by hand.
/// </summary>
public sealed class ComposeProject : IAsyncDisposable
{
    private readonly string _envFile;
    private readonly List<string> _composeFiles;

    /// <param name="overrideFiles">
    /// Compose override files layered on top of the real <c>compose.yaml</c>. AC4's
    /// mutations arrive this way rather than by editing a copy of the file: the base
    /// stays the file the project actually ships, so a mutation test still exercises
    /// the real stack definition with one thing deliberately broken.
    /// </param>
    public ComposeProject(string projectName, params string[] overrideFiles)
    {
        ProjectName = projectName;
        _composeFiles = [RepositoryRoot.ComposeFile, .. overrideFiles];

        _envFile = Path.Combine(Path.GetTempPath(), $"{projectName}-{Guid.NewGuid():N}.env");
        File.WriteAllText(_envFile, EnvironmentFileContents);
    }

    public string ProjectName { get; }

    /// <summary>
    /// A project name that is actually unique.
    ///
    /// Truncating a name to a fixed width silently removed the GUID whenever the label
    /// was long enough — so two runs shared one project, its containers and its volumes,
    /// which is the very collision the per-run project name exists to prevent.
    ///
    /// Nothing is truncated, and no cap is applied at all: a cap chosen to be "big
    /// enough" is the same bug with a larger number, and it fails the same silent way.
    /// The prefix is short instead.
    /// </summary>
    public static string UniqueName(string label) => $"gs-{label}-{Guid.NewGuid():N}";

    /// <summary>
    /// Fixed credentials for a throwaway stack. Not secrets: this database exists for
    /// the duration of one test run and is destroyed with its volume.
    /// </summary>
    public const string PostgresUser = "smoke";
    public const string PostgresPassword = "not-a-secret-throwaway-stack";
    public const string PostgresDatabase = "smoke";
    public const string AdminClientId = "smoke-admin-client";
    public const string AdminClientSecret = "not-a-secret-throwaway-stack";
    public const string MemberClientId = "smoke-member-client";
    public const string MemberClientSecret = "not-a-secret-throwaway-stack";

    /// <summary>
    /// The issuer the identity host stamps into tokens and the API validates. It is a
    /// string both sides agree on, not an address anything dials: metadata is fetched
    /// inside the Compose network and the host reaches services on ephemeral ports.
    /// </summary>
    public const string IssuerUri = "http://localhost:8081";

    public const string ApiAudience = "gotissues-api";

    private static string EnvironmentFileContents =>
        $"""
         POSTGRES_USER={PostgresUser}
         POSTGRES_PASSWORD={PostgresPassword}
         POSTGRES_DB={PostgresDatabase}
         API_PORT=0
         IDENTITY_PORT=0
         IDENTITY_ISSUER_URI={IssuerUri}
         ADMIN_CLIENT_ID={AdminClientId}
         ADMIN_CLIENT_SECRET={AdminClientSecret}
         MEMBER_CLIENT_ID={MemberClientId}
         MEMBER_CLIENT_SECRET={MemberClientSecret}
         """;

    private IEnumerable<string> BaseArguments
    {
        get
        {
            yield return "compose";
            yield return "--project-name";
            yield return ProjectName;
            yield return "--env-file";
            yield return _envFile;

            foreach (var file in _composeFiles)
            {
                yield return "--file";
                yield return file;
            }
        }
    }

    public Task<CommandResult> ComposeAsync(params string[] arguments) =>
        ComposeAsync(arguments, CancellationToken.None);

    public async Task<CommandResult> ComposeAsync(
        IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var all = BaseArguments.Concat(arguments).ToList();
        return await ProcessRunner.RunAsync("docker", all, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Builds images once so start-up timings are not dominated by the build.</summary>
    public async Task<CommandResult> BuildAsync(CancellationToken cancellationToken = default) =>
        await ComposeAsync(["build"], cancellationToken).ConfigureAwait(false);

    /// <summary>
    /// Starts services and waits for their declared health conditions. <c>--wait</c> is
    /// what makes AC1 an assertion rather than a sleep: Compose returns non-zero if a
    /// service never reaches healthy.
    /// </summary>
    public async Task<CommandResult> UpAsync(
        string[]? services = null,
        bool wait = true,
        bool noDeps = false,
        CancellationToken cancellationToken = default)
    {
        var arguments = new List<string> { "up", "--detach" };

        if (wait)
        {
            arguments.Add("--wait");
        }

        if (noDeps)
        {
            arguments.Add("--no-deps");
        }

        if (services is not null)
        {
            arguments.AddRange(services);
        }

        return await ComposeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Stops the stack. <paramref name="removeVolumes"/> false is what makes AC2 possible.</summary>
    public async Task<CommandResult> DownAsync(
        bool removeVolumes = true, CancellationToken cancellationToken = default)
    {
        var arguments = new List<string> { "down", "--remove-orphans" };

        if (removeVolumes)
        {
            arguments.Add("--volumes");
        }

        return await ComposeAsync(arguments, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The host port Docker actually assigned. Never assume 8080: this stack publishes
    /// ephemerally precisely so it cannot be answered by somebody else's container.
    /// </summary>
    public async Task<int> HostPortAsync(string service, int containerPort)
    {
        var result = await ComposeAsync("port", service, containerPort.ToString(CultureInfo.InvariantCulture))
            .ConfigureAwait(false);
        result.EnsureSucceeded($"docker compose port {service} {containerPort}");

        var mapping = result.StandardOutput.Trim();
        var separator = mapping.LastIndexOf(':');

        Assert.True(
            separator > 0 && int.TryParse(mapping[(separator + 1)..], CultureInfo.InvariantCulture, out _),
            $"Could not read a host port for {service} from '{mapping}'.");

        return int.Parse(mapping[(separator + 1)..], CultureInfo.InvariantCulture);
    }

    public async Task<Uri> BaseAddressAsync(string service, int containerPort = 8080) =>
        new($"http://localhost:{await HostPortAsync(service, containerPort).ConfigureAwait(false)}");

    /// <summary>
    /// The state Compose reports for each service, from <c>ps --format json</c> rather
    /// than parsed table text.
    /// </summary>
    public async Task<IReadOnlyList<ServiceState>> ServicesAsync(CancellationToken cancellationToken = default)
    {
        var result = await ComposeAsync(["ps", "--all", "--format", "json"], cancellationToken)
            .ConfigureAwait(false);
        result.EnsureSucceeded("docker compose ps");

        var states = new List<ServiceState>();

        // One JSON object per line, not a JSON array.
        foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] != '{')
            {
                continue;
            }

            using var document = JsonDocument.Parse(trimmed);
            var root = document.RootElement;

            states.Add(new ServiceState(
                Service: root.GetProperty("Service").GetString() ?? string.Empty,
                State: root.GetProperty("State").GetString() ?? string.Empty,
                Health: root.TryGetProperty("Health", out var health) ? health.GetString() ?? string.Empty : string.Empty,
                ExitCode: root.TryGetProperty("ExitCode", out var exit) ? exit.GetInt32() : 0));
        }

        return states;
    }

    public async Task<ServiceState> ServiceAsync(string service, CancellationToken cancellationToken = default)
    {
        var services = await ServicesAsync(cancellationToken).ConfigureAwait(false);
        var match = services.SingleOrDefault(s => s.Service == service);

        Assert.True(
            match is not null,
            $"No service '{service}' in project '{ProjectName}'. Present: {string.Join(", ", services.Select(s => s.Service))}");

        return match!;
    }

    /// <summary>Runs a command inside a running container — used to query Postgres, which publishes no port.</summary>
    public async Task<CommandResult> ExecAsync(string service, params string[] command)
    {
        var arguments = new List<string> { "exec", "--no-TTY", service };
        arguments.AddRange(command);
        return await ComposeAsync(arguments, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>A single-value SQL query against the stack's own database.</summary>
    public async Task<string> QueryAsync(string sql)
    {
        var result = await ExecAsync(
            "postgres", "psql", "-U", PostgresUser, "-d", PostgresDatabase, "-tAc", sql).ConfigureAwait(false);
        result.EnsureSucceeded($"psql: {sql}");
        return result.StandardOutput.Trim();
    }

    public async ValueTask DisposeAsync()
    {
        // Read, not discarded. A teardown that fails leaves containers and volumes behind
        // on a machine where every future run then competes with them, and it was
        // invisible precisely because this was the one result nobody checked.
        (await DownAsync().ConfigureAwait(false)).EnsureSucceeded($"docker compose down ({ProjectName})");

        if (File.Exists(_envFile))
        {
            File.Delete(_envFile);
        }
    }
}

public sealed record ServiceState(string Service, string State, string Health, int ExitCode)
{
    public bool IsRunning => string.Equals(State, "running", StringComparison.OrdinalIgnoreCase);

    public bool IsHealthy => string.Equals(Health, "healthy", StringComparison.OrdinalIgnoreCase);

    public bool ExitedCleanly =>
        string.Equals(State, "exited", StringComparison.OrdinalIgnoreCase) && ExitCode == 0;
}
