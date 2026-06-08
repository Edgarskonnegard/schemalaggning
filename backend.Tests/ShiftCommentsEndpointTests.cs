using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Services.Interfaces;
using backend.Tests.Infrastructure;

namespace backend.Tests;

public class ShiftCommentsEndpointTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public ShiftCommentsEndpointTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Employee_can_comment_own_published_shift()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync();
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.FirstEmployeeEmail);

        var response = await client.PostAsJsonAsync(
            $"/api/me/shifts/{seed.FirstEmployeeShiftId}/comments",
            new { message = "Jag behöver dubbelkolla den här tiden." });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(context.ShiftComments);
    }

    [Fact]
    public async Task Employee_cannot_comment_another_employees_shift()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync();
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.FirstEmployeeEmail);

        var response = await client.PostAsJsonAsync(
            $"/api/me/shifts/{seed.SecondEmployeeShiftId}/comments",
            new { message = "Den här borde jag inte kunna kommentera." });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Employee_cannot_create_empty_comment()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync();
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.FirstEmployeeEmail);

        var response = await client.PostAsJsonAsync(
            $"/api/me/shifts/{seed.FirstEmployeeShiftId}/comments",
            new { message = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Employee_cannot_comment_shift_in_draft_schedule()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync();
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.FirstEmployeeEmail);

        var response = await client.PostAsJsonAsync(
            $"/api/me/shifts/{seed.DraftShiftId}/comments",
            new { message = "Det här passet är inte publicerat ännu." });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_see_pending_shift_comments()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync();
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.FirstEmployeeEmail);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/me/shifts/{seed.FirstEmployeeShiftId}/comments",
            new { message = "Kan admin se den här kommentaren?" });
        createResponse.EnsureSuccessStatusCode();

        await AuthTestHelper.AuthenticateAsync(client, seed.AdminEmail);

        var response = await client.GetAsync("/api/admin/shift-comments/pending-count");
        var count = await response.Content.ReadFromJsonAsync<int>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Admin_can_resolve_pending_shift_comment()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync();
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.FirstEmployeeEmail);

        var createResponse = await client.PostAsJsonAsync(
            $"/api/me/shifts/{seed.FirstEmployeeShiftId}/comments",
            new { message = "Markera mig som hanterad." });
        createResponse.EnsureSuccessStatusCode();

        int commentId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            commentId = context.ShiftComments.Single().Id;
        }

        await AuthTestHelper.AuthenticateAsync(client, seed.AdminEmail);

        var response = await client.PostAsync($"/api/admin/shift-comments/{commentId}/resolve", null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal("Resolved", verifyContext.ShiftComments.Single().Status);
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

        var firstEmployee = new Employee
        {
            Name = "Anna Test",
            Store = store,
            Role = role,
            EmploymentPercentage = 100
        };

        var secondEmployee = new Employee
        {
            Name = "Bertil Test",
            Store = store,
            Role = role,
            EmploymentPercentage = 100
        };

        const string firstEmployeeEmail = "anna.test@example.local";
        const string secondEmployeeEmail = "bertil.test@example.local";

        var firstAccount = new UserAccount
        {
            Email = firstEmployeeEmail,
            PasswordHash = passwordHasher.Hash(AuthTestHelper.Password),
            AccessRole = "Employee",
            Employee = firstEmployee,
            Store = store,
            IsActive = true
        };

        var secondAccount = new UserAccount
        {
            Email = secondEmployeeEmail,
            PasswordHash = passwordHasher.Hash(AuthTestHelper.Password),
            AccessRole = "Employee",
            Employee = secondEmployee,
            Store = store,
            IsActive = true
        };

        const string adminEmail = "admin.test@example.local";
        var adminAccount = new UserAccount
        {
            Email = adminEmail,
            PasswordHash = passwordHasher.Hash(AuthTestHelper.Password),
            AccessRole = "Admin",
            Store = store,
            IsActive = true
        };

        var schedule = new Schedule
        {
            Name = "Publicerat testschema",
            Store = store,
            PeriodStart = new DateOnly(2026, 7, 1),
            PeriodEnd = new DateOnly(2026, 7, 7),
            Status = "Published"
        };

        var draftSchedule = new Schedule
        {
            Name = "Utkast testschema",
            Store = store,
            PeriodStart = new DateOnly(2026, 8, 1),
            PeriodEnd = new DateOnly(2026, 8, 7),
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
            Status = "Published"
        };

        var secondShift = new Shift
        {
            Schedule = schedule,
            Employee = secondEmployee,
            ShiftType = shiftType,
            Date = new DateOnly(2026, 7, 2),
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0),
            Source = "Test",
            Status = "Published"
        };

        var draftShift = new Shift
        {
            Schedule = draftSchedule,
            Employee = firstEmployee,
            ShiftType = shiftType,
            Date = new DateOnly(2026, 8, 1),
            StartTime = new TimeOnly(8, 0),
            EndTime = new TimeOnly(16, 0),
            Source = "Test",
            Status = "Draft"
        };

        context.AddRange(
            store,
            role,
            shiftType,
            firstEmployee,
            secondEmployee,
            firstAccount,
            secondAccount,
            adminAccount,
            schedule,
            draftSchedule,
            firstShift,
            secondShift,
            draftShift);

        await context.SaveChangesAsync();

        return new SeedResult(
            firstEmployeeEmail,
            adminEmail,
            firstShift.Id,
            secondShift.Id,
            draftShift.Id);
    }

    private sealed record SeedResult(
        string FirstEmployeeEmail,
        string AdminEmail,
        int FirstEmployeeShiftId,
        int SecondEmployeeShiftId,
        int DraftShiftId);

}
