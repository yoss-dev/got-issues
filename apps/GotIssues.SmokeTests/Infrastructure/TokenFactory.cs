using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GotIssues.SmokeTests.Infrastructure;

/// <summary>
/// Obtains genuine tokens from the running identity host, and mints the refusal cases.
///
/// The accepted case (AC6) is always a real token from the token endpoint — a happy path
/// proved with a synthetic token proves nothing about the issuer.
///
/// The three refusals are minted against the identity host's own signing key, read from
/// the running container. The reason is `ClockSkew`: the API leaves JwtBearer's default
/// five-minute grace, so a token that has genuinely just expired is still accepted, and a
/// check that waited out that window would take longer than the entire suite. Minting
/// with the real key sets `exp` well outside the window and keeps everything else
/// identical, so the refusal is attributable to expiry and nothing else.
///
/// The five-minute grace is a framework default nobody chose. It is out of scope here —
/// this ticket adds verification, it does not change the resource server — and is raised
/// as its own ticket rather than fixed in passing.
/// </summary>
public sealed class TokenFactory(ComposeProject stack)
{
    private const string SigningKeyPath = "/app/keys/tempkey.jwk";

    /// <summary>A genuine access token, obtained the way a real client obtains one.</summary>
    public async Task<string> IssuedTokenAsync(string clientId, string clientSecret)
    {
        var address = await stack.BaseAddressAsync("identity").ConfigureAwait(false);
        using var client = new HttpClient { BaseAddress = address, Timeout = TimeSpan.FromSeconds(30) };

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["scope"] = "gotissues.api",
        });

        using var response = await client
            .PostAsync(new Uri("/connect/token", UriKind.Relative), content).ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        Assert.True(response.IsSuccessStatusCode, $"Token request failed ({(int)response.StatusCode}): {body}");

        using var document = JsonDocument.Parse(body);
        return document.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Token response carried no access_token.");
    }

    /// <summary>
    /// The identity host's real signing key, read out of the running container. Minting
    /// with it is what isolates the single defect under test in each refusal case.
    /// </summary>
    public async Task<SecurityKey> SigningKeyAsync()
    {
        var result = await stack.ExecAsync("identity", "cat", SigningKeyPath).ConfigureAwait(false);
        result.EnsureSucceeded($"reading {SigningKeyPath} from the identity container");

        return new JsonWebKey(result.StandardOutput.Trim());
    }

    public async Task<string> ExpiredTokenAsync() =>
        Mint(await SigningKeyAsync().ConfigureAwait(false),
            audience: ComposeProject.ApiAudience,
            // Well outside JwtBearer's five-minute default grace, so the refusal is
            // attributable to expiry rather than to timing.
            expires: DateTime.UtcNow.AddHours(-1));

    public async Task<string> WrongAudienceTokenAsync() =>
        Mint(await SigningKeyAsync().ConfigureAwait(false),
            audience: "some-other-api",
            expires: DateTime.UtcNow.AddMinutes(30));

    /// <summary>
    /// Signed by a key the identity host has never published — the case T-0010 called
    /// "the one that matters", because it is the difference between validating a
    /// signature and merely reading a token.
    /// </summary>
    public static string UnknownKeyToken()
    {
        using var rsa = RSA.Create(2048);
        var key = new RsaSecurityKey(rsa.ExportParameters(true)) { KeyId = "smoke-unknown-key" };

        return Mint(key, ComposeProject.ApiAudience, DateTime.UtcNow.AddMinutes(30));
    }

    private static string Mint(SecurityKey key, string audience, DateTime expires)
    {
        var handler = new JsonWebTokenHandler();

        return handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = ComposeProject.IssuerUri,
            Audience = audience,
            Expires = expires,
            IssuedAt = DateTime.UtcNow.AddHours(-2),
            NotBefore = DateTime.UtcNow.AddHours(-2),
            Claims = new Dictionary<string, object>
            {
                ["client_id"] = ComposeProject.MemberClientId,
                ["role"] = "member",
                ["scope"] = "gotissues.api",
                ["jti"] = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
            },
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
        });
    }

    /// <summary>Calls the API's authenticated endpoint with the given token.</summary>
    public static async Task<System.Net.HttpStatusCode> CallAuthenticatedAsync(Uri apiAddress, string? token)
    {
        using var client = new HttpClient { BaseAddress = apiAddress, Timeout = TimeSpan.FromSeconds(30) };

        if (token is not null)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client
            .GetAsync(new Uri("/health/authenticated", UriKind.Relative)).ConfigureAwait(false);

        return response.StatusCode;
    }
}
