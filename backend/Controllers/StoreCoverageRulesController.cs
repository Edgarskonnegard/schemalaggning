using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.StoreCoverageRules;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
[Route("api/stores/{storeId:int}/coverage-rules")]
public class StoreCoverageRulesController : ControllerBase
{
    private readonly IStoreCoverageRuleService _coverageRuleService;

    public StoreCoverageRulesController(IStoreCoverageRuleService coverageRuleService)
    {
        _coverageRuleService = coverageRuleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<StoreCoverageRuleReadDto>>> GetByStoreId(int storeId)
    {
        return Ok(await _coverageRuleService.GetByStoreIdAsync(storeId));
    }

    [HttpPut]
    public async Task<ActionResult<StoreCoverageRuleReadDto>> SetRule(
        int storeId,
        StoreCoverageRuleCreateDto dto)
    {
        try
        {
            return Ok(await _coverageRuleService.SetRuleAsync(storeId, dto));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int storeId, int id)
    {
        var deleted = await _coverageRuleService.DeleteAsync(storeId, id);
        return deleted ? NoContent() : NotFound();
    }
}
