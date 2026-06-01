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

        if (dto.Date < shift.Schedule.PeriodStart || dto.Date > shift.Schedule.PeriodEnd)
        {
            throw new ArgumentException("Shift date must be within the schedule period.");
        }

        if (!await _employeeRepository.HasAllowedShiftTypeAsync(dto.EmployeeId, dto.ShiftTypeId))
        {
            throw new InvalidOperationException("Employee is not allowed to work this shift type.");
        }

        shift.EmployeeId = dto.EmployeeId;
        shift.ShiftTypeId = dto.ShiftTypeId;
        shift.Date = dto.Date;
        shift.StartTime = dto.StartTime;
        shift.EndTime = dto.EndTime;

        return await _scheduleRepository.UpdateShiftAsync(shift);
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

        schedule.Status = "Published";
        foreach (var shift in schedule.Shifts)
        {
            shift.Status = "Published";
        }

        return await _scheduleRepository.UpdateAsync(schedule);
    }
}
