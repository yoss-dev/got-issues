using System.Security.Claims;
using GotIssues.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace GotIssues.Api.UnitTests;

/// <summary>
/// The policies evaluated directly, against principals shaped the way each part of
/// the system actually produces them.
///
/// **These tests exist because the integration suite could not catch the bug they
/// guard.** The policies read a short <c>role</c> claim; JwtBearer's default inbound
/// mapping rewrites it to the WS-Federation URI; and every integration test passed
/// regardless, because the test host builds the short claim the JWT pipeline never
/// produces. The suite agreed with the test host rather than with reality, and the
/// failure was closed — a genuine admin was refused, and no permitted-path test
/// noticed because none used a real token's claim shape.
///
/// So this file deliberately uses **no test authentication handler**. It builds the
/// principals itself, both ways, and asks the real policies.
/// </summary>
public sealed class AuthorizationPolicyTests
{
    private static readonly IAuthorizationService Authorization = BuildAuthorizationService();

    private static IAuthorizationService BuildAuthorizationService()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options => options.AddGotIssuesPolicies());
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }

    private static ClaimsPrincipal PrincipalWith(string claimType, string role) =>
        new(new ClaimsIdentity([new Claim(claimType, role)], authenticationType: "test"));

    /// <summary>The short name the identity host writes and MapInboundClaims=false preserves.</summary>
    public const string ShortClaim = "role";

    private static async Task<bool> Allows(ClaimsPrincipal user, string policy) =>
        (await Authorization.AuthorizeAsync(user, resource: null, policy)).Succeeded;

    [Theory]
    [InlineData(ShortClaim)]
    [InlineData(ClaimTypes.Role)]
    public async Task An_admin_satisfies_both_policies_under_either_claim_type(string claimType)
    {
        var admin = PrincipalWith(claimType, GotIssuesRoles.Admin);

        Assert.True(await Allows(admin, AuthorizationPolicies.Admin));
        Assert.True(await Allows(admin, AuthorizationPolicies.Member));
    }

    [Theory]
    [InlineData(ShortClaim)]
    [InlineData(ClaimTypes.Role)]
    public async Task A_member_satisfies_only_the_member_policy_under_either_claim_type(string claimType)
    {
        var member = PrincipalWith(claimType, GotIssuesRoles.Member);

        Assert.False(await Allows(member, AuthorizationPolicies.Admin));
        Assert.True(await Allows(member, AuthorizationPolicies.Member));
    }

    [Theory]
    [InlineData(ShortClaim, "superuser")]
    [InlineData(ShortClaim, "Admin")]
    [InlineData(ShortClaim, "")]
    [InlineData(ShortClaim, " admin")]
    [InlineData(ClaimTypes.Role, "superuser")]
    [InlineData(ClaimTypes.Role, "Admin")]
    public async Task An_unrecognised_role_satisfies_nothing_under_either_claim_type(
        string claimType, string role)
    {
        var stranger = PrincipalWith(claimType, role);

        Assert.False(await Allows(stranger, AuthorizationPolicies.Admin));
        Assert.False(await Allows(stranger, AuthorizationPolicies.Member));
    }

    [Fact]
    public async Task A_principal_with_no_role_claim_satisfies_nothing()
    {
        var anonymousish = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "s-1")], "test"));

        Assert.False(await Allows(anonymousish, AuthorizationPolicies.Admin));
        Assert.False(await Allows(anonymousish, AuthorizationPolicies.Member));
    }

    [Fact]
    public async Task An_unauthenticated_principal_satisfies_nothing_even_holding_admin()
    {
        // No authentication type ⇒ not authenticated, whatever the claims say.
        var unauthenticated = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim("role", GotIssuesRoles.Admin)]));

        Assert.False(await Allows(unauthenticated, AuthorizationPolicies.Admin));
        Assert.False(await Allows(unauthenticated, AuthorizationPolicies.Member));
    }

    [Fact]
    public async Task One_recognised_role_among_several_claims_is_enough()
    {
        // A token may legitimately carry more than one role claim.
        var multiple = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("role", "superuser"),
                new Claim(ClaimTypes.Role, GotIssuesRoles.Admin),
            ],
            authenticationType: "test"));

        Assert.True(await Allows(multiple, AuthorizationPolicies.Admin));
    }
}
