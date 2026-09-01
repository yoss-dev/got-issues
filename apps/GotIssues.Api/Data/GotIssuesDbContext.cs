using Microsoft.EntityFrameworkCore;

namespace GotIssues.Api.Data;

public sealed class GotIssuesDbContext(DbContextOptions<GotIssuesDbContext> options)
    : DbContext(options)
{
    public DbSet<ProjectRecord> Projects => Set<ProjectRecord>();

    public DbSet<IssueRecord> Issues => Set<IssueRecord>();

    public DbSet<UserRecord> Users => Set<UserRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<IssueRecord>(entity =>
        {
            entity.ToTable("issues");
            entity.HasKey(e => e.Id);

            // The guarantee, not the mechanism. The project's counter is what
            // allocates a number; this is what makes a duplicate impossible even if
            // the allocator is ever replaced by something that looks equivalent and
            // is not. T-0004 paid for this distinction: a read-then-insert check
            // passes every sequential test and fails only under concurrency.
            entity.HasIndex(e => new { e.ProjectId, e.Number }).IsUnique();

            entity.Property(e => e.Title).HasMaxLength(200);

            // Stored as their names rather than ordinals: a column reading `InProgress`
            // survives someone reordering the enum, and an integer does not. The
            // default lives in the **database**, not only in the CLR initialiser —
            // T-0005 shipped a migration that backfilled existing rows with a value the
            // contract forbade precisely because that distinction was missed.
            //
            // The sentinel is stated rather than inferred. A database default means EF
            // must decide whether a given CLR value counts as "unset", and its guess is
            // `default(T)` — 0 here, which is not a declared member of any of these
            // enums. That guess is correct *because* every member starts at 1, and EF
            // logs Model.Validation[20601] on every start to say it is guessing.
            //
            // Stating it silences three warnings per process — and, on its own, that is
            // all it does. It does **not** make a future `= 0` member fail loudly; it
            // removes the only standing signal that anything was being assumed. The
            // guarantee lives in `IssueLifecycleEnumTests`, which fails if any member of
            // these three enums is ever zero. Review caught the first version of this
            // comment claiming the mechanism did what the test does.
            entity.Property(e => e.Type)
                .HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(IssueType.Task).HasSentinel(default);
            entity.Property(e => e.Status)
                .HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(IssueStatus.Open).HasSentinel(default);
            entity.Property(e => e.Priority)
                .HasConversion<string>().HasMaxLength(20)
                .HasDefaultValue(IssuePriority.Normal).HasSentinel(default);

            entity.Property(e => e.AssigneeSubject).HasMaxLength(255);

            // Restrict, not Cascade: deleting a person must never delete the work they
            // were holding. Nothing deletes users yet, so this is a decision made now
            // rather than discovered by whoever adds that.
            entity.HasOne(e => e.Assignee)
                .WithMany()
                .HasForeignKey(e => e.AssigneeSubject)
                .HasPrincipalKey(u => u.Subject)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(e => e.Description).HasMaxLength(10000);
            entity.Property(e => e.CreatedAt).IsRequired();

            // An issue cannot outlive its project, but nothing deletes projects yet;
            // Restrict rather than Cascade so that when deletion arrives it is a
            // decision someone makes rather than one this line already made.
            entity.HasOne(e => e.Project)
                .WithMany()
                .HasForeignKey(e => e.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);
        });

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

            // Default 1, in the database and not only in the CLR initialiser. Without
            // it the migration backfills existing projects with 0, and the first issue
            // in any project created before this migration gets number 0 — a key like
            // GOTI-0, which violates the pattern and the `minimum: 1` the contract
            // itself declares, and which the read path then rejects with 400. Every
            // test migrates an empty schema, so nothing saw it.
            entity.Property(e => e.NextIssueNumber).HasDefaultValue(1);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}
