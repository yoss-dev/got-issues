using System.Security.Claims;
using GotIssues.Api.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
public sealed partial class UserProjectionMiddleware(
    RequestDelegate next, ILogger<UserProjectionMiddleware> logger)
{
    private readonly ILogger<UserProjectionMiddleware> _logger = logger;

    /// <summary>Matches the column width in GotIssuesDbContext.</summary>
    private const int MaximumDisplayNameLength = 400;

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

    /// <summary>
    /// Trims a display name to what the column holds.
    ///
    /// Unlike the subject, there is nothing to anchor a limit to: OIDC places no
    /// length on the `name` claim, so any column width is arbitrary and an over-long
    /// name would otherwise fail *every* request from that caller for as long as the
    /// identity provider held it.
    ///
    /// A display name is a convenience field — it is not identity, nothing is keyed
    /// on it, and a truncated one is still useful. Failing a request over it would be
    /// the wrong trade, and silently dropping the whole projection (which is what
    /// happened before the write-failure catch was narrowed) is worse than both.
    /// </summary>
    private static string? Fit(string? displayName)
    {
        if (displayName is not { Length: > MaximumDisplayNameLength })
        {
            return displayName;
        }

        var cut = MaximumDisplayNameLength;

        // Never cut through a surrogate pair. Slicing at a raw UTF-16 index can leave
        // a lone high surrogate, which cannot be encoded to UTF-8 — PostgreSQL then
        // throws an EncoderFallbackException wrapped in DbUpdateException, which is
        // not a unique violation, so it propagates: a hard failure on every request
        // from that caller, which is precisely what this method exists to prevent.
        // Any non-BMP character does it, emoji included, and emoji in display names
        // are ordinary input.
        if (char.IsHighSurrogate(displayName[cut - 1]))
        {
            cut--;
        }

        return displayName[..cut];
    }

    /// <summary>
    /// Records that a name was shortened, never what it was. AC7 forbids logging the
    /// display name; it does not forbid saying one was trimmed, and without this the
    /// projection quietly disagrees with the token forever.
    /// </summary>
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Display name trimmed from {OriginalLength} to {StoredLength} characters.")]
    private partial void LogDisplayNameTrimmed(int originalLength, int storedLength);

    /// <summary>
    /// A unique violation, and nothing else — any other write failure must propagate
    /// rather than be reported to the caller as success.
    ///
    /// Note this matches <em>any</em> unique violation, which is correct while the
    /// primary key is the only unique constraint on this table. Adding a unique index
    /// later would silently widen what gets swallowed here.
    /// </summary>
    private static bool IsDuplicateKey(DbUpdateException exception) =>
        exception.InnerException is PostgresException
        {
            SqlState: PostgresErrorCodes.UniqueViolation,
        };

    private async Task ProjectAsync(
        GotIssuesDbContext dbContext, HttpContext context, string subject)
    {
        // A token may legitimately carry no display name; that must not fail the
        // request, and the caller remains usable as an assignee (AC8).
        var rawName = context.User.FindFirstValue("name")
            ?? context.User.FindFirstValue(ClaimTypes.Name);
        var displayName = Fit(rawName);

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

        // Logged here rather than inside Fit, which runs before the early return
        // above: a request can trim and then write nothing, and one Information line
        // per request from a caller with an over-long name is noise, not a record.
        if (rawName is not null && displayName!.Length != rawName.Length)
        {
            LogDisplayNameTrimmed(rawName.Length, displayName.Length);
        }

        try
        {
            await dbContext.SaveChangesAsync(context.RequestAborted).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (IsDuplicateKey(ex))
        {
            // Two concurrent first requests from the same subject both see no existing
            // record and both insert; the second violates the primary key. The other
            // request created the projection, which is the outcome we wanted — so this
            // is a race won by someone else, not a failure to report to the caller.
            //
            // Narrowed to the unique violation deliberately. Catching DbUpdateException
            // wholesale silently swallowed *every* write failure: a subject longer than
            // the column (255, the OIDC limit itself) returned 200 with no
            // row written and nothing logged, leaving a caller who appeared to succeed
            // permanently unusable as an assignee. Found in acceptance; the broad catch
            // turned a loud failure into an invisible one.
            dbContext.ChangeTracker.Clear();
        }
    }
}
