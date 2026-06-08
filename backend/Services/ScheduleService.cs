using Schemalaggning.DTOs.Schedules;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class ScheduleService : IScheduleService
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly IEmployeeRepository _employeeRepository;

    public ScheduleService(IScheduleRepository scheduleRepository, IEmployeeRepository employeeRepository)
    {
        _scheduleRepository = scheduleRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task<List<ScheduleReadDto>> GetAllAsync()
    {
        var schedules = await _scheduleRepository.GetAllAsync();
        return schedules.Select(schedule => schedule.ToReadDto()).ToList();
    }

    public async Task<ScheduleReadDto?> GetByIdAsync(int id)
    {
        var schedule = await _scheduleRepository.GetByIdWithShiftsAsync(id);
        return schedule?.ToReadDto();
    }

    public async Task<bool> UpdateShiftAsync(int shiftId, ShiftUpdateDto dto)
    {
        if (dto.EndTime <= dto.StartTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }

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
}
