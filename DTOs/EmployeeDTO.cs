using BackendApp.DTOs.Common;

namespace BackendApp.DTOs;

public class EmployeeFilterRequestDTO : PaginationParam
{
    public string? SearchTerm { get; set; }

    public int? DepartmentId { get; set; }
}

// DTO dùng khi trả dữ liệu Employee ra ngoài
public record EmployeeResponseDTO(
    int Id,
    string EmployeeCode,
    string Username,
    string Email,
    string FullName,
    string? Phone,
    DateTime HireDate,
    DateTime EndDate,
    string Status,
    int RoleId,
    string RoleName,
    int? DepartmentId,
    string? DepartmentName,
    int? JobTitleId,
    string? JobTitleName,
    int? ManagerId,
    string? ManagerName,
    DateTime CreatedAt
);

// DTO dùng cho tạo mới Employee
public record CreateEmployeeDTO(
    string Username,
    string Email,
    string Password,
    string FullName,
    string? Phone,
    DateTime HireDate,
    DateTime EndDate,
    string Status = "PROBATION", // PROBATION, ACTIVE, ON_LEAVE, TERMINATED
    int RoleId = 3,             // Mặc định Employee
    int? DepartmentId = null,
    int? JobTitleId = null,
    int? ManagerId = null
);

// DTO dùng cho cập nhật thông tin Employee
public record UpdateEmployeeDTO(
    string Email,
    string FullName,
    string? Phone,
    DateTime HireDate,
    DateTime EndDate,
    string Status,
    int RoleId,
    int? DepartmentId,
    int? JobTitleId,
    int? ManagerId
);