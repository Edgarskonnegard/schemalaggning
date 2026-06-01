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
    public DbSet<EmployeeShiftType> EmployeeShiftTypes => Set<EmployeeShiftType>();
    public DbSet<BaseScheduleRule> BaseScheduleRules => Set<BaseScheduleRule>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<Shift> Shifts => Set<Shift>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<EmployeeShiftType>()
            .HasKey(est => new { est.EmployeeId, est.ShiftTypeId });

        modelBuilder.Entity<EmployeeShiftType>()
            .HasOne(est => est.Employee)
            .WithMany(e => e.EmployeeShiftTypes)
            .HasForeignKey(est => est.EmployeeId);

        modelBuilder.Entity<EmployeeShiftType>()
            .HasOne(est => est.ShiftType)
            .WithMany(st => st.EmployeeShiftTypes)
            .HasForeignKey(est => est.ShiftTypeId);

        modelBuilder.Entity<BaseScheduleRule>()
            .HasIndex(r => new { r.EmployeeId, r.DayOfWeek })
            .IsUnique();

        modelBuilder.Entity<Role>()
            .HasIndex(r => r.Name)
            .IsUnique();

        modelBuilder.Entity<ShiftType>()
            .HasIndex(st => st.Name)
            .IsUnique();

        modelBuilder.Entity<Employee>()
            .Property(e => e.EmploymentPercentage)
            .HasColumnType("decimal(5,2)");
    }
}