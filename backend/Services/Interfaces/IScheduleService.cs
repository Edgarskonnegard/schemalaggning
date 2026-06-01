using Schemalaggning.DTOs.Schedules;

namespace Schemalaggning.Services.Interfaces;

public interface IScheduleService
{
    Task<List<ScheduleReadDto>> GetAllAsync();
    Task<ScheduleReadDto?> GetByIdAsync(int id);
    Task<bool> UpdateShiftAsync(int shiftId, ShiftUpdateDto dto);
    Task<bool> PublishScheduleAsync(int scheduleId);
}
