using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.DTOs.LeaveRequests;
using Schemalaggning.Models;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize]
[Route("api/me/leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    private const int DefaultAnnualLeaveDays = 25;
    private readonly AppDbContext _context;

    public LeaveRequestsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaveRequestReadDto>>> GetMyLeaveRequests()
    {
        var employeeId = GetEmployeeIdFromClaims();
        if (employeeId is null)
        {
            return Forbid();
        }

        var requests = await _context.LeaveRequests
            .AsNoTracking()
            .Include(request => request.Employee)
            .Where(request => request.EmployeeId == employeeId.Value)
            .OrderByDescending(request => request.StartDate)
            .Select(request => ToReadDto(request))
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("balance")]
    public async Task<ActionResult<LeaveBalanceReadDto>> GetMyLeaveBalance([FromQuery] int? year)
    {
        var employeeId = GetEmployeeIdFromClaims();
        if (employeeId is null)
        {
            return Forbid();
        }

        return Ok(await GetLeaveBalanceAsync(employeeId.Value, year ?? DateTime.Today.Year));
    }

    [HttpPost]
    public async Task<ActionResult<LeaveRequestReadDto>> CreateMyLeaveRequest(LeaveRequestCreateDto dto)
    {
        var employeeId = GetEmployeeIdFromClaims();
        if (employeeId is null)
        {
            return Forbid();
        }

        if (dto.EndDate < dto.StartDate)
        {
            return BadRequest("Slutdatum måste vara efter eller samma som startdatum.");
        }

        if (dto.StartDate.Year != dto.EndDate.Year)
        {
            return BadRequest("Ledighetsansökan måste ligga inom ett kalenderår.");
        }

        var requestedDays = CountWeekdays(dto.StartDate, dto.EndDate);
        if (requestedDays <= 0)
        {
            return BadRequest("Ledighetsansökan måste innehålla minst en vardag.");
        }

        var balance = await GetLeaveBalanceAsync(employeeId.Value, dto.StartDate.Year);
        if (requestedDays > balance.RemainingDays)
        {
            return BadRequest("Ansökan överstiger kvarvarande semesterdagar.");
        }

        var request = new LeaveRequest
        {
            EmployeeId = employeeId.Value,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            RequestedDays = requestedDays,
            Reason = dto.Reason.Trim(),
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.LeaveRequests.Add(request);
        await _context.SaveChangesAsync();

        var created = await _context.LeaveRequests
            .AsNoTracking()
            .Include(leaveRequest => leaveRequest.Employee)
            .FirstAsync(leaveRequest => leaveRequest.Id == request.Id);

        return CreatedAtAction(nameof(GetMyLeaveRequests), new { id = request.Id }, ToReadDto(created));
    }

    private int? GetEmployeeIdFromClaims()
    {
        var employeeIdClaim = User.FindFirst("employeeId")?.Value;
        return int.TryParse(employeeIdClaim, out var employeeId) ? employeeId : null;
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

    private static LeaveRequestReadDto ToReadDto(LeaveRequest request)
    {
        return new LeaveRequestReadDto
        {
            Id = request.Id,
            EmployeeId = request.EmployeeId,
            EmployeeName = request.Employee?.Name ?? string.Empty,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            RequestedDays = request.RequestedDays,
            Reason = request.Reason,
            Status = request.Status,
            CreatedAtUtc = request.CreatedAtUtc,
            ReviewedAtUtc = request.ReviewedAtUtc,
            AdminComment = request.AdminComment
        };
    }

    private static int CountWeekdays(DateOnly startDate, DateOnly endDate)
    {
        var days = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
            {
                days++;
            }
        }

        return days;
    }
}
