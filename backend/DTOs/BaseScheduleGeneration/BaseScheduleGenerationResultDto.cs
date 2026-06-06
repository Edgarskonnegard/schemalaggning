namespace Schemalaggning.DTOs.BaseScheduleGeneration;

public class BaseScheduleGenerationResultDto
{
    public int BatchId { get; set; }

    public int StoreId { get; set; }

    public string Status { get; set; } = string.Empty;

    public int EmployeeCount { get; set; }

    public int CoverageRuleCount { get; set; }

    public int CreatedRuleCount { get; set; }

    public int UnassignedNeedCount { get; set; }

    public List<string> Warnings { get; set; } = new();
}
