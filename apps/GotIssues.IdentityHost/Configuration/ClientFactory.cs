using Duende.IdentityServer.Models;

namespace GotIssues.IdentityHost.Configuration;

/// <summary>
/// Turns a configured seed identity into a Duende client. Identities here are OAuth
/// <em>clients</em>, not users: interactive login and user registration are out of
/// scope for T-0010, and client-credentials tokens carry no user subject.
/// </summary>
public static class ClientFactory
{
    public static Client Build(SeedClient definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        return new Client
        {
            ClientId = definition.ClientId,
            ClientSecrets = { new Secret(definition.Secret.Sha256()) },
            AllowedGrantTypes = GrantTypes.ClientCredentials,
            AllowedScopes = { IdentityResources.ApiScope },

            // The role travels as a claim in the token; the API reads it per request
            // and never stores it (PROJECT.md §5). AlwaysSendClientClaims puts it in a
            // client-credentials token, and the empty prefix keeps it named `role`
            // rather than `client_role`, which is what T-0009's policies will read.
            Claims = { new ClientClaim("role", definition.Role) },
            AlwaysSendClientClaims = true,
            ClientClaimsPrefix = string.Empty,
        };
    }
}
