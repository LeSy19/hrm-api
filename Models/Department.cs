namespace BackendApp.Models;

public class Department
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // Mã PB: HR, IT, MKT
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    // Trưởng phòng
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}