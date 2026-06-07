using Schemalaggning.DTOs.ScheduleGenerationSettings;

namespace Schemalaggning.Services.Interfaces;

public interface IScheduleGenerationSettingsService
{
    Task<ScheduleGenerationSettingsReadDto> GetByStoreIdAsync(int storeId);
    Task<ScheduleGenerationSettingsReadDto> UpdateAsync(int storeId, ScheduleGenerationSettingsUpdateDto dto);
}
