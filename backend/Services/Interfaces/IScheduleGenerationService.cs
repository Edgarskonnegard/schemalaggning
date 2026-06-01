using Schemalaggning.DTOs.Schedules;

namespace Schemalaggning.Services.Interfaces;

public interface IScheduleGenerationService
{
    Task<ScheduleReadDto> GenerateFromBaseScheduleAsync(ScheduleCreateDto dto);
}
