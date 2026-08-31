namespace GotIssues.Api.Data;

/// <summary>
/// The lifecycle vocabularies, as the domain holds them.
///
/// <para>
/// Deliberately not the generated contract's enums. Those belong to the API surface and
/// are regenerated from <c>spec/openapi.yaml</c>; binding persistence to them would make
/// a contract change a schema change. The controller maps between the two, and ADR-0010
/// will formalise that separation.
/// </para>
/// </summary>
public enum IssueType
{
    Bug = 1,
    Task = 2,
}

/// <summary>Where the work stands. Any value may follow any other — see T-0006 AC5.</summary>
public enum IssueStatus
{
    Open = 1,
    InProgress = 2,
    Done = 3,
}

public enum IssuePriority
{
    Low = 1,
    Normal = 2,
    High = 3,
}
