using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.BaseScheduleRules;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
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
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpDelete("{dayOfWeek}")]
    public async Task<IActionResult> DeleteRule(int employeeId, DayOfWeek dayOfWeek)
    {
        var deleted = await _baseScheduleRuleService.DeleteRuleAsync(employeeId, dayOfWeek);
        return deleted ? NoContent() : NotFound();
    }
}
