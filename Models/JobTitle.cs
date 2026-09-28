
namespace BackendApp.Models;

public class JobTitle
{
    public int Id { get; set; }
    public string TitleName { get; set; } = string.Empty; // Junior Dev, HR Executive...
    public string Level { get; set; } = string.Empty; // Junior, Senior, Lead, Manager
    public bool IsActive { get; set; } = true;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}