using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.ShiftTypes;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/shift-types")]
public class ShiftTypesController : ControllerBase
{
    private readonly IShiftTypeService _shiftTypeService;

    public ShiftTypesController(IShiftTypeService shiftTypeService)
    {
        _shiftTypeService = shiftTypeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ShiftTypeReadDto>>> GetAll()
    {
        return Ok(await _shiftTypeService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ShiftTypeReadDto>> GetById(int id)
    {
        var shiftType = await _shiftTypeService.GetByIdAsync(id);
        return shiftType is null ? NotFound() : Ok(shiftType);
    }

    [HttpPost]
    public async Task<ActionResult<ShiftTypeReadDto>> Create(ShiftTypeCreateDto dto)
    {
        try
        {
            var shiftType = await _shiftTypeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = shiftType.Id }, shiftType);
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

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, ShiftTypeUpdateDto dto)
    {
        try
        {
            var updated = await _shiftTypeService.UpdateAsync(id, dto);
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

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _shiftTypeService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
