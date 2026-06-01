using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.Repositories;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services;
using Schemalaggning.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IShiftTypeRepository, ShiftTypeRepository>();
builder.Services.AddScoped<IBaseScheduleRuleRepository, BaseScheduleRuleRepository>();
builder.Services.AddScoped<IScheduleRepository, ScheduleRepository>();

builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IShiftTypeService, ShiftTypeService>();
builder.Services.AddScoped<IBaseScheduleRuleService, BaseScheduleRuleService>();
builder.Services.AddScoped<IScheduleService, ScheduleService>();
builder.Services.AddScoped<IScheduleGenerationService, ScheduleGenerationService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
