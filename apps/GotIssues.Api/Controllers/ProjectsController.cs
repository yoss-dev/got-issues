using GotIssues.Api.Authorization;
using GotIssues.Api.Data;
using GotIssues.Contracts.Controllers;
using GotIssues.Contracts.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GotIssues.Api.Controllers;

/// <summary>
/// Implements the generated <see cref="ProjectsApiController"/>.
///
/// <para>
/// Note what is absent: no routing attributes, no status-code declarations, no
/// parameter binding. All of that lives in <c>spec/openapi.yaml</c> and reaches this
/// class through generated code (ADR-0004). Adding a route attribute here would be a
/// review rejection.
/// </para>
/// <para>
/// What <em>is</em> here: the <see cref="AuthorizeAttribute"/> policies, per
/// <b>ADR-0008</b> — a role restriction is enforced by a policy attribute and declared
/// in the contract as a description plus a <c>403</c>. The reasoning lives in the ADR
/// and is not repeated here; a second copy would be free to drift from the decision.
/// </para>
/// </summary>
public sealed class ProjectsController(GotIssuesDbContext dbContext) : ProjectsApiController
{
    private const int DefaultPageSize = 20;

    /// <summary>Creating a project is one of the three admin acts (PROJECT.md §5).</summary>
    [Authorize(Policy = AuthorizationPolicies.Admin)]
    public override async Task<IActionResult> CreateProject(CreateProjectRequest createProjectRequest)
    {
        ArgumentNullException.ThrowIfNull(createProjectRequest);

        var record = new ProjectRecord
        {
            Id = Guid.NewGuid(),
            Key = createProjectRequest.Key,
            Name = createProjectRequest.Name,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.Projects.Add(record);

        try
        {
            await dbContext.SaveChangesAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            // Deliberately no read-then-insert check before this. Such a check narrows
            // the race without closing it — two concurrent creates both find nothing
            // and both insert — so it would produce a friendlier message while leaving
            // the defect it appears to fix. The unique index is the guarantee; this is
            // where it becomes a 409.
            dbContext.ChangeTracker.Clear();

            // ControllerBase.Problem(), not Conflict(new Problem{…}): the latter
            // serialises the right shape with the wrong content type —
            // `application/json` where the specification declares
            // `application/problem+json`. That is T-0002's defect 5 exactly (a 401
            // declaring a problem document and not returning one), and it was caught
            // here by a test asserting the media type rather than only the status.
            return Problem(
                type: "https://httpstatuses.io/409",
                title: "Project key already in use.",
                statusCode: StatusCodes.Status409Conflict,
                detail: $"A project with key '{createProjectRequest.Key}' already exists.");
        }

        return StatusCode(StatusCodes.Status201Created, ToContract(record));
    }

    /// <summary>Listing is open to any caller holding a recognised role.</summary>
    [Authorize(Policy = AuthorizationPolicies.Member)]
    public override async Task<IActionResult> ListProjects(int? page, int? pageSize)
    {
        // Both parameters carry [Range(...)] on the generated contract, from the bounds
        // declared in the specification, so out-of-range values are rejected with a
        // problem document before reaching this method. Nothing is silently adjusted
        // here: that would be a validation rule living only in code.
        var pageNumber = page ?? 1;
        var size = pageSize ?? DefaultPageSize;

        var query = dbContext.Projects.AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ThenBy(p => p.Id);   // stable tiebreaker, so paging cannot duplicate or skip

        var total = await query.CountAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        var items = await query
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToListAsync(HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return Ok(new ProjectPage
        {
            Items = [.. items.Select(ToContract)],
            Page = pageNumber,
            PageSize = size,
            TotalCount = total,
        });
    }

    /// <summary>
    /// A unique violation, and nothing else — any other write failure must propagate
    /// rather than be reported to the caller as a key collision. Narrow on purpose:
    /// the same broad-catch mistake cost T-0009 an acceptance round.
    /// </summary>
    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };

    private static Project ToContract(ProjectRecord record) => new()
    {
        Id = record.Id,
        Key = record.Key,
        Name = record.Name,
        CreatedAt = record.CreatedAt.UtcDateTime,
    };
}
