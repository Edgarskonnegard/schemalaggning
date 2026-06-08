using Schemalaggning.DTOs.BaseScheduleApprovals;

namespace Schemalaggning.Services.Interfaces;

public interface IBaseScheduleApprovalService
{
    Task<int> GetPendingCountAsync();
    Task<List<BaseScheduleApprovalBatchReadDto>> GetPendingAsync();
    Task<BaseScheduleApprovalBatchReadDto?> GetByIdAsync(int id);
    Task<bool> ApproveAsync(int id);
    Task<bool> RejectAsync(int id);
    Task<bool> ApproveEmployeeAsync(int batchId, int employeeId);
    Task<bool> RejectEmployeeAsync(int batchId, int employeeId);
    Task<BaseScheduleDraftRuleReadDto?> AddDraftRuleAsync(int batchId, int employeeId, BaseScheduleDraftRuleCreateDto dto);
    Task<bool> DeleteDraftRuleAsync(int batchId, int employeeId, int ruleId);
}
