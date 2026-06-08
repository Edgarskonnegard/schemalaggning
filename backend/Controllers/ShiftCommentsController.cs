using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.DTOs.ShiftComments;
using Schemalaggning.Models;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize]
public class ShiftCommentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShiftCommentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("api/me/shifts/{shiftId:int}/comments")]
    public async Task<ActionResult<ShiftCommentReadDto>> CreateForMyShift(
        int shiftId,
        ShiftCommentCreateDto dto)
    {
        var employeeId = GetEmployeeIdFromClaims();
        if (employeeId is null)
        {
            return Forbid();
        }

        var message = dto.Message.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return BadRequest("Kommentaren får inte vara tom.");
        }

        if (message.Length > 1000)
        {
            return BadRequest("Kommentaren får vara max 1000 tecken.");
        }

        var shift = await _context.Shifts
            .Include(current => current.Schedule)
                .ThenInclude(schedule => schedule.Store)
            .Include(current => current.Employee)
            .Include(current => current.ShiftType)
            .FirstOrDefaultAsync(current => current.Id == shiftId);

        if (shift is null)
        {
            return NotFound();
        }

        if (shift.EmployeeId != employeeId.Value)
        {
            return Forbid();
        }

        if (shift.Schedule.Status != "Published")
        {
            return Conflict("Endast publicerade pass kan kommenteras.");
        }

        var comment = new ShiftComment
        {
            ShiftId = shift.Id,
            EmployeeId = employeeId.Value,
            Message = message,
            Status = "New",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ShiftComments.Add(comment);
        await _context.SaveChangesAsync();

        comment.Shift = shift;
        comment.Employee = shift.Employee;

        return Created($"/api/admin/shift-comments/{comment.Id}", ToReadDto(comment));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("api/admin/shift-comments/pending-count")]
    public Task<int> GetPendingCount()
    {
        return _context.ShiftComments.CountAsync(comment => comment.Status == "New");
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("api/admin/shift-comments")]
    public async Task<ActionResult<List<ShiftCommentReadDto>>> GetPendingForAdmin()
    {
        var comments = await _context.ShiftComments
            .AsNoTracking()
            .Include(comment => comment.Employee)
            .Include(comment => comment.Shift)
                .ThenInclude(shift => shift.Schedule)
                    .ThenInclude(schedule => schedule.Store)
            .Include(comment => comment.Shift)
                .ThenInclude(shift => shift.ShiftType)
            .Where(comment => comment.Status == "New")
            .OrderBy(comment => comment.CreatedAtUtc)
            .Select(comment => ToReadDto(comment))
            .ToListAsync();

        return Ok(comments);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost("api/admin/shift-comments/{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id)
    {
        var comment = await _context.ShiftComments.FindAsync(id);
        if (comment is null)
        {
            return NotFound();
        }

        if (comment.Status != "New")
        {
            return Conflict("Endast nya passkommentarer kan markeras som hanterade.");
        }

        comment.Status = "Resolved";
        comment.ResolvedAtUtc = DateTime.UtcNow;
        comment.ResolvedByUserAccountId = GetUserIdFromClaims();

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private int? GetEmployeeIdFromClaims()
    {
        var employeeIdClaim = User.FindFirst("employeeId")?.Value;
        return int.TryParse(employeeIdClaim, out var employeeId) ? employeeId : null;
    }

    private int? GetUserIdFromClaims()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private static ShiftCommentReadDto ToReadDto(ShiftComment comment)
    {
        return new ShiftCommentReadDto
        {
            Id = comment.Id,
            ShiftId = comment.ShiftId,
            EmployeeId = comment.EmployeeId,
            EmployeeName = comment.Employee?.Name ?? string.Empty,
            StoreName = comment.Shift?.Schedule?.Store?.Name ?? string.Empty,
            ShiftTypeName = comment.Shift?.ShiftType?.Name ?? string.Empty,
            Date = comment.Shift?.Date ?? default,
            StartTime = comment.Shift?.StartTime ?? default,
            EndTime = comment.Shift?.EndTime ?? default,
            Message = comment.Message,
            Status = comment.Status,
            CreatedAtUtc = comment.CreatedAtUtc,
            ResolvedAtUtc = comment.ResolvedAtUtc
        };
    }
}
