using GotIssues.Api.Data;
using GotIssues.Contracts.Controllers;
using GotIssues.Contracts.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GotIssues.Api.Controllers;

/// <summary>
/// Implements the generated <see cref="PlaceholderApiController"/>.
///
/// Note what is absent: there are no routing attributes, no status-code
/// declarations and no parameter binding here. All of that lives in
/// <c>spec/openapi.yaml</c> and reaches this class through generated code
/// (ADR-0004). Adding a route attribute to this file would be a review rejection.
/// </summary>
public sealed class PlaceholderController(GotIssuesDbContext dbContext) : PlaceholderApiController
{
    private const int DefaultPageSize = 20;

    public override async Task<IActionResult> CreatePlaceholder(
        CreatePlaceholderRequest createPlaceholderRequest)
    {
        ArgumentNullException.ThrowIfNull(createPlaceholderRequest);

        var record = new PlaceholderRecord
        {
            Id = Guid.NewGuid(),
            Label = createPlaceholderRequest.Label,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        dbContext.PlaceholderRecords.Add(record);
        await dbContext.SaveChangesAsync(HttpContext.RequestAborted).ConfigureAwait(false);

        return StatusCode(StatusCodes.Status201Created, ToContract(record));
    }

    public override async Task<IActionResult> ListPlaceholders(int? page, int? pageSize)
    {
        // Both parameters carry [Range(...)] on the generated contract, from the
        // bounds declared in the specification, so out-of-range values are rejected
        // with a problem document before reaching this method. Nothing is silently
        // adjusted here: that would be a validation rule living only in code, which
        // is exactly what the contract exists to prevent.
        var pageNumber = page ?? 1;
        var size = pageSize ?? DefaultPageSize;

        var query = dbContext.PlaceholderRecords.AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ThenBy(r => r.Id);   // stable tiebreaker, so paging cannot duplicate or skip

        var total = await query.CountAsync(HttpContext.RequestAborted).ConfigureAwait(false);
        var items = await query
            .Skip((pageNumber - 1) * size)
            .Take(size)
            .ToListAsync(HttpContext.RequestAborted)
            .ConfigureAwait(false);

        return Ok(new PlaceholderPage
        {
            Items = [.. items.Select(ToContract)],
            Page = pageNumber,
            PageSize = size,
            TotalCount = total,
        });
    }

    private static Placeholder ToContract(PlaceholderRecord record) => new()
    {
        Id = record.Id,
        Label = record.Label,
        CreatedAt = record.CreatedAt.UtcDateTime,
    };
}
