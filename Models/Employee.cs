namespace BackendApp.Models;

public class Employee
{
    public int Id { get; set; }
    public string EmployeeCode { get; set; } = string.Empty; // Auto: EMP-001
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public DateTime HireDate { get; set; }
    public DateTime EndDate { get; set; }

    // Status: PROBATION (Thử việc), ACTIVE (Chính thức), ON_LEAVE (Tạm nghỉ), TERMINATED (Thôi việc)
    public string Status { get; set; } = "PROBATION";
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? JobTitleId { get; set; }
    public JobTitle? JobTitle { get; set; }

    // Quản lý trực tiếp (Manager - Employee Self-Referencing)
    public int? ManagerId { get; set; }
    public Employee? Manager { get; set; }
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    // Refresh Token
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiryTime { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}
