using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Schemalaggning.DTOs.BaseScheduleApprovals;
using Schemalaggning.Services.Interfaces;

namespace Schemalaggning.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/approvals/base-schedules")]
public class BaseScheduleApprovalsController : ControllerBase
{
    private readonly IBaseScheduleApprovalService _approvalService;

    public BaseScheduleApprovalsController(IBaseScheduleApprovalService approvalService)
    {
        _approvalService = approvalService;
    }

    [HttpGet("pending-count")]
    public async Task<ActionResult<int>> GetPendingCount()
    {
        return Ok(await _approvalService.GetPendingCountAsync());
    }

    [HttpGet]
    public async Task<ActionResult<List<BaseScheduleApprovalBatchReadDto>>> GetPending()
    {
        return Ok(await _approvalService.GetPendingAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BaseScheduleApprovalBatchReadDto>> GetById(int id)
    {
        var batch = await _approvalService.GetByIdAsync(id);
        return batch is null ? NotFound() : Ok(batch);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id)
    {
        var approved = await _approvalService.ApproveAsync(id);
        return await ToApprovalResultAsync(id, approved);
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id)
    {
        var rejected = await _approvalService.RejectAsync(id);
        return await ToApprovalResultAsync(id, rejected);
    }

    [HttpPost("{id:int}/employees/{employeeId:int}/approve")]
    public async Task<IActionResult> ApproveEmployee(int id, int employeeId)
    {
        var approved = await _approvalService.ApproveEmployeeAsync(id, employeeId);
        return await ToApprovalResultAsync(id, approved);
    }

    [HttpPost("{id:int}/employees/{employeeId:int}/reject")]
    public async Task<IActionResult> RejectEmployee(int id, int employeeId)
    {
        var rejected = await _approvalService.RejectEmployeeAsync(id, employeeId);
        return await ToApprovalResultAsync(id, rejected);
    }

    [HttpPost("{id:int}/employees/{employeeId:int}/rules")]
    public async Task<ActionResult<BaseScheduleDraftRuleReadDto>> AddDraftRule(
        int id,
        int employeeId,
        BaseScheduleDraftRuleCreateDto dto)
    {
        try
        {
            var rule = await _approvalService.AddDraftRuleAsync(id, employeeId, dto);
            return rule is null ? NotFound() : Ok(rule);
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

    [HttpDelete("{id:int}/employees/{employeeId:int}/rules/{ruleId:int}")]
    public async Task<IActionResult> DeleteDraftRule(int id, int employeeId, int ruleId)
    {
        var deleted = await _approvalService.DeleteDraftRuleAsync(id, employeeId, ruleId);
        return deleted ? NoContent() : NotFound();
    }

    private async Task<IActionResult> ToApprovalResultAsync(int batchId, bool succeeded)
    {
        if (succeeded)
        {
            return NoContent();
        }

        var batch = await _approvalService.GetByIdAsync(batchId);
        if (batch is null)
        {
            return NotFound();
        }

        return batch.Status == "Pending"
            ? NotFound()
            : Conflict("Endast väntande grundschemautkast kan ändras.");
    }
}
