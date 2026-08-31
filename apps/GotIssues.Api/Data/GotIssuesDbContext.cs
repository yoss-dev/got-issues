using Microsoft.EntityFrameworkCore;

namespace GotIssues.Api.Data;

public sealed class GotIssuesDbContext(DbContextOptions<GotIssuesDbContext> options)
    : DbContext(options)
{
    public DbSet<ProjectRecord> Projects => Set<ProjectRecord>();

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
            // 255 is the limit OpenID Connect Core places on `sub`, so a legal
            // subject always fits. The column was 200, which made any subject of
            // 201-255 characters a hard failure on every request once the write-
            // failure catch was correctly narrowed — legal input the system refused.
            entity.Property(e => e.Subject).HasMaxLength(255);
            entity.Property(e => e.DisplayName).HasMaxLength(400);
            entity.Property(e => e.FirstSeenAt).IsRequired();
            entity.Property(e => e.LastSeenAt).IsRequired();
        });

        modelBuilder.Entity<ProjectRecord>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Id);

            // The unique index is the guarantee; the pre-insert check in the
            // controller is only the error message. Two concurrent creates both pass
            // a read-then-insert check and only the database can refuse the second
            // (T-0004 AC1c).
            entity.HasIndex(e => e.Key).IsUnique();

            entity.Property(e => e.Key).HasMaxLength(10);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
