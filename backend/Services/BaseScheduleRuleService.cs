using Schemalaggning.DTOs.BaseScheduleRules;
using Schemalaggning.Models;
using Schemalaggning.Repositories.Interfaces;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Services;

public class BaseScheduleRuleService : IBaseScheduleRuleService
{
    private readonly IBaseScheduleRuleRepository _baseScheduleRuleRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IShiftTypeRepository _shiftTypeRepository;

    public BaseScheduleRuleService(
        IBaseScheduleRuleRepository baseScheduleRuleRepository,
        IEmployeeRepository employeeRepository,
        IShiftTypeRepository shiftTypeRepository)
    {
        _baseScheduleRuleRepository = baseScheduleRuleRepository;
        _employeeRepository = employeeRepository;
        _shiftTypeRepository = shiftTypeRepository;
    }

    public async Task<List<BaseScheduleRuleReadDto>> GetByEmployeeIdAsync(int employeeId)
    {
        var rules = await _baseScheduleRuleRepository.GetByEmployeeIdAsync(employeeId);
        return rules.Select(rule => rule.ToReadDto()).ToList();
    }

    public async Task<BaseScheduleRuleReadDto> SetRuleAsync(int employeeId, BaseScheduleRuleCreateDto dto)
    {
        if (dto.WeekInCycle is < 1 or > 4)
        {
            throw new ArgumentException("Week in cycle must be between 1 and 4.");
        }

        var employee = await _employeeRepository.GetByIdAsync(employeeId);
        if (employee is null)
        {
            throw new InvalidOperationException($"Employee {employeeId} does not exist.");
        }

        if (!await _employeeRepository.CanWorkShiftTypeAsync(employeeId, dto.ShiftTypeId))
        {
            throw new InvalidOperationException("Employee role does not allow this shift type.");
        }

        var shiftType = await _shiftTypeRepository.GetByIdAsync(dto.ShiftTypeId);
        if (shiftType is null)
        {
            throw new InvalidOperationException($"Shift type {dto.ShiftTypeId} does not exist.");
        }

        var startTime = dto.StartTime ?? shiftType.DefaultStartTime;
        var endTime = dto.EndTime ?? shiftType.DefaultEndTime;

        if (endTime <= startTime)
        {
            throw new ArgumentException("End time must be after start time.");
        }

        var existingRule = await _baseScheduleRuleRepository.GetByEmployeeWeekAndDayAsync(
            employeeId,
            dto.WeekInCycle,
            dto.DayOfWeek);

        if (existingRule is null)
        {
            var created = await _baseScheduleRuleRepository.CreateAsync(new BaseScheduleRule
            {
                EmployeeId = employeeId,
                ShiftTypeId = dto.ShiftTypeId,
                WeekInCycle = dto.WeekInCycle,
                DayOfWeek = dto.DayOfWeek,
                StartTime = startTime,
                EndTime = endTime
            });

            var rules = await _baseScheduleRuleRepository.GetByEmployeeIdAsync(employeeId);
            return rules.First(rule => rule.Id == created.Id).ToReadDto();
        }

        existingRule.ShiftTypeId = dto.ShiftTypeId;
        existingRule.WeekInCycle = dto.WeekInCycle;
        existingRule.StartTime = startTime;
        existingRule.EndTime = endTime;
        await _baseScheduleRuleRepository.UpdateAsync(existingRule);

        var updatedRules = await _baseScheduleRuleRepository.GetByEmployeeIdAsync(employeeId);
        return updatedRules.First(rule => rule.Id == existingRule.Id).ToReadDto();
    }

    public Task<bool> DeleteRuleAsync(int employeeId, int weekInCycle, DayOfWeek dayOfWeek)
    {
        return _baseScheduleRuleRepository.DeleteByEmployeeWeekAndDayAsync(employeeId, weekInCycle, dayOfWeek);
    }
}
