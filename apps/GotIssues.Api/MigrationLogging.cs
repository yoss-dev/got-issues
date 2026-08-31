namespace GotIssues.Api;

/// <summary>
/// Source-generated log messages. The project builds with warnings as errors and
/// <c>latest-recommended</c> analysis (ENGINEERING.md), which rules out the
/// convenience <c>LogInformation</c> extensions (CA1848).
/// </summary>
internal static partial class MigrationLogging
{
    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Applying database migrations.")]
    public static partial void ApplyingMigrations(ILogger logger);

    [LoggerMessage(EventId = 2, Level = LogLevel.Information, Message = "Migrations applied.")]
    public static partial void MigrationsApplied(ILogger logger);
}
