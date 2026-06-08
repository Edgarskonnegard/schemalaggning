using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.DTOs.Me;
using Schemalaggning.DTOs.LeaveRequests;
using Schemalaggning.Services;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class MeController : ControllerBase
{
    private const int DefaultAnnualLeaveDays = 25;
    private readonly AppDbContext _context;

    public MeController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<MeReadDto>> GetMe()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var account = await _context.UserAccounts
            .AsNoTracking()
            .Include(userAccount => userAccount.Employee)
                .ThenInclude(employee => employee!.Store)
            .Include(userAccount => userAccount.Employee)
                .ThenInclude(employee => employee!.Role)
            .FirstOrDefaultAsync(userAccount => userAccount.Id == userId);

        if (account is null || !account.IsActive)
        {
            return Unauthorized();
        }

        var dto = new MeReadDto
        {
            Account = account.ToReadDto(),
            Employee = account.Employee?.ToReadDto()
        };

        if (account.EmployeeId is not null)
        {
            var today = DateOnly.FromDateTime(DateTime.Today);
            dto.LeaveBalance = await GetLeaveBalanceAsync(account.EmployeeId.Value, today.Year);

            dto.UpcomingShifts = await _context.Shifts
                .AsNoTracking()
                .Include(shift => shift.Schedule)
                    .ThenInclude(schedule => schedule.Store)
                .Include(shift => shift.ShiftType)
                .Where(shift =>
                    shift.EmployeeId == account.EmployeeId.Value &&
                    shift.Schedule.Status == "Published" &&
                    shift.Date >= today)
                .OrderBy(shift => shift.Date)
                .ThenBy(shift => shift.StartTime)
                .Take(20)
                .Select(shift => new MeShiftReadDto
                {
                    Id = shift.Id,
                    ScheduleId = shift.ScheduleId,
                    ScheduleName = shift.Schedule.Name,
                    StoreName = shift.Schedule.Store.Name,
                    ShiftTypeName = shift.ShiftType.Name,
                    Date = shift.Date,
                    StartTime = shift.StartTime,
                    EndTime = shift.EndTime,
                    Status = shift.Status
                })
                .ToListAsync();
        }

        return Ok(dto);
    }

    private async Task<LeaveBalanceReadDto> GetLeaveBalanceAsync(int employeeId, int year)
    {
        var allowance = await _context.LeaveAllowances
            .AsNoTracking()
            .FirstOrDefaultAsync(current => current.EmployeeId == employeeId && current.Year == year);

        var requests = await _context.LeaveRequests
            .AsNoTracking()
            .Where(request => request.EmployeeId == employeeId && request.StartDate.Year == year)
            .ToListAsync();

        var usedDays = requests
            .Where(request => request.Status == "Approved")
            .Sum(request => request.RequestedDays);
        var pendingDays = requests
            .Where(request => request.Status == "Pending")
            .Sum(request => request.RequestedDays);
        var totalDays = allowance?.TotalDays ?? DefaultAnnualLeaveDays;

        return new LeaveBalanceReadDto
        {
            Year = year,
            TotalDays = totalDays,
            UsedDays = usedDays,
            RemainingDays = Math.Max(0, totalDays - usedDays),
            PendingDays = pendingDays
        };
    }
}
