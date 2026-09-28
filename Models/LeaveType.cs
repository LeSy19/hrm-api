namespace BackendApp.Models;

public class LeaveType
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // Annual Leave, Sick Leave, Unpaid Leave
    public decimal DaysAllowed { get; set; }// Mặc định 12 ngày/năm
    public bool IsActive { get; set; } = true;

    public ICollection<LeaveBalance> LeaveBalances { get; set; } = new List<LeaveBalance>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
}