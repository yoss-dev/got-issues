using System.Diagnostics;
using System.Text;

namespace GotIssues.SmokeTests.Infrastructure;

/// <summary>The result of a command: its own exit code, never a pipeline's.</summary>
public sealed record CommandResult(int ExitCode, string StandardOutput, string StandardError)
{
    public string Combined => StandardOutput + StandardError;

    public bool Succeeded => ExitCode == 0;

    /// <summary>
    /// Fails the test with the command's own output attached. A stack check that reports
    /// "docker compose failed" without saying how is a check nobody can act on.
    /// </summary>
    public CommandResult EnsureSucceeded(string what)
    {
        Assert.True(
            Succeeded,
            $"{what} exited {ExitCode}.\n--- stdout ---\n{StandardOutput}\n--- stderr ---\n{StandardError}");
        return this;
    }
}

/// <summary>
/// Runs a process and reads the exit status of that process.
///
/// TESTING.md: "read the exit status of the tool you are checking, not of a pipeline it
/// feeds." This type exists so no smoke test can accidentally read a shell's status
/// instead of docker's — there is no shell here to get it wrong.
/// </summary>
public static class ProcessRunner
{
    public static async Task<CommandResult> RunAsync(
        string fileName,
        IEnumerable<string> arguments,
        IDictionary<string, string>? environment = null,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory ?? RepositoryRoot.Path,
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        using var process = new Process { StartInfo = startInfo };
        var stdout = new StringBuilder();
        var stderr = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { lock (stdout) { stdout.AppendLine(e.Data); } } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { lock (stderr) { stderr.AppendLine(e.Data); } } };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

        string outText, errText;
        lock (stdout) { outText = stdout.ToString(); }
        lock (stderr) { errText = stderr.ToString(); }

        return new CommandResult(process.ExitCode, outText, errText);
    }
}

/// <summary>
/// Locates the repository root by walking up for <c>compose.yaml</c>.
///
/// The check must drive the real compose file: a smoke test against a copied compose
/// file verifies the copy. Everything here resolves from this one path.
/// </summary>
public static class RepositoryRoot
{
    public static string Path { get; } = Locate();

    public static string ComposeFile => System.IO.Path.Combine(Path, "compose.yaml");

    private static string Locate()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(System.IO.Path.Combine(directory.FullName, "compose.yaml")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException(
            $"No compose.yaml found above {AppContext.BaseDirectory}. The smoke tests must run inside the repository.");
    }
}
