using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IBaseScheduleRuleRepository
{
    Task<List<BaseScheduleRule>> GetAllWithDetailsAsync();
    Task<List<BaseScheduleRule>> GetByEmployeeIdAsync(int employeeId);
    Task<BaseScheduleRule?> GetByEmployeeAndDayAsync(int employeeId, DayOfWeek dayOfWeek);
    Task<BaseScheduleRule> CreateAsync(BaseScheduleRule rule);
    Task<bool> UpdateAsync(BaseScheduleRule rule);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByEmployeeAndDayAsync(int employeeId, DayOfWeek dayOfWeek);
}
