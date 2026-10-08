using BackendApp.DTOs.Common;

namespace BackendApp.DTOs;

public class LeaveRequestFilterRequestDTO : PaginationParam
{
    public string? SearchTerm { get; set; }

}

// Trả về thông tin đơn xin nghỉ
public record LeaveRequestResponseDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    int LeaveTypeId,
    string LeaveTypeName,
    bool IsPaid,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalRequestedDays,
    string? Reason,
    string Status,
    int? ApprovedBy,
    string? ApproverName,
    string? RejectionReason,
    DateTime CreatedAt
);

// DTO Employee tạo đơn xin nghỉ
public record CreateLeaveRequestDto(
    int LeaveTypeId,
    bool IsPaid,
    DateTime StartDate,
    DateTime EndDate,
    string? Reason
);

// DTO Phê duyệt hoặc Từ chối đơn
public record ApproveLeaveRequestDto(
    bool IsApproved,
    string? RejectionReason
);

// DTO thống kê số ngày nghỉ không lương của nhân viên theo tháng
public record UnpaidLeaveSummaryDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string? DepartmentName,
    int Month,
    int Year,
    decimal TotalUnpaidDays
);