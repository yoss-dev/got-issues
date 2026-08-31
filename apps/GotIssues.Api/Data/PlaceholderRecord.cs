namespace GotIssues.Api.Data;

/// <summary>
/// Placeholder entity. T-0001 delivers the persistence *mechanism* — a DbContext,
/// a migration, and an explicit migration step — not the domain. The real model
/// (projects, issues, comments) arrives with T-0004 onward and replaces this.
/// </summary>
public sealed class PlaceholderRecord
{
    public Guid Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
