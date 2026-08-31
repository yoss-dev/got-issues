namespace GotIssues.IdentityHost.Configuration;

/// <summary>
/// The development identities seeded on first start. Bound from configuration —
/// the values come from the environment (see <c>.env.example</c>) and are never
/// committed (T-0010 AC10).
/// </summary>
public sealed class SeedConfiguration
{
    public const string SectionName = "Seed";

    /// <summary>Client id → seed definition. Two are expected: one admin, one member.</summary>
    public IList<SeedClient> Clients { get; } = [];
}

public sealed class SeedClient
{
    public string ClientId { get; set; } = string.Empty;

    public string Secret { get; set; } = string.Empty;

    /// <summary>The global role this identity carries: <c>admin</c> or <c>member</c>.</summary>
    public string Role { get; set; } = string.Empty;
}
