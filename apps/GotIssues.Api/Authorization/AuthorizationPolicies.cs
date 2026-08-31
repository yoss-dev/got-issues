using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace GotIssues.Api.Authorization;

/// <summary>
/// The project's two authorisation policies, registered once and centrally so that
/// no endpoint re-derives what a role means.
///
/// <para>
/// <b>These constants are the sanctioned mechanism. <c>[Authorize(Roles = "…")]</c>
/// and <c>RequireRole</c> are NOT equivalent and must not be used instead.</b>
/// </para>
/// <para>
/// The framework's role syntax does an exact match, so <c>Roles = "member"</c>
/// <i>refuses an admin</i> — while <see cref="Member"/> is a floor that an admin
/// satisfies, because an admin can do anything a member can (<c>PROJECT.md</c> §5).
/// The divergence fails closed, denying access it should grant rather than granting
/// access it should deny, which makes it quiet rather than harmless: an endpoint
/// guarded the framework way would simply turn admins away.
/// </para>
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Restricted to callers holding the <c>admin</c> role.</summary>
    public const string Admin = "admin-policy";

    /// <summary>
    /// Open to any caller holding a recognised role. Admins satisfy it too: an admin
    /// can do anything a member can (<c>PROJECT.md</c> §5), so this is a floor rather
    /// than an exact match.
    /// </summary>
    public const string Member = "member-policy";

    public static void AddGotIssuesPolicies(this AuthorizationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.AddPolicy(Admin, policy =>
            policy.RequireAssertion(context => HasRole(context.User, GotIssuesRoles.Admin)));

        options.AddPolicy(Member, policy =>
            policy.RequireAssertion(context =>
                HasRole(context.User, GotIssuesRoles.Admin)
                || HasRole(context.User, GotIssuesRoles.Member)));
    }

    /// <summary>
    /// True only when the caller carries the named role <em>and</em> that role is one
    /// this system recognises. An unrecognised value satisfies nothing, so a token
    /// claiming <c>role: superuser</c> is refused rather than treated as a member.
    /// </summary>
    private static bool HasRole(ClaimsPrincipal user, string role) =>
        user.Identity?.IsAuthenticated == true
        && RoleValues(user)
            .Where(GotIssuesRoles.Known.Contains)
            .Any(value => string.Equals(value, role, StringComparison.Ordinal));

    /// <summary>
    /// Role values under either claim type.
    ///
    /// <c>Program.cs</c> sets <c>MapInboundClaims = false</c> so the identity host's
    /// short <c>role</c> claim survives intact — that is the fix. This also accepts
    /// the mapped <see cref="ClaimTypes.Role"/> URI, so if inbound mapping is ever
    /// re-enabled, or a scheme is added that maps, authorisation degrades to working
    /// rather than silently refusing everyone.
    ///
    /// It defends against a failure that already happened: the policies read only
    /// <c>role</c>, the JWT pipeline produced only the URI, and every integration
    /// test passed because the test host produced the short name the pipeline never
    /// does. It failed closed, so no permitted-path test noticed.
    /// </summary>
    private static IEnumerable<string> RoleValues(ClaimsPrincipal user) =>
        user.FindAll("role")
            .Concat(user.FindAll(ClaimTypes.Role))
            .Select(claim => claim.Value);
}
