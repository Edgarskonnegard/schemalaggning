using Microsoft.EntityFrameworkCore;
using Schemalaggning.Models;

namespace Schemalaggning.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ShiftType> ShiftTypes => Set<ShiftType>();
    public DbSet<BaseScheduleRule> BaseScheduleRules => Set<BaseScheduleRule>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Role>()
            .Property(r => r.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasMany(r => r.Employees)
            .WithOne(e => e.Role)
            .HasForeignKey(e => e.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Role>()
            .HasMany(r => r.ShiftTypes)
            .WithOne(st => st.Role)
            .HasForeignKey(st => st.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Employee>()
            .Property(e => e.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Employee>()
            .Property(e => e.EmploymentPercentage)
            .HasColumnType("decimal(5,2)");

        modelBuilder.Entity<Employee>()
            .HasMany(e => e.BaseScheduleRules)
            .WithOne(r => r.Employee)
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Employee>()
            .HasMany(e => e.Shifts)
            .WithOne(s => s.Employee)
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ShiftType>()
            .Property(st => st.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<ShiftType>()
            .HasIndex(st => st.Name)
            .IsUnique();

        modelBuilder.Entity<ShiftType>()
            .HasMany(st => st.BaseScheduleRules)
            .WithOne(r => r.ShiftType)
            .HasForeignKey(r => r.ShiftTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ShiftType>()
            .HasMany(st => st.Shifts)
            .WithOne(s => s.ShiftType)
            .HasForeignKey(s => s.ShiftTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BaseScheduleRule>()
            .HasIndex(r => new { r.EmployeeId, r.WeekInCycle, r.DayOfWeek })
            .IsUnique();

        modelBuilder.Entity<Schedule>()
            .Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Schedule>()
            .Property(s => s.Status)
            .HasMaxLength(30)
            .IsRequired();

        modelBuilder.Entity<Schedule>()
            .HasMany(s => s.Shifts)
            .WithOne(s => s.Schedule)
            .HasForeignKey(s => s.ScheduleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Shift>()
            .Property(s => s.Source)
            .HasMaxLength(50)
            .IsRequired();

        modelBuilder.Entity<Shift>()
            .Property(s => s.Status)
            .HasMaxLength(30)
            .IsRequired();
    }
}
