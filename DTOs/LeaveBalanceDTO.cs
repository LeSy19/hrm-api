using BackendApp.DTOs.Common;

namespace BackendApp.DTOs;

// Trả về thông tin quỹ phép của nhân viên
public class LeaveBalanceFilterRequestDTO : PaginationParam
{
    public string? SearchTerm { get; set; }

}
public record LeaveBalanceResponseDto(
    int Id,
    int EmployeeId,
    string EmployeeName,
    string EmployeeCode,
    int LeaveTypeId,
    string LeaveTypeName,
    int Year,
    decimal TotalDays,
    decimal UsedDays,
    decimal RemainingDays
);

// Cấp quỹ phép cho một nhân viên cụ thể
public record AssignLeaveBalanceDto(
    int EmployeeId,
    int LeaveTypeId,
    int Year,
    decimal TotalDays
);

// Cấp quỹ phép hàng loạt theo Loại phép cho TẤT CẢ nhân viên trong năm
public record BulkAssignLeaveBalanceDto(
    int LeaveTypeId,
    int Year,
    decimal TotalDays
);