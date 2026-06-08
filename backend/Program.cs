using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Schemalaggning.Data;
using Schemalaggning.Models;
using Schemalaggning.Repositories;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services;
using Schemalaggning.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Schemalaggning";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "Schemalaggning";
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException("JWT signing key is missing.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IStoreRepository, StoreRepository>();
builder.Services.AddScoped<IUserAccountRepository, UserAccountRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IShiftTypeRepository, ShiftTypeRepository>();
builder.Services.AddScoped<IBaseScheduleRuleRepository, BaseScheduleRuleRepository>();
builder.Services.AddScoped<IStoreCoverageRuleRepository, StoreCoverageRuleRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();

builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IStoreService, StoreService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IShiftTypeService, ShiftTypeService>();
builder.Services.AddScoped<IBaseScheduleRuleService, BaseScheduleRuleService>();
builder.Services.AddScoped<IBaseScheduleGenerationService, BaseScheduleGenerationService>();
builder.Services.AddScoped<IBaseScheduleApprovalService, BaseScheduleApprovalService>();
builder.Services.AddScoped<IStoreCoverageRuleService, StoreCoverageRuleService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IScheduleGenerationService, ScheduleGenerationService>();
builder.Services.AddScoped<IScheduleGenerationSettingsService, ScheduleGenerationSettingsService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    await SeedDevelopmentAdminAsync(app.Services);
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static async Task SeedDevelopmentAdminAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

    const string adminLogin = "admin";

    if (await context.UserAccounts.AnyAsync(account => account.Email == adminLogin))
    {
        return;
    }

    context.UserAccounts.Add(new UserAccount
    {
        Email = adminLogin,
        PasswordHash = passwordHasher.Hash("admin"),
        AccessRole = "Admin",
        IsActive = true
    });

    await context.SaveChangesAsync();
}

public partial class Program
{
}
