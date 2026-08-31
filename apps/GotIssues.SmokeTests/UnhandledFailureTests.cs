using System.Net;
using System.Net.Http.Headers;
using GotIssues.SmokeTests.Infrastructure;

namespace GotIssues.SmokeTests;

/// <summary>
/// The API answers with the shape its contract declares even when a dependency fails
/// underneath it.
///
/// <para>
/// <b>Why the smoke tier — precisely.</b> Not because the integration tier cannot
/// assert response shapes: an exception thrown during <em>endpoint execution</em>
/// unwinds into <c>UseExceptionHandler</c> there exactly as it does in production, and
/// asserting its status, media type and body is entirely possible. That correction is
/// worth stating plainly, because this project's signature defect is *missing*
/// response-shape assertions, and a comment implying they are impossible in the
/// habitual tier would discourage the thing most worth doing.
/// </para>
/// <para>
/// What the integration tier cannot reach is a failure raised <em>upstream of the
/// application's own pipeline</em>. Its host injects authentication through an
/// <c>IStartupFilter</c>, so the authentication middleware — and
/// <c>UserProjectionMiddleware</c> with it — runs above <c>UseExceptionHandler</c>;
/// a database failure there reaches the client as a thrown exception. The same
/// arrangement is why the 401's body is asserted in <c>ResourceServerTests</c> rather
/// than here, and why the 403's is asserted nowhere (T-0004 review, N1).
/// </para>
/// <para>
/// This test stops the database out from under a live stack, which fails the request
/// wherever the work happens rather than choosing a layer — so it needs the real
/// pipeline, and only this tier has one.
/// </para>
/// </summary>
[Collection(SerialExecution.Name)]
public sealed class UnhandledFailureTests
{
    [Fact]
    public async Task A_failure_underneath_the_api_returns_a_problem_document_not_an_empty_body()
    {
        // Found by acceptance on T-0004: an unexpected failure produced HTTP 500 with a
        // zero-length body and no Content-Type — a response the contract never declared.
        await using var stack = new ComposeProject(ComposeProject.UniqueName("unhandled"));

        (await stack.BuildAsync()).EnsureSucceeded("docker compose build");
        (await stack.UpAsync()).EnsureSucceeded("docker compose up --wait");

        var tokens = new TokenFactory(stack);
        var token = await tokens.IssuedTokenAsync(
            ComposeProject.MemberClientId, ComposeProject.MemberClientSecret);
        var api = await stack.BaseAddressAsync("api");

        using var client = new HttpClient { BaseAddress = api, Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Warm the API's issuer metadata *before* stopping the database. Without this
        // the API cannot fetch the identity host's signing keys once postgres is gone,
        // and the request comes back 401 — which looks like a failing test and is
        // actually a different failure entirely. The first version of this test made
        // exactly that mistake, and it also made a mutation of the exception handler
        // appear to be caught when the 401 was doing the work.
        using (var warmup = await client.GetAsync(new Uri("/projects", UriKind.Relative)))
        {
            Assert.Equal(HttpStatusCode.OK, warmup.StatusCode);
        }

        // Now stop the database out from under a healthy, already-authenticating API,
        // so the request fails where the work happens — the failure this test is about.
        (await stack.ComposeAsync("stop", "postgres")).EnsureSucceeded("docker compose stop postgres");

        try
        {
            using var response = await client.GetAsync(new Uri("/projects", UriKind.Relative));
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

            // The three things that were wrong, asserted separately so a partial
            // regression stays legible.
            Assert.Equal(
                "application/problem+json",
                response.Content.Headers.ContentType?.MediaType);
            Assert.False(string.IsNullOrWhiteSpace(body), "The 500 carried no body at all.");
            Assert.Contains("\"status\":500", body, StringComparison.Ordinal);

            // And nothing about the failure leaks: an exception message can carry a
            // connection string or user input (SECURITY.md).
            Assert.DoesNotContain("Password", body, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Npgsql", body, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            (await stack.ComposeAsync("start", "postgres")).EnsureSucceeded("docker compose start postgres");
        }
    }
}
