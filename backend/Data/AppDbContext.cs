using Microsoft.EntityFrameworkCore;
using Schemalaggning.Models;

namespace Schemalaggning.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();
    public DbSet<StoreCoverageRule> StoreCoverageRules => Set<StoreCoverageRule>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<ShiftType> ShiftTypes => Set<ShiftType>();
    public DbSet<RoleShiftType> RoleShiftTypes => Set<RoleShiftType>();
    public DbSet<BaseScheduleRule> BaseScheduleRules => Set<BaseScheduleRule>();
    public DbSet<BaseScheduleGenerationBatch> BaseScheduleGenerationBatches => Set<BaseScheduleGenerationBatch>();
    public DbSet<BaseScheduleDraftRule> BaseScheduleDraftRules => Set<BaseScheduleDraftRule>();
    public DbSet<BaseScheduleEmployeeApproval> BaseScheduleEmployeeApprovals => Set<BaseScheduleEmployeeApproval>();
    public DbSet<BaseScheduleUnassignedDraftRule> BaseScheduleUnassignedDraftRules => Set<BaseScheduleUnassignedDraftRule>();
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

        modelBuilder.Entity<Store>()
            .Property(s => s.Name)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Store>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<Store>()
            .HasMany(s => s.Employees)
            .WithOne(e => e.Store)
            .HasForeignKey(e => e.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Store>()
            .HasMany(s => s.UserAccounts)
            .WithOne(ua => ua.Store)
            .HasForeignKey(ua => ua.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Store>()
            .HasMany(s => s.CoverageRules)
            .WithOne(r => r.Store)
            .HasForeignKey(r => r.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<UserAccount>()
            .Property(ua => ua.Email)
            .HasMaxLength(255)
            .IsRequired();

        modelBuilder.Entity<UserAccount>()
            .HasIndex(ua => ua.Email)
            .IsUnique();

        modelBuilder.Entity<UserAccount>()
            .Property(ua => ua.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        modelBuilder.Entity<UserAccount>()
            .Property(ua => ua.AccessRole)
            .HasMaxLength(30)
            .IsRequired();

        modelBuilder.Entity<UserAccount>()
            .HasOne(ua => ua.Employee)
            .WithOne(e => e.UserAccount)
            .HasForeignKey<UserAccount>(ua => ua.EmployeeId)
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

        modelBuilder.Entity<RoleShiftType>()
            .HasKey(rst => new { rst.RoleId, rst.ShiftTypeId });

        modelBuilder.Entity<RoleShiftType>()
            .HasOne(rst => rst.Role)
            .WithMany(r => r.RoleShiftTypes)
            .HasForeignKey(rst => rst.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RoleShiftType>()
            .HasOne(rst => rst.ShiftType)
            .WithMany(st => st.RoleShiftTypes)
            .HasForeignKey(rst => rst.ShiftTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShiftType>()
            .HasMany(st => st.BaseScheduleRules)
            .WithOne(r => r.ShiftType)
            .HasForeignKey(r => r.ShiftTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ShiftType>()
            .HasMany(st => st.StoreCoverageRules)
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

        modelBuilder.Entity<BaseScheduleGenerationBatch>()
            .Property(batch => batch.Status)
            .HasMaxLength(30)
            .IsRequired();

        modelBuilder.Entity<BaseScheduleGenerationBatch>()
            .HasMany(batch => batch.DraftRules)
            .WithOne(rule => rule.Batch)
            .HasForeignKey(rule => rule.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BaseScheduleGenerationBatch>()
            .HasMany(batch => batch.EmployeeApprovals)
            .WithOne(approval => approval.Batch)
            .HasForeignKey(approval => approval.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BaseScheduleGenerationBatch>()
            .HasMany(batch => batch.UnassignedDraftRules)
            .WithOne(rule => rule.Batch)
            .HasForeignKey(rule => rule.BatchId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BaseScheduleGenerationBatch>()
            .HasOne(batch => batch.Store)
            .WithMany()
            .HasForeignKey(batch => batch.StoreId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BaseScheduleDraftRule>()
            .HasOne(rule => rule.Employee)
            .WithMany()
            .HasForeignKey(rule => rule.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BaseScheduleDraftRule>()
            .HasOne(rule => rule.ShiftType)
            .WithMany()
            .HasForeignKey(rule => rule.ShiftTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BaseScheduleEmployeeApproval>()
            .Property(approval => approval.Status)
            .HasMaxLength(30)
            .IsRequired();

        modelBuilder.Entity<BaseScheduleEmployeeApproval>()
            .HasOne(approval => approval.Employee)
            .WithMany()
            .HasForeignKey(approval => approval.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BaseScheduleEmployeeApproval>()
            .HasIndex(approval => new { approval.BatchId, approval.EmployeeId })
            .IsUnique();

        modelBuilder.Entity<BaseScheduleUnassignedDraftRule>()
            .HasOne(rule => rule.ShiftType)
            .WithMany()
            .HasForeignKey(rule => rule.ShiftTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<StoreCoverageRule>()
            .HasIndex(r => new { r.StoreId, r.DayOfWeek, r.ShiftTypeId })
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
            .HasOne(s => s.Store)
            .WithMany()
            .HasForeignKey(s => s.StoreId)
            .OnDelete(DeleteBehavior.Restrict);

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
