using System.Collections.Concurrent;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GotIssues.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Captures everything the application logs during a test, so assertions about what
/// is <em>not</em> logged can be made against real output rather than by reading the
/// code and trusting it.
/// </summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentQueue<string> _lines = new();

    public string Text => string.Join('\n', _lines);

    public ILogger CreateLogger(string categoryName) => new Capturing(_lines);

    public void Dispose() => GC.SuppressFinalize(this);

    private sealed class Capturing(ConcurrentQueue<string> lines) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            ArgumentNullException.ThrowIfNull(formatter);

            var builder = new StringBuilder(formatter(state, exception));
            if (exception is not null)
            {
                builder.Append(' ').Append(exception);
            }

            lines.Enqueue(builder.ToString());
        }
    }
}

public static class LogCaptureExtensions
{
    /// <summary>
    /// A copy of the factory whose application logs are captured. The provider is
    /// added rather than replacing the others, so nothing about normal logging
    /// behaviour changes for the code under test.
    /// </summary>
    public static WebApplicationFactory<Program> WithLogCapture(
        this WebApplicationFactory<Program> factory, CapturingLoggerProvider provider)
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                services.AddSingleton<ILoggerProvider>(provider);

                // The logging *filter* runs before any provider, so a capturing
                // provider returning IsEnabled => true still sees nothing below the
                // configured level. SetMinimumLevel is NOT enough: appsettings.json
                // binds Logging:LogLevel:Default as a filter *rule*, and rule
                // matching takes precedence over MinLevel — verified, the same leak
                // still passed at Debug with SetMinimumLevel in place.
                //
                // An always-true filter is what actually opens the guard, and Debug
                // is exactly where a leak would appear: it is the level someone
                // reaches for while debugging a projection with a real name in front
                // of them.
                services.AddLogging(logging => logging.AddFilter((_, _) => true));
            }));
    }
}
