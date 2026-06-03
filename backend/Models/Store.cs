namespace Schemalaggning.Models;

public class Store
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public ICollection<UserAccount> UserAccounts { get; set; } = new List<UserAccount>();

    public ICollection<StoreCoverageRule> CoverageRules { get; set; } = new List<StoreCoverageRule>();
}
