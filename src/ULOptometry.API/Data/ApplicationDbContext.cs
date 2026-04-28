using Microsoft.EntityFrameworkCore;
using ULOptometry.Domain.Entities;

namespace ULOptometry.API.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<PatientInfo> PatientInfos => Set<PatientInfo>();
    public DbSet<ClinicSession> ClinicSessions => Set<ClinicSession>();
    public DbSet<Cubicle> Cubicles => Set<Cubicle>();
    public DbSet<CubicleAssignment> CubicleAssignments => Set<CubicleAssignment>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<PoERecord> PoERecords => Set<PoERecord>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Booking>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Encounter>().HasQueryFilter(e => !e.IsDeleted);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();

        modelBuilder.Entity<CubicleAssignment>()
            .HasOne(ca => ca.Student)
            .WithMany()
            .HasForeignKey(ca => ca.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CubicleAssignment>()
            .HasOne(ca => ca.Supervisor)
            .WithMany()
            .HasForeignKey(ca => ca.SupervisorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Booking>()
            .HasOne(b => b.AcceptedByStudent)
            .WithMany()
            .HasForeignKey(b => b.AcceptedByStudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Encounter>()
            .HasOne(e => e.Student)
            .WithMany()
            .HasForeignKey(e => e.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Encounter>()
            .HasOne(e => e.Supervisor)
            .WithMany()
            .HasForeignKey(e => e.SupervisorId)
            .OnDelete(DeleteBehavior.Restrict);

        for (int i = 1; i <= 8; i++)
        {
            modelBuilder.Entity<Cubicle>().HasData(new Cubicle
            {
                Id = i,
                CubicleNumber = i,
                Description = $"Cubicle {i}",
                IsActive = true,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            });
        }
    }
}
