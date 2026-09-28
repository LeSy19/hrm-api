namespace BackendApp.DTOs;

// DTO Tổng quan chỉ số nhân sự
public record DashboardSummaryDto(
    int TotalActiveEmployees,
    int TotalProbationEmployees,
    int TotalDepartments,
    int EmployeesOnLeaveToday
);

// DTO Phân bổ nhân sự theo phòng ban
public record DepartmentEmployeeCountDto(
    int DepartmentId,
    string DepartmentCode,
    string DepartmentName,
    int TotalEmployees
);

// DTO Thông tin nhân viên đang nghỉ phép hôm nay
public record EmployeeOnLeaveTodayDto(
    int EmployeeId,
    string EmployeeCode,
    string FullName,
    string DepartmentName,
    string LeaveTypeName,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalDays
);

// DTO Thống kê trạng thái đơn nghỉ phép trong tháng
public record MonthlyLeaveStatsDto(
    int Month,
    int Year,
    int TotalPending,
    int TotalApproved,
    int TotalRejected
);