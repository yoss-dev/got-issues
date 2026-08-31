namespace GotIssues.Api.Data;

/// <summary>
/// A unit of work inside a project.
///
/// <para>
/// <see cref="Number"/> is allocated from the owning project's counter, not derived
/// from the issues that exist. That is what makes a number permanently retired: an
/// issue removed later cannot hand its number to another, because the counter has
/// already moved past it. <c>MAX(number) + 1</c> would quietly break that property
/// as well as duplicating under concurrency.
/// </para>
/// </summary>
public sealed class IssueRecord
{
    public Guid Id { get; init; }

    /// <summary>The owning project. An issue cannot exist without one.</summary>
    public Guid ProjectId { get; init; }

    /// <summary>The number within the project, starting at 1. Never reused.</summary>
    public int Number { get; init; }

    /// <summary>A one-line summary.</summary>
    public required string Title { get; set; }

    /// <summary>Optional free text; multi-line by design.</summary>
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>What kind of work this is.</summary>
    public IssueType Type { get; set; } = IssueType.Task;

    /// <summary>Where the work stands. No transition rules — any value may follow any other.</summary>
    public IssueStatus Status { get; set; } = IssueStatus.Open;

    /// <summary>How urgent the work is.</summary>
    public IssuePriority Priority { get; set; } = IssuePriority.Normal;

    /// <summary>
    /// The subject of the person holding this issue, or null when nobody does.
    ///
    /// <para>
    /// Never-assigned and since-unassigned are deliberately the same state: assignment
    /// history is not kept, and a tri-state here would imply an audit trail that does
    /// not exist.
    /// </para>
    /// </summary>
    public string? AssigneeSubject { get; set; }

    public UserRecord? Assignee { get; set; }

    public ProjectRecord Project { get; init; } = null!;
}
