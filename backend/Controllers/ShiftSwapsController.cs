using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.DTOs.ShiftSwaps;
using Schemalaggning.Models;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
public class ShiftSwapsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ShiftSwapsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("shift-swaps")]
    public async Task<ActionResult<List<ShiftSwapRequestReadDto>>> GetMyShiftSwaps()
    {
        var employeeId = GetEmployeeIdFromClaims();
        if (employeeId is null)
        {
            return Forbid();
        }

        var requests = await BuildSwapRequestQuery()
            .Where(request =>
                request.Status == "Pending" &&
                (request.FromEmployeeId == employeeId.Value || request.ToEmployeeId == employeeId.Value))
            .OrderBy(request => request.Shift.Date)
            .ThenBy(request => request.Shift.StartTime)
            .Select(request => ToReadDto(request))
            .ToListAsync();

        return Ok(requests);
    }

    [HttpGet("shifts/{shiftId:int}/swap-candidates")]
    public async Task<ActionResult<List<ShiftSwapCandidateReadDto>>> GetSwapCandidates(int shiftId)
    {
        var employeeId = GetEmployeeIdFromClaims();
        if (employeeId is null)
        {
            return Forbid();
        }

        var shift = await _context.Shifts
            .AsNoTracking()
            .Include(current => current.Schedule)
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
            return Conflict("Endast publicerade pass kan bytas.");
        }

        return Ok(await GetEligibleEmployeesAsync(shift));
    }

    [HttpPost("shifts/{shiftId:int}/swap-requests")]
    public async Task<ActionResult<ShiftSwapRequestReadDto>> CreateSwapRequest(
        int shiftId,
        ShiftSwapCreateDto dto)
    {
        var employeeId = GetEmployeeIdFromClaims();
        if (employeeId is null)
        {
            return Forbid();
        }

        var shift = await _context.Shifts
            .Include(current => current.Schedule)
                .ThenInclude(schedule => schedule.Store)
            .Include(current => current.ShiftType)
            .Include(current => current.Employee)
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
            return Conflict("Endast publicerade pass kan bytas.");
        }

        if (dto.ToEmployeeId == employeeId.Value)
        {
            return BadRequest("Du kan inte skicka ett passbyte till dig själv.");
        }

        var message = dto.Message.Trim();
        if (message.Length > 1000)
        {
            return BadRequest("Meddelandet får vara max 1000 tecken.");
        }

        if (await _context.ShiftSwapRequests.AnyAsync(request =>
            request.ShiftId == shift.Id &&
            request.Status == "Pending"))
        {
            return Conflict("Det finns redan en väntande bytesförfrågan för passet.");
        }

        if (!await IsEligibleEmployeeAsync(dto.ToEmployeeId, shift))
        {
            return Conflict("Mottagaren är inte ledig eller får inte arbeta passtypen.");
        }

        var request = new ShiftSwapRequest
        {
            ShiftId = shift.Id,
            FromEmployeeId = employeeId.Value,
            ToEmployeeId = dto.ToEmployeeId,
            Message = message,
            Status = "Pending",
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ShiftSwapRequests.Add(request);
        await _context.SaveChangesAsync();

        var created = await BuildSwapRequestQuery()
            .FirstAsync(current => current.Id == request.Id);

        return Created($"/api/me/shift-swaps/{request.Id}", ToReadDto(created));
    }

    [HttpPost("shift-swaps/{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var employeeId = GetEmployeeIdFromClaims();
        if (employeeId is null)
        {
            return Forbid();
        }

        var request = await BuildSwapRequestQuery()
            .FirstOrDefaultAsync(current => current.Id == id);

        if (request is null)
        {
            return NotFound();
        }

        if (request.ToEmployeeId != employeeId.Value)
        {
            return Forbid();
        }

        if (request.Status != "Pending")
        {
            return Conflict("Endast väntande bytesförfrågningar kan godkännas.");
        }

        if (request.Shift.EmployeeId != request.FromEmployeeId)
        {
            return Conflict("Passet ägs inte längre av avsändaren.");
        }

        if (!await IsEligibleEmployeeAsync(request.ToEmployeeId, request.Shift))
        {
            return Conflict("Du är inte längre ledig eller behörig för passet.");
        }

        request.Shift.EmployeeId = request.ToEmployeeId;
        request.Status = "Approved";
        request.RespondedAtUtc = DateTime.UtcNow;

        var otherPendingRequests = await _context.ShiftSwapRequests
            .Where(current =>
                current.Id != request.Id &&
                current.ShiftId == request.ShiftId &&
                current.Status == "Pending")
            .ToListAsync();

        foreach (var otherRequest in otherPendingRequests)
        {
            otherRequest.Status = "Rejected";
            otherRequest.RespondedAtUtc = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("shift-swaps/{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var employeeId = GetEmployeeIdFromClaims();
        if (employeeId is null)
        {
            return Forbid();
        }

        var request = await _context.ShiftSwapRequests.FindAsync(id);
        if (request is null)
        {
            return NotFound();
        }

        if (request.ToEmployeeId != employeeId.Value)
        {
            return Forbid();
        }

        if (request.Status != "Pending")
        {
            return Conflict("Endast väntande bytesförfrågningar kan nekas.");
        }

        request.Status = "Rejected";
        request.RespondedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<ShiftSwapRequest> BuildSwapRequestQuery()
    {
        return _context.ShiftSwapRequests
            .Include(request => request.FromEmployee)
            .Include(request => request.ToEmployee)
            .Include(request => request.Shift)
                .ThenInclude(shift => shift.Schedule)
                    .ThenInclude(schedule => schedule.Store)
            .Include(request => request.Shift)
                .ThenInclude(shift => shift.ShiftType);
    }

    private async Task<List<ShiftSwapCandidateReadDto>> GetEligibleEmployeesAsync(Shift shift)
    {
        return await _context.Employees
            .AsNoTracking()
            .Include(employee => employee.Role)
            .Where(employee =>
                employee.Id != shift.EmployeeId &&
                employee.StoreId == shift.Schedule.StoreId &&
                employee.UserAccount != null &&
                employee.UserAccount.IsActive &&
                employee.Role.RoleShiftTypes.Any(roleShiftType => roleShiftType.ShiftTypeId == shift.ShiftTypeId) &&
                !employee.Shifts.Any(current =>
                    current.Schedule.Status == "Published" &&
                    current.Date == shift.Date) &&
                !employee.LeaveRequests.Any(request =>
                    (request.Status == "Pending" || request.Status == "Approved") &&
                    request.StartDate <= shift.Date &&
                    request.EndDate >= shift.Date))
            .OrderBy(employee => employee.Name)
            .Select(employee => new ShiftSwapCandidateReadDto
            {
                EmployeeId = employee.Id,
                EmployeeName = employee.Name,
                RoleName = employee.Role.Name
            })
            .ToListAsync();
    }

    private Task<bool> IsEligibleEmployeeAsync(int employeeId, Shift shift)
    {
        return _context.Employees.AnyAsync(employee =>
            employee.Id == employeeId &&
            employee.StoreId == shift.Schedule.StoreId &&
            employee.UserAccount != null &&
            employee.UserAccount.IsActive &&
            employee.Role.RoleShiftTypes.Any(roleShiftType => roleShiftType.ShiftTypeId == shift.ShiftTypeId) &&
            !employee.Shifts.Any(current =>
                current.Schedule.Status == "Published" &&
                current.Date == shift.Date) &&
            !employee.LeaveRequests.Any(request =>
                (request.Status == "Pending" || request.Status == "Approved") &&
                request.StartDate <= shift.Date &&
                request.EndDate >= shift.Date));
    }

    private int? GetEmployeeIdFromClaims()
    {
        var employeeIdClaim = User.FindFirst("employeeId")?.Value;
        return int.TryParse(employeeIdClaim, out var employeeId) ? employeeId : null;
    }

    private static ShiftSwapRequestReadDto ToReadDto(ShiftSwapRequest request)
    {
        return new ShiftSwapRequestReadDto
        {
            Id = request.Id,
            ShiftId = request.ShiftId,
            FromEmployeeId = request.FromEmployeeId,
            FromEmployeeName = request.FromEmployee?.Name ?? string.Empty,
            ToEmployeeId = request.ToEmployeeId,
            ToEmployeeName = request.ToEmployee?.Name ?? string.Empty,
            StoreName = request.Shift?.Schedule?.Store?.Name ?? string.Empty,
            ShiftTypeName = request.Shift?.ShiftType?.Name ?? string.Empty,
            Date = request.Shift?.Date ?? default,
            StartTime = request.Shift?.StartTime ?? default,
            EndTime = request.Shift?.EndTime ?? default,
            Message = request.Message,
            Status = request.Status,
            CreatedAtUtc = request.CreatedAtUtc,
            RespondedAtUtc = request.RespondedAtUtc
        };
    }
}
