using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.Schedules;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
[Route("api")]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _scheduleService;
    private readonly IScheduleGenerationService _scheduleGenerationService;

    public SchedulesController(
        IScheduleService scheduleService,
        IScheduleGenerationService scheduleGenerationService)
    {
        _scheduleService = scheduleService;
        _scheduleGenerationService = scheduleGenerationService;
    }

    [HttpGet("schedules")]
    public async Task<ActionResult<List<ScheduleReadDto>>> GetAll()
    {
        return Ok(await _scheduleService.GetAllAsync());
    }

    [HttpGet("schedules/{id:int}")]
    public async Task<ActionResult<ScheduleReadDto>> GetById(int id)
    {
        var schedule = await _scheduleService.GetByIdAsync(id);
        return schedule is null ? NotFound() : Ok(schedule);
    }

    [HttpPost("schedules/generate")]
    public async Task<ActionResult<ScheduleReadDto>> Generate(ScheduleCreateDto dto)
    {
        try
        {
            var schedule = await _scheduleGenerationService.GenerateFromBaseScheduleAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, schedule);
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

    [HttpPost("stores/{storeId:int}/schedules/generate")]
    public async Task<ActionResult<ScheduleReadDto>> GenerateForStore(int storeId, ScheduleCreateDto dto)
    {
        try
        {
            dto.StoreId = storeId;
            var schedule = await _scheduleGenerationService.GenerateFromBaseScheduleAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = schedule.Id }, schedule);
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

    [HttpPut("schedules/{id:int}/publish")]
    public async Task<IActionResult> Publish(int id)
    {
        try
        {
            var published = await _scheduleService.PublishScheduleAsync(id);
            return published ? NoContent() : NotFound();
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(exception.Message);
        }
    }

    [HttpPut("shifts/{shiftId:int}")]
    public async Task<IActionResult> UpdateShift(int shiftId, ShiftUpdateDto dto)
    {
        try
        {
            var updated = await _scheduleService.UpdateShiftAsync(shiftId, dto);
            return updated ? NoContent() : NotFound();
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

    [HttpPut("shifts/{shiftId:int}/swap")]
    public async Task<IActionResult> SwapShiftEmployees(int shiftId, ShiftSwapDto dto)
    {
        try
        {
            var updated = await _scheduleService.SwapShiftEmployeesAsync(shiftId, dto);
            return updated ? NoContent() : NotFound();
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
}
