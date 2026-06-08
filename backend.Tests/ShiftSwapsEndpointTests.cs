using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Services.Interfaces;
using backend.Tests.Infrastructure;

namespace backend.Tests;

public class ShiftSwapsEndpointTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ShiftSwapsEndpointTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Receiver_can_approve_shift_swap_and_shift_moves_to_receiver()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync(includeBusyShiftForReceiver: false);
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.FromEmployeeEmail);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/me/shifts/{seed.ShiftId}/swap-requests",
            new { toEmployeeId = seed.ToEmployeeId, message = "Kan du ta mitt pass?" });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        int requestId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            requestId = context.ShiftSwapRequests.Single().Id;
        }

        await AuthTestHelper.AuthenticateAsync(client, seed.ToEmployeeEmail);

        var approveResponse = await client.PostAsync($"/api/me/shift-swaps/{requestId}/approve", null);

        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var shift = verifyContext.Shifts.Single(current => current.Id == seed.ShiftId);
        var request = verifyContext.ShiftSwapRequests.Single();
        Assert.Equal(seed.ToEmployeeId, shift.EmployeeId);
        Assert.Equal("Approved", request.Status);
    }

    [Fact]
    public async Task Employee_cannot_create_shift_swap_to_busy_employee()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync(includeBusyShiftForReceiver: true);
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.FromEmployeeEmail);

        var response = await client.PostAsJsonAsync(
            $"/api/me/shifts/{seed.ShiftId}/swap-requests",
            new { toEmployeeId = seed.ToEmployeeId, message = "Kan du ta mitt pass?" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<SeedResult> SeedScenarioAsync(bool includeBusyShiftForReceiver)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var suffix = Guid.NewGuid().ToString("N");
        var store = new Store { Name = $"Testbutik {suffix}" };
        var role = new Role { Name = $"Butiksmedarbetare {suffix}" };
        var shiftType = new ShiftType
        {
            Name = $"Stangning {suffix}",
            DefaultStartTime = new TimeOnly(12, 0),
            DefaultEndTime = new TimeOnly(20, 0)
        };
        role.RoleShiftTypes.Add(new RoleShiftType
        {
            Role = role,
            ShiftType = shiftType
        });

        var fromEmployee = new Employee
        {
            Name = "Anna Byte",
            Store = store,
            Role = role,
            EmploymentPercentage = 100
        };
        var toEmployee = new Employee
        {
            Name = "Bertil Byte",
            Store = store,
            Role = role,
            EmploymentPercentage = 100
        };
        var schedule = new Schedule
        {
            Name = "Publicerat bytesschema",
            Store = store,
            PeriodStart = new DateOnly(2026, 7, 1),
            PeriodEnd = new DateOnly(2026, 7, 7),
            Status = "Published"
        };
        var sourceShift = new Shift
        {
            Schedule = schedule,
            Employee = fromEmployee,
            ShiftType = shiftType,
            Date = new DateOnly(2026, 7, 1),
            StartTime = new TimeOnly(12, 0),
            EndTime = new TimeOnly(20, 0),
            Source = "Test",
            Status = "Published"
        };

        const string fromEmployeeEmail = "anna.swap@example.local";
        const string toEmployeeEmail = "bertil.swap@example.local";
        context.AddRange(
            store,
            role,
            shiftType,
            fromEmployee,
            toEmployee,
            new UserAccount
            {
                Email = fromEmployeeEmail,
                PasswordHash = passwordHasher.Hash(AuthTestHelper.Password),
                AccessRole = "Employee",
                Employee = fromEmployee,
                Store = store,
                IsActive = true
            },
            new UserAccount
            {
                Email = toEmployeeEmail,
                PasswordHash = passwordHasher.Hash(AuthTestHelper.Password),
                AccessRole = "Employee",
                Employee = toEmployee,
                Store = store,
                IsActive = true
            },
            schedule,
            sourceShift);

        if (includeBusyShiftForReceiver)
        {
            context.Shifts.Add(new Shift
            {
                Schedule = schedule,
                Employee = toEmployee,
                ShiftType = shiftType,
                Date = sourceShift.Date,
                StartTime = new TimeOnly(8, 0),
                EndTime = new TimeOnly(11, 0),
                Source = "Test",
                Status = "Published"
            });
        }

        await context.SaveChangesAsync();

        return new SeedResult(
            fromEmployeeEmail,
            toEmployeeEmail,
            toEmployee.Id,
            sourceShift.Id);
    }

    private sealed record SeedResult(
        string FromEmployeeEmail,
        string ToEmployeeEmail,
        int ToEmployeeId,
        int ShiftId);
}
