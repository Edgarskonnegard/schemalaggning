using Schemalaggning.DTOs.BaseScheduleRules;

namespace Schemalaggning.Services.Interfaces;

public interface IBaseScheduleRuleService
{
    Task<List<BaseScheduleRuleReadDto>> GetByEmployeeIdAsync(int employeeId);
    Task<BaseScheduleRuleReadDto> SetRuleAsync(int employeeId, BaseScheduleRuleCreateDto dto);
    Task<bool> DeleteRuleAsync(int employeeId, int weekInCycle, DayOfWeek dayOfWeek);
}
