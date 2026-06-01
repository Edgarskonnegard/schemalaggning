using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IBaseScheduleRuleRepository
{
    Task<List<BaseScheduleRule>> GetAllWithDetailsAsync();
    Task<List<BaseScheduleRule>> GetByEmployeeIdAsync(int employeeId);
    Task<BaseScheduleRule?> GetByEmployeeWeekAndDayAsync(int employeeId, int weekInCycle, DayOfWeek dayOfWeek);
    Task<BaseScheduleRule> CreateAsync(BaseScheduleRule rule);
    Task<bool> UpdateAsync(BaseScheduleRule rule);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByEmployeeWeekAndDayAsync(int employeeId, int weekInCycle, DayOfWeek dayOfWeek);
}
