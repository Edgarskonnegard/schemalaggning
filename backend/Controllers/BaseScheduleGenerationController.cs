using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.BaseScheduleGeneration;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/stores/{storeId:int}/base-schedules")]
public class BaseScheduleGenerationController : ControllerBase
{
    private readonly IBaseScheduleGenerationService _generationService;

    public BaseScheduleGenerationController(IBaseScheduleGenerationService generationService)
    {
        _generationService = generationService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<BaseScheduleGenerationResultDto>> Generate(int storeId)
    {
        try
        {
            return Ok(await _generationService.GenerateForStoreAsync(storeId));
        }
        catch (InvalidOperationException exception)
        {
            if (exception.Message.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(exception.Message);
            }

            return Conflict(exception.Message);
        }
    }
}
