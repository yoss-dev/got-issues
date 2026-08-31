using Duende.IdentityServer.Models;

namespace GotIssues.IdentityHost.Configuration;

/// <summary>
/// The API surface this identity host issues tokens for. One scope and one resource:
/// Got Issues is a single API, and inventing more would be speculative.
/// </summary>
public static class IdentityResources
{
    /// <summary>The scope machine clients request.</summary>
    public const string ApiScope = "gotissues.api";

    /// <summary>The audience the API validates tokens against.</summary>
    public const string ApiAudience = "gotissues-api";

    public static IReadOnlyCollection<ApiScope> Scopes { get; } =
        [new ApiScope(ApiScope, "Got Issues API")];

    public static IReadOnlyCollection<ApiResource> Resources { get; } =
        [new ApiResource(ApiAudience, "Got Issues API") { Scopes = { ApiScope } }];
}
