using Schemalaggning.DTOs.BaseScheduleGeneration;

namespace Schemalaggning.Services.Interfaces;

public interface IBaseScheduleGenerationService
{
    Task<BaseScheduleGenerationResultDto> GenerateForStoreAsync(int storeId);
}
