using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Services.Interfaces;
using backend.Tests.Infrastructure;

namespace backend.Tests;

public class BaseScheduleGenerationServiceTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public BaseScheduleGenerationServiceTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Generate_creates_unassigned_need_when_no_employee_can_work_shift_type()
    {
        await _factory.ResetDatabaseAsync();
        var storeId = await SeedCoverageScenarioAsync(allowEmployeeToWorkShiftType: false);

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBaseScheduleGenerationService>();

        var result = await service.GenerateForStoreAsync(storeId);

        Assert.Equal(0, result.CreatedRuleCount);
        Assert.Equal(4, result.UnassignedNeedCount);
        Assert.NotEmpty(result.Warnings);

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(4, context.BaseScheduleUnassignedDraftRules.Count());
    }

    [Fact]
    public async Task Generate_assigns_need_when_employee_role_can_work_shift_type()
    {
        await _factory.ResetDatabaseAsync();
        var storeId = await SeedCoverageScenarioAsync(allowEmployeeToWorkShiftType: true);

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBaseScheduleGenerationService>();

        var result = await service.GenerateForStoreAsync(storeId);

        Assert.Equal(4, result.CreatedRuleCount);
        Assert.Equal(0, result.UnassignedNeedCount);

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(4, context.BaseScheduleDraftRules.Count());
    }

    [Fact]
    public async Task Generate_respects_max_consecutive_work_days_setting()
    {
        await _factory.ResetDatabaseAsync();
        var storeId = await SeedConsecutiveDaysScenarioAsync();

        using var scope = _factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IBaseScheduleGenerationService>();

        var result = await service.GenerateForStoreAsync(storeId);

        Assert.Equal(4, result.CreatedRuleCount);
        Assert.Equal(4, result.UnassignedNeedCount);

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var draftRules = context.BaseScheduleDraftRules.AsNoTracking().ToList();
        Assert.All(draftRules, rule => Assert.Equal(DayOfWeek.Monday, rule.DayOfWeek));
    }

    private async Task<int> SeedCoverageScenarioAsync(bool allowEmployeeToWorkShiftType)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString("N");
        var store = new Store { Name = $"Testbutik {suffix}" };
        var role = new Role { Name = $"Roll {suffix}" };
        var shiftType = new ShiftType
        {
            Name = $"Pass {suffix}",
            DefaultStartTime = new TimeOnly(8, 0),
            DefaultEndTime = new TimeOnly(16, 0)
        };

        if (allowEmployeeToWorkShiftType)
        {
            role.RoleShiftTypes.Add(new RoleShiftType
            {
                Role = role,
                ShiftType = shiftType
            });
        }

        var employee = new Employee
        {
            Name = "Anna Grundschema",
            Store = store,
            Role = role,
            EmploymentPercentage = 100
        };
        var coverageRule = new StoreCoverageRule
        {
            Store = store,
            ShiftType = shiftType,
            DayOfWeek = DayOfWeek.Monday,
            RequiredCount = 1,
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0)
        };

        context.AddRange(store, role, shiftType, employee, coverageRule);
        await context.SaveChangesAsync();

        return store.Id;
    }

    private async Task<int> SeedConsecutiveDaysScenarioAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var suffix = Guid.NewGuid().ToString("N");
        var store = new Store { Name = $"Testbutik {suffix}" };
        var role = new Role { Name = $"Roll {suffix}" };
        var shiftType = new ShiftType
        {
            Name = $"Pass {suffix}",
            DefaultStartTime = new TimeOnly(8, 0),
            DefaultEndTime = new TimeOnly(16, 0)
        };
        role.RoleShiftTypes.Add(new RoleShiftType
        {
            Role = role,
            ShiftType = shiftType
        });

        var employee = new Employee
        {
            Name = "Anna Grundschema",
            Store = store,
            Role = role,
            EmploymentPercentage = 100
        };

        context.AddRange(store, role, shiftType, employee);

        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday })
        {
            context.StoreCoverageRules.Add(new StoreCoverageRule
            {
                Store = store,
                ShiftType = shiftType,
                DayOfWeek = day,
                RequiredCount = 1,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0)
            });
        }

        context.ScheduleGenerationSettings.Add(new ScheduleGenerationSettings
        {
            Store = store,
            MinimumRestHours = 11,
            MaxConsecutiveWorkDays = 1,
            BalanceWeekends = false
        });

        await context.SaveChangesAsync();

        return store.Id;
    }
}
