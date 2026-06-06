using Schemalaggning.Models;

namespace Schemalaggning.Repositories.Interfaces;

public interface IScheduleRepository
{
    Task<List<Schedule>> GetAllAsync();
    Task<Schedule?> GetByIdAsync(int id);
    Task<Schedule?> GetByIdWithShiftsAsync(int id);
    Task<Schedule> CreateAsync(Schedule schedule);
    Task<bool> UpdateAsync(Schedule schedule);
    Task<bool> DeleteAsync(int id);
    Task<Shift?> GetShiftByIdAsync(int shiftId);
    Task<bool> UpdateShiftAsync(Shift shift);
    Task<bool> UpdateShiftsAsync(IEnumerable<Shift> shifts);
}
