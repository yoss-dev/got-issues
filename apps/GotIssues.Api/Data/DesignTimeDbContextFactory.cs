using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GotIssues.Api.Data;

/// <summary>
/// Used only by <c>dotnet ef</c> at design time (scaffolding migrations). EF never
/// opens this connection, so the fallback carries no credentials — deliberately, so
/// that nothing password-shaped is ever committed (T-0001 AC8). Point it at a real
/// database by exporting <c>ConnectionStrings__GotIssues</c> when a command needs one.
/// </summary>
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<GotIssuesDbContext>
{
    public GotIssuesDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__GotIssues")
            ?? "Host=localhost;Database=gotissues_design_time";

        var options = new DbContextOptionsBuilder<GotIssuesDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new GotIssuesDbContext(options);
    }
}
