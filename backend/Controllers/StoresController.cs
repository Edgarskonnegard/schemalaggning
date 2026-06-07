using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.Stores;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/stores")]
public class StoresController : ControllerBase
{
    private readonly IStoreService _storeService;

    public StoresController(IStoreService storeService)
    {
        _storeService = storeService;
    }

    [HttpGet]
    public async Task<ActionResult<List<StoreReadDto>>> GetAll()
    {
        return Ok(await _storeService.GetAllAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<StoreReadDto>> GetById(int id)
    {
        var store = await _storeService.GetByIdAsync(id);
        return store is null ? NotFound() : Ok(store);
    }

    [HttpPost]
    public async Task<ActionResult<StoreReadDto>> Create(StoreCreateDto dto)
    {
        try
        {
            var store = await _storeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = store.Id }, store);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, StoreCreateDto dto)
    {
        try
        {
            var updated = await _storeService.UpdateAsync(id, dto);
            return updated ? NoContent() : NotFound();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var deleted = await _storeService.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
