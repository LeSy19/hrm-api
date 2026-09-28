namespace BackendApp.Models;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Ví dụ: Admin, HRManager, Employee
    public string Description { get; set; } = string.Empty;

    // Navigation Property: Một Role chứa danh sách nhiều Employees
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
