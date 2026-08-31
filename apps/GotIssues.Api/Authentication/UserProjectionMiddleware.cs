using System.Security.Claims;
using GotIssues.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GotIssues.Api.Authentication;

/// <summary>
/// Keeps the API's thin user projection in step with the tokens it sees.
///
/// Runs after authentication, on authenticated requests only. It writes solely when
/// something actually changed: this sits on the request path, and an unconditional
/// write would cost a database round trip per call (recorded as a risk on T-0010).
///
/// Nothing here logs the display name — it is personal data belonging to an
/// identifiable employee (<c>SECURITY.md</c>, and <c>PROJECT.md</c> Q8).
/// </summary>
public sealed class UserProjectionMiddleware(RequestDelegate next)
{
    /// <summary>How stale a last-seen timestamp may get before it is worth a write.</summary>
    private static readonly TimeSpan LastSeenPrecision = TimeSpan.FromMinutes(5);

    public async Task InvokeAsync(HttpContext context, GotIssuesDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(dbContext);

        var subject = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");

        if (context.User.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(subject))
        {
            await ProjectAsync(dbContext, context, subject).ConfigureAwait(false);
        }

        await next(context).ConfigureAwait(false);
    }

    private static async Task ProjectAsync(
        GotIssuesDbContext dbContext, HttpContext context, string subject)
    {
        // A token may legitimately carry no display name; that must not fail the
        // request, and the caller remains usable as an assignee (AC8).
        var displayName = context.User.FindFirstValue("name")
            ?? context.User.FindFirstValue(ClaimTypes.Name);

        var now = DateTimeOffset.UtcNow;
        var existing = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Subject == subject, context.RequestAborted)
            .ConfigureAwait(false);

        if (existing is null)
        {
            dbContext.Users.Add(new UserRecord
            {
                Subject = subject,
                DisplayName = displayName,
                FirstSeenAt = now,
                LastSeenAt = now,
            });
        }
        else
        {
            var nameChanged = !string.Equals(existing.DisplayName, displayName, StringComparison.Ordinal);
            var lastSeenStale = now - existing.LastSeenAt > LastSeenPrecision;

            if (!nameChanged && !lastSeenStale)
            {
                return;   // nothing worth a write
            }

            existing.DisplayName = displayName;
            existing.LastSeenAt = now;
        }

        try
        {
            await dbContext.SaveChangesAsync(context.RequestAborted).ConfigureAwait(false);
        }
        catch (DbUpdateException)
        {
            // Two concurrent first requests from the same subject both see no existing
            // record and both insert; the second violates the primary key. The other
            // request created the projection, which is the outcome we wanted — so this
            // is a race won by someone else, not a failure to report to the caller.
            //
            // The alternative, an unhandled DbUpdateException, is a 500 on a caller's
            // very first request. Same shape as T-0004's recorded risk: a read-then-
            // insert check that passes every single-threaded test.
            dbContext.ChangeTracker.Clear();
        }
    }
}
