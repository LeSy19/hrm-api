namespace BackendApp.Models;

public class LeaveRequest
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;

    public int LeaveTypeId { get; set; }
    public LeaveType LeaveType { get; set; } = null!;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal TotalRequestedDays { get; set; }
    public string? Reason { get; set; }

    // Status: PENDING, APPROVED, REJECTED
    public string Status { get; set; } = "PENDING";

    public int? ApprovedBy { get; set; }
    public Employee? Approver { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}