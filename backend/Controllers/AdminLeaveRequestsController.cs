using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.DTOs.LeaveRequests;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/leave-requests")]
public class AdminLeaveRequestsController : ControllerBase
{
    private const int DefaultAnnualLeaveDays = 25;
    private readonly AppDbContext _context;

    public AdminLeaveRequestsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("pending-count")]
    public Task<int> GetPendingCount()
    {
        return _context.LeaveRequests.CountAsync(request => request.Status == "Pending");
    }

    [HttpGet]
    public async Task<ActionResult<List<LeaveRequestReadDto>>> GetPending()
    {
        var requests = await _context.LeaveRequests
            .AsNoTracking()
            .Include(request => request.Employee)
                .ThenInclude(employee => employee.Role)
            .Where(request => request.Status == "Pending")
            .OrderBy(request => request.StartDate)
            .Select(request => ToReadDto(request))
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("schedule-blocks")]
    public async Task<ActionResult<List<LeaveRequestReadDto>>> GetScheduleBlocks(
        [FromQuery] int storeId,
        [FromQuery] DateOnly periodStart,
        [FromQuery] DateOnly periodEnd)
    {
        if (periodEnd < periodStart)
        {
            return BadRequest("Period end must be after or equal to period start.");
        }

        var requests = await _context.LeaveRequests
            .AsNoTracking()
            .Include(request => request.Employee)
                .ThenInclude(employee => employee.Role)
            .Where(request =>
                request.Employee.StoreId == storeId &&
                (request.Status == "Pending" || request.Status == "Approved") &&
                request.StartDate <= periodEnd &&
                request.EndDate >= periodStart)
            .OrderBy(request => request.StartDate)
            .ThenBy(request => request.Employee.Name)
            .Select(request => ToReadDto(request))
            .ToListAsync();

        return Ok(requests);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, LeaveRequestReviewDto dto)
    {
        var request = await _context.LeaveRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != "Pending")
        {
            return Conflict("Endast väntande ledighetsansökningar kan godkännas.");
        }

        var balance = await GetLeaveBalanceAsync(request.EmployeeId, request.StartDate.Year);
        if (request.RequestedDays > balance.RemainingDays)
        {
            return Conflict("Ansökan överstiger kvarvarande semesterdagar.");
        }

        request.Status = "Approved";
        request.AdminComment = dto.AdminComment.Trim();
        request.ReviewedAtUtc = DateTime.UtcNow;
        request.ReviewedByUserAccountId = GetUserIdFromClaims();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, LeaveRequestReviewDto dto)
    {
        var request = await _context.LeaveRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.Status != "Pending")
        {
            return Conflict("Endast väntande ledighetsansökningar kan nekas.");
        }

        request.Status = "Rejected";
        request.AdminComment = dto.AdminComment.Trim();
        request.ReviewedAtUtc = DateTime.UtcNow;
        request.ReviewedByUserAccountId = GetUserIdFromClaims();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task<LeaveBalanceReadDto> GetLeaveBalanceAsync(int employeeId, int year)
    {
        var allowance = await _context.LeaveAllowances
            .AsNoTracking()
            .FirstOrDefaultAsync(current => current.EmployeeId == employeeId && current.Year == year);

        var usedDays = await _context.LeaveRequests
            .AsNoTracking()
            .Where(request =>
                request.EmployeeId == employeeId &&
                request.StartDate.Year == year &&
                request.Status == "Approved")
            .SumAsync(request => request.RequestedDays);

        var totalDays = allowance?.TotalDays ?? DefaultAnnualLeaveDays;

        return new LeaveBalanceReadDto
        {
            Year = year,
            TotalDays = totalDays,
            UsedDays = usedDays,
            RemainingDays = Math.Max(0, totalDays - usedDays),
            PendingDays = 0
        };
    }

    private static LeaveRequestReadDto ToReadDto(Models.LeaveRequest request)
    {
        return new LeaveRequestReadDto
        {
            Id = request.Id,
            EmployeeId = request.EmployeeId,
            EmployeeName = request.Employee?.Name ?? string.Empty,
            EmployeeRoleName = request.Employee?.Role?.Name ?? string.Empty,
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
}
