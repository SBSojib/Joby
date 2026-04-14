using Joby.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Joby.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<ResumeDocument> ResumeDocuments => Set<ResumeDocument>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Domain.Entities.Application> Applications => Set<Domain.Entities.Application>();
    public DbSet<ApplicationEvent> ApplicationEvents => Set<ApplicationEvent>();
    public DbSet<Reminder> Reminders => Set<Reminder>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(256).IsRequired();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EmailVerificationCodeHash).HasMaxLength(200);

            entity.HasOne(e => e.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<Profile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // RefreshToken configuration
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(500).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Profile configuration
        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.ActiveResume)
                .WithMany()
                .HasForeignKey(e => e.ActiveResumeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ResumeDocument configuration
        modelBuilder.Entity<ResumeDocument>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Resumes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Job configuration
        modelBuilder.Entity<Job>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Company).HasMaxLength(500).IsRequired();
            entity.HasIndex(e => new { e.UserId, e.SourceUrl });

            entity.HasOne(e => e.User)
                .WithMany(u => u.Jobs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Application configuration
        modelBuilder.Entity<Domain.Entities.Application>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.JobId }).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Applications)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Job)
                .WithOne(j => j.Application)
                .HasForeignKey<Domain.Entities.Application>(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Resume)
                .WithMany()
                .HasForeignKey(e => e.ResumeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ApplicationEvent configuration
        modelBuilder.Entity<ApplicationEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Events)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Reminder configuration
        modelBuilder.Entity<Reminder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(500).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Reminders)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Application)
                .WithMany(a => a.Reminders)
                .HasForeignKey(e => e.ApplicationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Job)
                .WithMany(j => j.Reminders)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Recommendation configuration
        modelBuilder.Entity<Recommendation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.JobId }).IsUnique();
            entity.HasIndex(e => new { e.UserId, e.Score, e.IsActive });

            entity.HasOne(e => e.User)
                .WithMany(u => u.Recommendations)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Job)
                .WithMany(j => j.Recommendations)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}





