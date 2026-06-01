namespace Schemalaggning.Models;

public class Schedule
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateOnly PeriodStart { get; set; }

    public DateOnly PeriodEnd { get; set; }

    public string Status { get; set; } = "Draft";

    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}