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

    public ProjectRecord Project { get; init; } = null!;
}
