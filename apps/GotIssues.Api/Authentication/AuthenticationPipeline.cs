namespace GotIssues.Api.Authentication;

/// <summary>
/// The order in which authentication, the user projection and authorisation run.
///
/// This exists as one method rather than three calls in <c>Program.cs</c> because the
/// integration test host builds its own front-of-pipeline, and a duplicated ordering
/// would let the two drift — producing tests that pass while production is wrong.
/// T-0002 hit exactly that with an unauthenticated 401: the fix looked verified
/// because the test host had been made to agree with the test, not with the app.
///
/// The projection sits between the other two deliberately: the caller is identified
/// by then, and a record must exist before anything can assign work to them.
/// </summary>
public static class AuthenticationPipeline
{
    public static IApplicationBuilder UseGotIssuesAuthentication(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseAuthentication();
        app.UseMiddleware<UserProjectionMiddleware>();
        app.UseAuthorization();

        return app;
    }
}
