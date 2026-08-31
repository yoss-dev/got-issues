namespace GotIssues.IdentityHost;

/// <summary>
/// Source-generated log messages. The project builds with warnings as errors and
/// <c>latest-recommended</c> analysis (ENGINEERING.md), which rules out the
/// convenience <c>LogInformation</c> extensions (CA1848).
/// </summary>
internal static partial class IdentityHostLogging
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "Applying identity migrations and seeding development identities.")]
    public static partial void MigratingAndSeeding(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information,
        Message = "Identity migrations applied and seeding complete.")]
    public static partial void MigratedAndSeeded(ILogger logger);
}
