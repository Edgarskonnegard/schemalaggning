using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Services.Interfaces;
using backend.Tests.Infrastructure;

namespace backend.Tests;

public class ShiftCommentsEndpointTests : IClassFixture<TestApplicationFactory>
{
    private const string EmployeePassword = "Password123!";
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
        await AuthenticateAsync(client, seed.FirstEmployeeEmail);

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
        await AuthenticateAsync(client, seed.FirstEmployeeEmail);

        var response = await client.PostAsJsonAsync(
            $"/api/me/shifts/{seed.SecondEmployeeShiftId}/comments",
            new { message = "Den här borde jag inte kunna kommentera." });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task AuthenticateAsync(HttpClient client, string email)
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email, password = EmployeePassword });

        response.EnsureSuccessStatusCode();

        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.AccessToken);
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
            PasswordHash = passwordHasher.Hash(EmployeePassword),
            AccessRole = "Employee",
            Employee = firstEmployee,
            Store = store,
            IsActive = true
        };

        var secondAccount = new UserAccount
        {
            Email = secondEmployeeEmail,
            PasswordHash = passwordHasher.Hash(EmployeePassword),
            AccessRole = "Employee",
            Employee = secondEmployee,
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

        context.AddRange(
            store,
            role,
            shiftType,
            firstEmployee,
            secondEmployee,
            firstAccount,
            secondAccount,
            schedule,
            firstShift,
            secondShift);

        await context.SaveChangesAsync();

        return new SeedResult(
            firstEmployeeEmail,
            firstShift.Id,
            secondShift.Id);
    }

    private sealed record SeedResult(
        string FirstEmployeeEmail,
        int FirstEmployeeShiftId,
        int SecondEmployeeShiftId);

    private sealed class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}
