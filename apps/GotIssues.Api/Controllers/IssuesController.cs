using System.Globalization;
using GotIssues.Api.Authorization;
using GotIssues.Api.Data;
using GotIssues.Contracts.Controllers;
using GotIssues.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GotIssues.Api.Controllers;

/// <summary>
/// Implements the generated <see cref="IssuesApiController"/>.
///
/// <para>
/// No routing attributes, no status-code declarations, no parameter binding — all of
/// that comes from <c>spec/openapi.yaml</c> through generated code (ADR-0004). The
/// policy attributes are here per ADR-0008.
/// </para>
/// </summary>
public sealed class IssuesController(GotIssuesDbContext dbContext) : IssuesApiController
{
    /// <summary>
    /// The largest number a key can express: `spec/openapi.yaml` allows nine digits.
    /// Declared here and in the contract, and the two must agree — this constant is
    /// what makes the refusal happen before a row is written.
    /// </summary>
    private const int MaximumIssueNumber = 999_999_999;

    /// <summary>
    /// Creating an issue is open to any recognised role. Only the three acts named in
    /// PROJECT.md §5 are administrative, and this is not one of them.
    /// </summary>
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public override async Task<IActionResult> CreateIssue(
        string projectKey, CreateIssueRequest createIssueRequest)
    {
        ArgumentNullException.ThrowIfNull(createIssueRequest);

        var cancellationToken = HttpContext.RequestAborted;

        // One transaction covers allocating the number and writing the issue. If the
        // insert fails, the counter increment rolls back with it and the number is
        // returned rather than burned — which is why AC1d can ask for "no number
        // skipped" at all. A sequence could not offer that; nextval() is deliberately
        // outside transaction control.
        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        // UPDATE … RETURNING in one statement: PostgreSQL takes a row lock on this
        // project, so concurrent creates queue behind each other and each sees a
        // distinct number. A read-then-write pair would not — both readers would see
        // the same value, which is precisely the defect AC1d exists to catch.
        //
        // No rows come back when the project does not exist, which is the 404 below;
        // asking separately would be a second round trip and a second race.
        var allocated = await dbContext.Database
            .SqlQuery<int>($"""
                UPDATE projects
                SET "NextIssueNumber" = "NextIssueNumber" + 1
                WHERE "Key" = {projectKey}
                RETURNING "NextIssueNumber" - 1
                """)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (allocated.Count == 0)
        {
            return Problem(
                type: "https://httpstatuses.io/404",
                title: "Project not found.",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No project with key '{projectKey}' exists.");
        }

        // A number beyond nine digits cannot be expressed as a key, because the
        // pattern in spec/openapi.yaml allows nine. Allocating one anyway would
        // return 201 with an identifier this API's own contract rejects, and the
        // issue would be unreachable through the only operation that fetches one —
        // the same defect as the GOTI-0 backfill, arriving from the other end of the
        // range. Refuse it instead, and let the transaction return the number.
        if (allocated[0] > MaximumIssueNumber)
        {
            return Problem(
                type: "https://httpstatuses.io/409",
                title: "Project has exhausted its issue numbers.",
                statusCode: StatusCodes.Status409Conflict,
                detail:
                    $"Project '{projectKey}' has used all {MaximumIssueNumber} issue numbers "
                    + "its key can express.");
        }

        var project = await dbContext.Projects
            .AsNoTracking()
            .SingleAsync(p => p.Key == projectKey, cancellationToken)
            .ConfigureAwait(false);

        var record = new IssueRecord
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Number = allocated[0],
            Title = createIssueRequest.Title,
            Description = createIssueRequest.Description,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Issues.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return StatusCode(StatusCodes.Status201Created, ToContract(record, project.Key));
    }

    /// <summary>Reading an issue is open to any recognised role.</summary>
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public override async Task<IActionResult> GetIssue(string issueKey)
    {
        // The key's shape is enforced by the generated contract, so this split cannot
        // fail on a request that reaches here: a malformed key is a 400 before this
        // method runs.
        var separator = issueKey.LastIndexOf('-');
        var projectKey = issueKey[..separator];
        var number = int.Parse(issueKey[(separator + 1)..], CultureInfo.InvariantCulture);

        var record = await dbContext.Issues
            .AsNoTracking()
            .Include(i => i.Project)
            .SingleOrDefaultAsync(
                i => i.Project.Key == projectKey && i.Number == number,
                HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (record is null)
        {
            return Problem(
                type: "https://httpstatuses.io/404",
                title: "Issue not found.",
                statusCode: StatusCodes.Status404NotFound,
                detail: $"No issue with key '{issueKey}' exists.");
        }

        return Ok(ToContract(record, projectKey));
    }

    private static Issue ToContract(IssueRecord record, string projectKey) => new()
    {
        Id = record.Id,
        Key = $"{projectKey}-{record.Number.ToString(CultureInfo.InvariantCulture)}",
        ProjectKey = projectKey,
        Number = record.Number,
        Title = record.Title,
        Description = record.Description,
        CreatedAt = record.CreatedAt.UtcDateTime,
    };
}
