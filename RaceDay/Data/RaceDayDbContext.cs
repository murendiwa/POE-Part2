using Microsoft.EntityFrameworkCore;
using RaceDay.Models;

namespace RaceDay.Data;

public class RaceDayDbContext : DbContext
{
    public RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<RaceRoute> Routes => Set<RaceRoute>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();

            entity.Property(u => u.Role)
                  .HasConversion<string>().HasMaxLength(20)
                  .HasDefaultValue(UserRole.Participant);

            entity.Property(u => u.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Users_Role", "[Role] IN ('Organiser','Participant')"));
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.Property(e => e.Status)
                  .HasConversion<string>().HasMaxLength(20)
                  .HasDefaultValue(EventStatus.Scheduled);

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Organiser)
                  .WithMany(u => u.OrganisedEvents)
                  .HasForeignKey(e => e.OrganiserId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Events_Status", "[Status] IN ('Scheduled','Cancelled','Completed')"));
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasIndex(c => new { c.EventId, c.Name }).IsUnique();
            entity.Property(c => c.MaxParticipants).HasDefaultValue(100);
            entity.Property(c => c.EntryFee).HasDefaultValue(0m);

            entity.HasOne(c => c.Event)
                  .WithMany(e => e.Categories)
                  .HasForeignKey(c => c.EventId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RaceRoute>(entity =>
        {
            entity.ToTable("Routes"); // class renamed to avoid clashing with RouteAttribute; table name unchanged
            entity.HasIndex(r => r.CategoryId).IsUnique(); // enforces the 1-to-1

            entity.HasOne(r => r.Category)
                  .WithOne(c => c.Route)
                  .HasForeignKey<RaceRoute>(r => r.CategoryId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.HasIndex(e => new { e.ParticipantId, e.CategoryId }).IsUnique();

            entity.Property(e => e.Status)
                  .HasConversion<string>().HasMaxLength(20)
                  .HasDefaultValue(EnrolmentStatus.Registered);

            entity.Property(e => e.PaymentStatus)
                  .HasConversion<string>().HasMaxLength(20)
                  .HasDefaultValue(PaymentStatus.Pending);

            entity.Property(e => e.EnrolmentDate).HasDefaultValueSql("GETDATE()");

            entity.HasOne(e => e.Participant)
                  .WithMany(u => u.Enrolments)
                  .HasForeignKey(e => e.ParticipantId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Category)
                  .WithMany(c => c.Enrolments)
                  .HasForeignKey(e => e.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Enrolments_Status", "[Status] IN ('Registered','Cancelled','Confirmed')");
                t.HasCheckConstraint("CK_Enrolments_PaymentStatus", "[PaymentStatus] IN ('Pending','Paid','Refunded')");
            });
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.HasIndex(r => r.EnrolmentId).IsUnique(); // enforces the 1-to-1

            entity.Property(r => r.Status)
                  .HasConversion<string>().HasMaxLength(20)
                  .HasDefaultValue(ResultStatus.Finished);

            entity.Property(r => r.StartTime).HasDefaultValueSql("GETDATE()");

            entity.HasOne(r => r.Enrolment)
                  .WithOne(e => e.Result)
                  .HasForeignKey<Result>(r => r.EnrolmentId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.ToTable(t => t.HasCheckConstraint(
                "CK_Results_Status", "[Status] IN ('Finished','DNF','DNS')"));
        });
    }
}