using Microsoft.Extensions.DependencyInjection;
using Schemalaggning.Data;
using Schemalaggning.DTOs.Schedules;
using Schemalaggning.Models;
using Schemalaggning.Services.Interfaces;
using backend.Tests.Infrastructure;

namespace backend.Tests;

public class ScheduleGenerationServiceTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ScheduleGenerationServiceTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Generate_starts_after_existing_published_schedule()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedBaseScheduleAsync(existingPublishedEnd: new DateOnly(2026, 7, 7));
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IScheduleGenerationService>();

        var schedule = await service.GenerateFromBaseScheduleAsync(new ScheduleCreateDto
        {
            StoreId = seed.StoreId,
            PeriodStart = new DateOnly(2026, 7, 1),
            PeriodEnd = new DateOnly(2026, 7, 7)
        });

        Assert.Equal(new DateOnly(2026, 7, 8), schedule.PeriodStart);
        Assert.Equal(new DateOnly(2026, 7, 14), schedule.PeriodEnd);
        Assert.Contains(schedule.Shifts, shift => shift.Date == new DateOnly(2026, 7, 8));
    }

    [Fact]
    public async Task Generate_continues_four_week_cycle_from_first_published_schedule()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedBaseScheduleAsync(
            existingPublishedEnd: new DateOnly(2026, 7, 21),
            includeWeekFourRule: true);
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IScheduleGenerationService>();

        var schedule = await service.GenerateFromBaseScheduleAsync(new ScheduleCreateDto
        {
            StoreId = seed.StoreId,
            PeriodStart = new DateOnly(2026, 7, 1),
            PeriodEnd = new DateOnly(2026, 7, 7)
        });

        var firstShift = Assert.Single(schedule.Shifts);
        Assert.Equal(new DateOnly(2026, 7, 22), schedule.PeriodStart);
        Assert.Equal(new DateOnly(2026, 7, 22), firstShift.Date);
        Assert.Equal(seed.WeekFourShiftTypeId, firstShift.ShiftTypeId);
    }

    [Fact]
    public async Task Generate_skips_shift_when_employee_has_pending_leave()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedBaseScheduleAsync(
            existingPublishedEnd: null,
            includeMondayRule: true,
            leaveDate: new DateOnly(2026, 7, 6));
        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IScheduleGenerationService>();

        var schedule = await service.GenerateFromBaseScheduleAsync(new ScheduleCreateDto
        {
            StoreId = seed.StoreId,
            PeriodStart = new DateOnly(2026, 7, 6),
            PeriodEnd = new DateOnly(2026, 7, 6)
        });

        Assert.Empty(schedule.Shifts);
    }

    private async Task<SeedResult> SeedBaseScheduleAsync(
        DateOnly? existingPublishedEnd,
        bool includeWeekFourRule = false,
        bool includeMondayRule = false,
        DateOnly? leaveDate = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString("N");
        var store = new Store { Name = $"Testbutik {suffix}" };
        var role = new Role { Name = $"Butiksmedarbetare {suffix}" };
        var weekTwoShiftType = new ShiftType
        {
            Name = $"Vecka tva {suffix}",
            DefaultStartTime = new TimeOnly(8, 0),
            DefaultEndTime = new TimeOnly(16, 0)
        };
        var weekFourShiftType = new ShiftType
        {
            Name = $"Vecka fyra {suffix}",
            DefaultStartTime = new TimeOnly(10, 0),
            DefaultEndTime = new TimeOnly(18, 0)
        };
        var mondayShiftType = new ShiftType
        {
            Name = $"Mandag {suffix}",
            DefaultStartTime = new TimeOnly(8, 0),
            DefaultEndTime = new TimeOnly(16, 0)
        };
        var employee = new Employee
        {
            Name = "Anna Generator",
            Store = store,
            Role = role,
            EmploymentPercentage = 100
        };

        context.AddRange(store, role, weekTwoShiftType, weekFourShiftType, mondayShiftType, employee);
        await context.SaveChangesAsync();

        context.BaseScheduleRules.Add(new BaseScheduleRule
        {
            EmployeeId = employee.Id,
            ShiftTypeId = weekTwoShiftType.Id,
            WeekInCycle = 2,
            DayOfWeek = DayOfWeek.Wednesday,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0)
        });

        if (includeWeekFourRule)
        {
            context.BaseScheduleRules.Add(new BaseScheduleRule
            {
                EmployeeId = employee.Id,
                ShiftTypeId = weekFourShiftType.Id,
                WeekInCycle = 4,
                DayOfWeek = DayOfWeek.Wednesday,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(18, 0)
            });
        }

        if (includeMondayRule)
        {
            context.BaseScheduleRules.Add(new BaseScheduleRule
            {
                EmployeeId = employee.Id,
                ShiftTypeId = mondayShiftType.Id,
                WeekInCycle = 1,
                DayOfWeek = DayOfWeek.Monday,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0)
            });
        }

        if (existingPublishedEnd is not null)
        {
            context.Schedules.Add(new Schedule
            {
                Name = "Publicerat schema",
                StoreId = store.Id,
                PeriodStart = new DateOnly(2026, 7, 1),
                PeriodEnd = existingPublishedEnd.Value,
                Status = "Published"
            });
        }

        if (leaveDate is not null)
        {
            context.LeaveRequests.Add(new LeaveRequest
            {
                EmployeeId = employee.Id,
                StartDate = leaveDate.Value,
                EndDate = leaveDate.Value,
                RequestedDays = 1,
                Reason = "Testledighet",
                Status = "Pending",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await context.SaveChangesAsync();

        return new SeedResult(store.Id, weekFourShiftType.Id);
    }

    private sealed record SeedResult(int StoreId, int WeekFourShiftTypeId);
}
