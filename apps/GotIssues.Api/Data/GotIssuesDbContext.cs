using Microsoft.EntityFrameworkCore;

namespace GotIssues.Api.Data;

public sealed class GotIssuesDbContext(DbContextOptions<GotIssuesDbContext> options)
    : DbContext(options)
{
    public DbSet<PlaceholderRecord> PlaceholderRecords => Set<PlaceholderRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PlaceholderRecord>(entity =>
        {
            entity.ToTable("placeholder_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
