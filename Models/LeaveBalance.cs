namespace BackendApp.Models;

public class LeaveBalance
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public int Year { get; set; } = DateTime.UtcNow.Year;
    public decimal TotalDays { get; set; }
    public decimal UsedDays { get; set; } = 0;
    public decimal RemainingDays { get; set; }
}