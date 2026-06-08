using Microsoft.Extensions.DependencyInjection;
using Schemalaggning.Data;
using Schemalaggning.DTOs.Schedules;
using Schemalaggning.Models;
using Schemalaggning.Services.Interfaces;
using backend.Tests.Infrastructure;

namespace backend.Tests;

public class ScheduleServiceTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ScheduleServiceTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Publish_sets_schedule_and_shifts_to_published()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScheduleAsync();

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IScheduleService>();

        var published = await service.PublishScheduleAsync(seed.DraftScheduleId);

        Assert.True(published);

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var schedule = context.Schedules.Single(item => item.Id == seed.DraftScheduleId);
        var shifts = context.Shifts.Where(shift => shift.ScheduleId == seed.DraftScheduleId).ToList();
        Assert.Equal("Published", schedule.Status);
        Assert.All(shifts, shift => Assert.Equal("Published", shift.Status));
    }

    [Fact]
    public async Task SwapShiftEmployees_rejects_shifts_on_different_dates()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScheduleAsync(includeSecondShiftDifferentDate: true);

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IScheduleService>();

        var exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SwapShiftEmployeesAsync(
                seed.FirstShiftId,
                new ShiftSwapDto { TargetShiftId = seed.SecondShiftId }));

        Assert.Contains("same date", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateShift_rejects_employee_without_shift_type_role()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScheduleAsync(includeEmployeeWithoutShiftType: true);

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IScheduleService>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpdateShiftAsync(
                seed.FirstShiftId,
                new ShiftUpdateDto
                {
                    EmployeeId = seed.EmployeeWithoutShiftTypeId,
                    ShiftTypeId = seed.ShiftTypeId,
                    Date = new DateOnly(2026, 7, 1),
                    StartTime = new TimeOnly(8, 0),
                    EndTime = new TimeOnly(16, 0)
                }));

        Assert.Contains("role", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<SeedResult> SeedScheduleAsync(
        bool includeSecondShiftDifferentDate = false,
        bool includeEmployeeWithoutShiftType = false)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString("N");
        var store = new Store { Name = $"Testbutik {suffix}" };
        var allowedRole = new Role { Name = $"Tillaten roll {suffix}" };
        var blockedRole = new Role { Name = $"Blockerad roll {suffix}" };
        var shiftType = new ShiftType
        {
            Name = $"Oppning {suffix}",
            DefaultStartTime = new TimeOnly(8, 0),
            DefaultEndTime = new TimeOnly(16, 0)
        };
        allowedRole.RoleShiftTypes.Add(new RoleShiftType
        {
            Role = allowedRole,
            ShiftType = shiftType
        });

        var firstEmployee = new Employee
        {
            Name = "Anna Service",
            Store = store,
            Role = allowedRole,
            EmploymentPercentage = 100
        };
        var secondEmployee = new Employee
        {
            Name = "Bertil Service",
            Store = store,
            Role = allowedRole,
            EmploymentPercentage = 100
        };
        var blockedEmployee = new Employee
        {
            Name = "Cecilia Service",
            Store = store,
            Role = blockedRole,
            EmploymentPercentage = 100
        };
        var schedule = new Schedule
        {
            Name = "Utkast",
            Store = store,
            PeriodStart = new DateOnly(2026, 7, 1),
            PeriodEnd = new DateOnly(2026, 7, 7),
            Status = "Draft"
        };
        var firstShift = new Shift
        {
            Schedule = schedule,
            Employee = firstEmployee,
            ShiftType = shiftType,
            Date = new DateOnly(2026, 7, 1),
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0),
            Source = "Test",
            Status = "Draft"
        };
        var secondShift = new Shift
        {
            Schedule = schedule,
            Employee = secondEmployee,
            ShiftType = shiftType,
            Date = includeSecondShiftDifferentDate
                ? new DateOnly(2026, 7, 2)
                : new DateOnly(2026, 7, 1),
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(20, 0),
            Source = "Test",
            Status = "Draft"
        };

        context.AddRange(
            store,
            allowedRole,
            blockedRole,
            shiftType,
            firstEmployee,
            secondEmployee,
            schedule,
            firstShift,
            secondShift);

        if (includeEmployeeWithoutShiftType)
        {
            context.Employees.Add(blockedEmployee);
        }

        await context.SaveChangesAsync();

        return new SeedResult(
            schedule.Id,
            firstShift.Id,
            secondShift.Id,
            shiftType.Id,
            blockedEmployee.Id);
    }

    private sealed record SeedResult(
        int DraftScheduleId,
        int FirstShiftId,
        int SecondShiftId,
        int ShiftTypeId,
        int EmployeeWithoutShiftTypeId);
}
