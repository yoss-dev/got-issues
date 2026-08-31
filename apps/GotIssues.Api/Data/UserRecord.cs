namespace GotIssues.Api.Data;

/// <summary>
/// The API's thin projection of a user, built from token claims so that issues can be
/// assigned and comments attributed.
///
/// It deliberately holds <b>no role and no credential</b>. Duende owns credentials,
/// and the role travels in the token and is read per request (<c>PROJECT.md</c> §5) —
/// caching it here would create a second source of truth that could disagree with the
/// token a caller just presented.
/// </summary>
public sealed class UserRecord
{
    /// <summary>The token's subject claim. Stable identity; never the display name.</summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// Optional. A token may carry no display-name claim, and that must not fail the
    /// request — the caller is still usable as an assignee (T-0009 AC8).
    /// </summary>
    public string? DisplayName { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    /// <summary>
    /// When this caller was last seen, to a precision of about five minutes.
    ///
    /// Deliberately imprecise: the projection sits on the request path, and refreshing
    /// this on every authenticated request would cost a database round trip per call.
    /// Do not build anything needing exact last-activity times on it without changing
    /// that trade first.
    /// </summary>
    public DateTimeOffset LastSeenAt { get; set; }
}
