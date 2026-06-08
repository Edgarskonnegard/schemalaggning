using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Services.Interfaces;
using backend.Tests.Infrastructure;

namespace backend.Tests;

public class SchedulesEndpointTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public SchedulesEndpointTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Admin_cannot_publish_schedule_that_overlaps_published_schedule()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync();
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.AdminEmail);

        var response = await client.PutAsync($"/api/schedules/{seed.OverlappingDraftScheduleId}/publish", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var draft = context.Schedules.Single(schedule => schedule.Id == seed.OverlappingDraftScheduleId);
        Assert.Equal("Draft", draft.Status);
    }

    private async Task<SeedResult> SeedScenarioAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var suffix = Guid.NewGuid().ToString("N");
        var store = new Store { Name = $"Testbutik {suffix}" };
        var role = new Role { Name = $"Butiksmedarbetare {suffix}" };
        var shiftType = new ShiftType
        {
            Name = $"Oppning {suffix}",
            DefaultStartTime = new TimeOnly(8, 0),
            DefaultEndTime = new TimeOnly(16, 0)
        };
        var employee = new Employee
        {
            Name = "Anna Schema",
            Store = store,
            Role = role,
            EmploymentPercentage = 100
        };
        var publishedSchedule = new Schedule
        {
            Name = "Publicerat testschema",
            Store = store,
            PeriodStart = new DateOnly(2026, 7, 1),
            PeriodEnd = new DateOnly(2026, 7, 7),
            Status = "Published"
        };
        var draftSchedule = new Schedule
        {
            Name = "Överlappande utkast",
            Store = store,
            PeriodStart = new DateOnly(2026, 7, 4),
            PeriodEnd = new DateOnly(2026, 7, 10),
            Status = "Draft"
        };

        const string adminEmail = "admin.schedule@example.local";
        context.AddRange(
            store,
            role,
            shiftType,
            employee,
            new UserAccount
            {
                Email = adminEmail,
                PasswordHash = passwordHasher.Hash(AuthTestHelper.Password),
                AccessRole = "Admin",
                Store = store,
                IsActive = true
            },
            publishedSchedule,
            draftSchedule,
            new Shift
            {
                Schedule = publishedSchedule,
                Employee = employee,
                ShiftType = shiftType,
                Date = new DateOnly(2026, 7, 1),
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0),
                Source = "Test",
                Status = "Published"
            },
            new Shift
            {
                Schedule = draftSchedule,
                Employee = employee,
                ShiftType = shiftType,
                Date = new DateOnly(2026, 7, 4),
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(16, 0),
                Source = "Test",
                Status = "Draft"
            });

        await context.SaveChangesAsync();

        return new SeedResult(adminEmail, draftSchedule.Id);
    }

    private sealed record SeedResult(string AdminEmail, int OverlappingDraftScheduleId);
}
