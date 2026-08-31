using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GotIssues.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Authentication for tests that are not themselves *about* authentication.
/// Authenticates only when the caller sends <see cref="HeaderName"/>; absent it, the
/// request is anonymous and a guarded endpoint must refuse it.
///
/// This type lives in the test assembly and is injected through an
/// <see cref="IStartupFilter"/>, so it cannot be reached from the API's own
/// composition — there is no configuration switch that turns it on in a real run
/// (T-0003 AC10). SECURITY.md forbids disabling authentication to make tests pass;
/// this adds a scheme for tests rather than removing enforcement.
/// </summary>
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "IntegrationTest";
    public const string HeaderName = "X-Test-Subject";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var subject) || subject.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, subject.ToString())], SchemeName);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}

/// <summary>
/// Adds authentication/authorization middleware and one guarded endpoint to the test
/// host only. The API has no auth pipeline yet — that is T-0010 — so without this
/// there would be nothing for AC5's refusal test to exercise.
/// </summary>
public sealed class GuardedEndpointStartupFilter : IStartupFilter
{
    public const string Route = "/test-only/guarded";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
                endpoints.MapGet(Route, () => Results.Ok("reached"))
                    .RequireAuthorization());
            next(app);
        };
}
