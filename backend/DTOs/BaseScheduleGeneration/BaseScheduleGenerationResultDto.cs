namespace Schemalaggning.DTOs.BaseScheduleGeneration;

public class BaseScheduleGenerationResultDto
{
    public int StoreId { get; set; }

    public int EmployeeCount { get; set; }

    public int CoverageRuleCount { get; set; }

    public int CreatedRuleCount { get; set; }

    public int UnassignedNeedCount { get; set; }

    public List<string> Warnings { get; set; } = new();
}
