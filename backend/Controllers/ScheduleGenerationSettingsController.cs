using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.ScheduleGenerationSettings;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/stores/{storeId:int}/schedule-generation-settings")]
public class ScheduleGenerationSettingsController : ControllerBase
{
    private readonly IScheduleGenerationSettingsService _settingsService;

    public ScheduleGenerationSettingsController(IScheduleGenerationSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<ScheduleGenerationSettingsReadDto>> GetByStoreId(int storeId)
    {
        try
        {
            return Ok(await _settingsService.GetByStoreIdAsync(storeId));
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(exception.Message);
        }
    }

    [HttpPut]
    public async Task<ActionResult<ScheduleGenerationSettingsReadDto>> Update(
        int storeId,
        ScheduleGenerationSettingsUpdateDto dto)
    {
        try
        {
            return Ok(await _settingsService.UpdateAsync(storeId, dto));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return NotFound(exception.Message);
        }
    }
}
