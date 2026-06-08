using Microsoft.EntityFrameworkCore;
using Schemalaggning.Data;
using Schemalaggning.DTOs.Schedules;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly AppDbContext _context;

    public ScheduleService(
        IScheduleRepository scheduleRepository,
        IEmployeeRepository employeeRepository,
        AppDbContext context)
    {
        _scheduleRepository = scheduleRepository;
        _employeeRepository = employeeRepository;
        _context = context;
    }

    public async Task<List<ScheduleReadDto>> GetAllAsync()
    {
        var schedules = await _scheduleRepository.GetAllAsync();
        return schedules.Select(schedule => schedule.ToReadDto()).ToList();
    }

    public async Task<ScheduleReadDto?> GetByIdAsync(int id)
    {
        var schedule = await _scheduleRepository.GetByIdWithShiftsAsync(id);
        return schedule is null ? null : await ToReadDtoWithCoverageAsync(schedule);
    }

    public async Task<ScheduleReadDto?> CreateShiftAsync(int scheduleId, ShiftCreateDto dto)
    {
        ValidateShiftTime(dto.StartTime, dto.EndTime);

        var schedule = await _scheduleRepository.GetByIdWithShiftsAsync(scheduleId);
        if (schedule is null)
        {
            return null;
        }

        if (schedule.Status != "Draft")
        {
            throw new InvalidOperationException("Only draft schedules can receive new shifts.");
        }

        if (dto.Date < schedule.PeriodStart || dto.Date > schedule.PeriodEnd)
        {
            throw new ArgumentException("Shift date must be within the schedule period.");
        }

        var employee = await _employeeRepository.GetByIdAsync(dto.EmployeeId);
        if (employee is null)
        {
            throw new InvalidOperationException($"Employee {dto.EmployeeId} does not exist.");
        }

        if (employee.StoreId != schedule.StoreId)
        {
            throw new InvalidOperationException("Employee belongs to another store.");
        }

        if (!await _employeeRepository.CanWorkShiftTypeAsync(dto.EmployeeId, dto.ShiftTypeId))
        {
            throw new InvalidOperationException("Employee role does not allow this shift type.");
        }

        var hasLeave = await _context.LeaveRequests.AnyAsync(request =>
            request.EmployeeId == dto.EmployeeId &&
            (request.Status == "Pending" || request.Status == "Approved") &&
            request.StartDate <= dto.Date &&
            request.EndDate >= dto.Date);

        if (hasLeave)
        {
            throw new InvalidOperationException("Employee has leave on this date.");
        }

        var hasShiftOnDate = schedule.Shifts.Any(shift =>
            shift.EmployeeId == dto.EmployeeId &&
            shift.Date == dto.Date);

        if (hasShiftOnDate)
        {
            throw new InvalidOperationException("Employee already has a shift on this date.");
        }

        _context.Shifts.Add(new Shift
        {
            ScheduleId = schedule.Id,
            EmployeeId = dto.EmployeeId,
            ShiftTypeId = dto.ShiftTypeId,
            Date = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            Source = "ManualCoverage",
            Status = "Draft"
        });

        await _context.SaveChangesAsync();

        var updatedSchedule = await _scheduleRepository.GetByIdWithShiftsAsync(scheduleId);
        return updatedSchedule is null ? null : await ToReadDtoWithCoverageAsync(updatedSchedule);
    }

    public async Task<bool> UpdateShiftAsync(int shiftId, ShiftUpdateDto dto)
    {
        ValidateShiftTime(dto.StartTime, dto.EndTime);

        var shift = await _scheduleRepository.GetShiftByIdAsync(shiftId);
        if (shift is null)
        {
            return false;
        }

        if (shift.Schedule.Status != "Draft" && shift.Schedule.Status != "Published")
        {
            throw new InvalidOperationException("Only draft or published schedule shifts can be updated.");
        }

        if (dto.Date != shift.Date)
        {
            throw new ArgumentException("Shift date cannot be changed from this view.");
        }

        if (dto.Date < shift.Schedule.PeriodStart || dto.Date > shift.Schedule.PeriodEnd)
        {
            throw new ArgumentException("Shift date must be within the schedule period.");
        }

        if (!await _employeeRepository.CanWorkShiftTypeAsync(dto.EmployeeId, dto.ShiftTypeId))
        {
            throw new InvalidOperationException("Employee role does not allow this shift type.");
        }

        shift.EmployeeId = dto.EmployeeId;
        shift.ShiftTypeId = dto.ShiftTypeId;
        shift.Date = dto.Date;
        shift.StartTime = dto.StartTime;
        shift.EndTime = dto.EndTime;

        return await _scheduleRepository.UpdateShiftAsync(shift);
    }

    public async Task<bool> SwapShiftEmployeesAsync(int shiftId, ShiftSwapDto dto)
    {
        if (shiftId == dto.TargetShiftId)
        {
            throw new ArgumentException("Cannot swap a shift with itself.");
        }

        var sourceShift = await _scheduleRepository.GetShiftByIdAsync(shiftId);
        var targetShift = await _scheduleRepository.GetShiftByIdAsync(dto.TargetShiftId);

        if (sourceShift is null || targetShift is null)
        {
            return false;
        }

        if (sourceShift.ScheduleId != targetShift.ScheduleId)
        {
            throw new ArgumentException("Shifts must belong to the same schedule.");
        }

        if (!CanEditSchedule(sourceShift.Schedule.Status) || !CanEditSchedule(targetShift.Schedule.Status))
        {
            throw new InvalidOperationException("Only draft or published schedule shifts can be swapped.");
        }

        if (sourceShift.Date != targetShift.Date)
        {
            throw new ArgumentException("Only shifts on the same date can be swapped.");
        }

        if (!await _employeeRepository.CanWorkShiftTypeAsync(targetShift.EmployeeId, sourceShift.ShiftTypeId))
        {
            throw new InvalidOperationException("Target employee role does not allow the source shift type.");
        }

        if (!await _employeeRepository.CanWorkShiftTypeAsync(sourceShift.EmployeeId, targetShift.ShiftTypeId))
        {
            throw new InvalidOperationException("Source employee role does not allow the target shift type.");
        }

        var sourceEmployeeId = sourceShift.EmployeeId;
        sourceShift.EmployeeId = targetShift.EmployeeId;
        targetShift.EmployeeId = sourceEmployeeId;

        return await _scheduleRepository.UpdateShiftsAsync([sourceShift, targetShift]);
    }

    public async Task<bool> PublishScheduleAsync(int scheduleId)
    {
        var schedule = await _scheduleRepository.GetByIdWithShiftsAsync(scheduleId);
        if (schedule is null)
        {
            return false;
        }

        if (schedule.Status != "Draft")
        {
            throw new InvalidOperationException("Only draft schedules can be published.");
        }

        if (await _scheduleRepository.HasPublishedOverlapAsync(
            schedule.StoreId,
            schedule.PeriodStart,
            schedule.PeriodEnd,
            schedule.Id))
        {
            throw new InvalidOperationException("Schedule period overlaps an already published schedule.");
        }

        var coverageGaps = await GetCoverageGapsAsync(schedule);
        if (coverageGaps.Count > 0)
        {
            throw new InvalidOperationException("Schedule has uncovered staffing requirements.");
        }

        schedule.Status = "Published";
        foreach (var shift in schedule.Shifts)
        {
            shift.Status = "Published";
        }

        return await _scheduleRepository.UpdateAsync(schedule);
    }

    private static bool CanEditSchedule(string status)
    {
        return status == "Draft" || status == "Published";
    }

    private static void ValidateShiftTime(TimeOnly startTime, TimeOnly endTime)
    {
        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }
    }

    private async Task<ScheduleReadDto> ToReadDtoWithCoverageAsync(Schedule schedule)
    {
        var dto = schedule.ToReadDto();
        dto.CoverageGaps = await GetCoverageGapsAsync(schedule);
        return dto;
    }

    private async Task<List<ScheduleCoverageGapDto>> GetCoverageGapsAsync(Schedule schedule)
    {
        var coverageRules = await _context.StoreCoverageRules
            .AsNoTracking()
            .Include(rule => rule.ShiftType)
            .Where(rule => rule.StoreId == schedule.StoreId)
            .ToListAsync();

        var gaps = new List<ScheduleCoverageGapDto>();

        for (var date = schedule.PeriodStart; date <= schedule.PeriodEnd; date = date.AddDays(1))
        {
            foreach (var rule in coverageRules.Where(rule => rule.DayOfWeek == date.DayOfWeek))
            {
                var assignedCount = schedule.Shifts.Count(shift =>
                    shift.Date == date &&
                    shift.ShiftTypeId == rule.ShiftTypeId &&
                    shift.StartTime == rule.StartTime &&
                    shift.EndTime == rule.EndTime);

                if (assignedCount >= rule.RequiredCount)
                {
                    continue;
                }

                gaps.Add(new ScheduleCoverageGapDto
                {
                    Date = date,
                    ShiftTypeId = rule.ShiftTypeId,
                    ShiftTypeName = rule.ShiftType?.Name ?? string.Empty,
                    StartTime = rule.StartTime,
                    EndTime = rule.EndTime,
                    RequiredCount = rule.RequiredCount,
                    AssignedCount = assignedCount,
                    MissingCount = rule.RequiredCount - assignedCount
                });
            }
        }

        return gaps
            .OrderBy(gap => gap.Date)
            .ThenBy(gap => gap.StartTime)
            .ThenBy(gap => gap.ShiftTypeName)
            .ToList();
    }
}
