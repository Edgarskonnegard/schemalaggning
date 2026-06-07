using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.BaseScheduleRules;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/employees/{employeeId:int}/base-schedule")]
public class BaseScheduleRulesController : ControllerBase
{
    private readonly IBaseScheduleRuleService _baseScheduleRuleService;

    public BaseScheduleRulesController(IBaseScheduleRuleService baseScheduleRuleService)
    {
        _baseScheduleRuleService = baseScheduleRuleService;
    }

    [HttpGet]
    public async Task<ActionResult<List<BaseScheduleRuleReadDto>>> GetByEmployeeId(int employeeId)
    {
        return Ok(await _baseScheduleRuleService.GetByEmployeeIdAsync(employeeId));
    }

    [HttpPut]
    public async Task<ActionResult<BaseScheduleRuleReadDto>> SetRule(
        int employeeId,
        BaseScheduleRuleCreateDto dto)
    {
        try
        {
            return Ok(await _baseScheduleRuleService.SetRuleAsync(employeeId, dto));
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

    [HttpDelete("{weekInCycle:int}/{dayOfWeek}")]
    public async Task<IActionResult> DeleteRule(int employeeId, int weekInCycle, DayOfWeek dayOfWeek)
    {
        var deleted = await _baseScheduleRuleService.DeleteRuleAsync(employeeId, weekInCycle, dayOfWeek);
        return deleted ? NoContent() : NotFound();
    }
}
