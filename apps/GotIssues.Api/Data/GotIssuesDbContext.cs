using Microsoft.EntityFrameworkCore;

namespace GotIssues.Api.Data;

public sealed class GotIssuesDbContext(DbContextOptions<GotIssuesDbContext> options)
    : DbContext(options)
{
    public DbSet<PlaceholderRecord> PlaceholderRecords => Set<PlaceholderRecord>();

    public DbSet<UserRecord> Users => Set<UserRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserRecord>(entity =>
        {
            entity.ToTable("users");
            // The token's subject is the key. Nothing here stores a role or a
            // credential — see UserRecord.
            entity.HasKey(e => e.Subject);
            entity.Property(e => e.Subject).HasMaxLength(200);
            entity.Property(e => e.DisplayName).HasMaxLength(400);
            entity.Property(e => e.FirstSeenAt).IsRequired();
            entity.Property(e => e.LastSeenAt).IsRequired();
        });

        modelBuilder.Entity<PlaceholderRecord>(entity =>
        {
            entity.ToTable("placeholder_records");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
