namespace GotIssues.Api.Data;

/// <summary>
/// A project: a name, and the key its issues are numbered under.
///
/// <para>
/// <see cref="Key"/> is <c>init</c>-only on purpose. Every issue reference derives
/// from it — including ones written in commit messages and chat logs outside this
/// system — so changing it orphans them all (T-0004 AC1d). There is no operation
/// that changes a key, and this makes that true in the type rather than only in the
/// absence of an endpoint.
/// </para>
/// </summary>
public sealed class ProjectRecord
{
    public Guid Id { get; init; }

    /// <summary>The immutable, deployment-unique key, e.g. <c>GOTI</c>.</summary>
    public required string Key { get; init; }

    /// <summary>The display name. Not unique — the key is the identifier.</summary>
    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// The next issue number to allocate in this project.
    ///
    /// <para>
    /// Allocation is <c>UPDATE … SET NextIssueNumber = NextIssueNumber + 1 …
    /// RETURNING</c> inside the creating transaction, so PostgreSQL's row lock
    /// serialises concurrent creates <em>for this project only</em>, and a rollback
    /// returns the number rather than burning it. A sequence would do neither: it
    /// needs DDL at runtime, which T-0013 exists to take away from this role, and it
    /// is deliberately non-transactional, so a rollback leaves a gap.
    /// </para>
    /// </summary>
    public int NextIssueNumber { get; set; } = 1;
}
