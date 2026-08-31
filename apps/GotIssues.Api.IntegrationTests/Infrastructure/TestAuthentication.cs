using System.Security.Claims;
using System.Text.Encodings.Web;
using GotIssues.Api.Authentication;
using GotIssues.Api.Authorization;
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

    /// <summary>Sets the caller's <c>role</c> claim. Omit it to test a token with none.</summary>
    public const string RoleHeaderName = "X-Test-Role";

    /// <summary>Sets the caller's display name. Omit it to test a token without one.</summary>
    public const string NameHeaderName = "X-Test-Name";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(HeaderName, out var subject) || subject.Count == 0)
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, subject.ToString()) };

        // Deliberately verbatim: the handler does not normalise, default or validate
        // the role. A test asking for `role: superuser` must produce exactly that, so
        // the API's own allow-list is what gets exercised rather than the test host's
        // idea of a sensible value.
        if (Request.Headers.TryGetValue(RoleHeaderName, out var role) && role.Count > 0)
        {
            claims.Add(new Claim("role", role.ToString()));
        }

        if (Request.Headers.TryGetValue(NameHeaderName, out var name) && name.Count > 0)
        {
            claims.Add(new Claim("name", name.ToString()));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
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
    public const string AdminRoute = "/test-only/admin";
    public const string MemberRoute = "/test-only/member";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            app.UseRouting();

            // The same extension the API uses, deliberately: this host maps its own
            // endpoints, so without sharing the wiring the projection middleware
            // would be absent here and its tests would prove nothing about the API.
            app.UseGotIssuesAuthentication();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet(Route, () => Results.Ok("reached"))
                    .RequireAuthorization();

                // One endpoint per policy, so AC1-AC4 exercise the real policies
                // rather than a test-local approximation of them.
                endpoints.MapGet(AdminRoute, () => Results.Ok("admin"))
                    .RequireAuthorization(AuthorizationPolicies.Admin);

                endpoints.MapGet(MemberRoute, () => Results.Ok("member"))
                    .RequireAuthorization(AuthorizationPolicies.Member);
            });
            next(app);
        };
}
