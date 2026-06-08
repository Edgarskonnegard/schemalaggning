using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Services.Interfaces;
using backend.Tests.Infrastructure;

namespace backend.Tests;

public class LeaveRequestsEndpointTests : IClassFixture<TestApplicationFactory>
{
    private readonly TestApplicationFactory _factory;

    public LeaveRequestsEndpointTests(TestApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Employee_can_create_leave_request_and_admin_can_approve_it()
    {
        await _factory.ResetDatabaseAsync();
        var seed = await SeedScenarioAsync();
        using var client = _factory.CreateClient();
        await AuthTestHelper.AuthenticateAsync(client, seed.EmployeeEmail);

        var createResponse = await client.PostAsJsonAsync(
            "/api/me/leave-requests",
            new
            {
                startDate = new DateOnly(2026, 7, 13),
                endDate = new DateOnly(2026, 7, 17),
                reason = "Semester"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        int requestId;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var request = context.LeaveRequests.Single();
            requestId = request.Id;
            Assert.Equal(5, request.RequestedDays);
            Assert.Equal("Pending", request.Status);
        }

        await AuthTestHelper.AuthenticateAsync(client, seed.AdminEmail);

        var approveResponse = await client.PostAsJsonAsync(
            $"/api/admin/leave-requests/{requestId}/approve",
            new { adminComment = "Godkänd" });

        Assert.Equal(HttpStatusCode.NoContent, approveResponse.StatusCode);

        using var verifyScope = _factory.Services.CreateScope();
        var verifyContext = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var approved = verifyContext.LeaveRequests.Single();
        Assert.Equal("Approved", approved.Status);
        Assert.Equal("Godkänd", approved.AdminComment);
    }

    private async Task<SeedResult> SeedScenarioAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var suffix = Guid.NewGuid().ToString("N");
        var store = new Store { Name = $"Testbutik {suffix}" };
        var role = new Role { Name = $"Butiksmedarbetare {suffix}" };
        var employee = new Employee
        {
            Name = "Anna Ledighet",
            Store = store,
            Role = role,
            EmploymentPercentage = 100
        };

        const string employeeEmail = "anna.leave@example.local";
        const string adminEmail = "admin.leave@example.local";

        context.AddRange(
            store,
            role,
            employee,
            new LeaveAllowance
            {
                Employee = employee,
                Year = 2026,
                TotalDays = 25
            },
            new UserAccount
            {
                Email = employeeEmail,
                PasswordHash = passwordHasher.Hash(AuthTestHelper.Password),
                AccessRole = "Employee",
                Employee = employee,
                Store = store,
                IsActive = true
            },
            new UserAccount
            {
                Email = adminEmail,
                PasswordHash = passwordHasher.Hash(AuthTestHelper.Password),
                AccessRole = "Admin",
                Store = store,
                IsActive = true
            });

        await context.SaveChangesAsync();

        return new SeedResult(employeeEmail, adminEmail);
    }

    private sealed record SeedResult(string EmployeeEmail, string AdminEmail);
}
