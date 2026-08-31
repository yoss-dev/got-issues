namespace GotIssues.Api.Authorization;

/// <summary>
/// The two global roles (<c>PROJECT.md</c> §5). Roles are company-wide, never per
/// project, and the API never stores them — the value is read from the token's
/// <c>role</c> claim on every request.
/// </summary>
public static class GotIssuesRoles
{
    public const string Admin = "admin";
    public const string Member = "member";

    /// <summary>
    /// The known roles, as an allow-list.
    ///
    /// This is deliberately an allow-list rather than a default. A policy written as
    /// "admin if the claim says admin, otherwise member" reads correctly and silently
    /// promotes a caller whose claim is missing or holds an unrecognised value — the
    /// exact failure T-0009 AC4 exists to prevent, and the kind that looks like
    /// working code in review.
    /// </summary>
    public static readonly IReadOnlySet<string> Known =
        new HashSet<string>([Admin, Member], StringComparer.Ordinal);
}
