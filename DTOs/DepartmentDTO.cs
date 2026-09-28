using BackendApp.DTOs.Common;

namespace BackendApp.DTOs;

public class DepartmentFilterRequestDTO : PaginationParam
{
    public string? SearchTerm { get; set; }

}

public record DepartmentResponseDto(
    int Id,
    string Code,
    string Name,
    int? ManagerId,
    string? ManagerName,
    int TotalEmployees,
    DateTime CreatedAt,
    bool IsActive
);

public record CreateDepartmentDto(
    string Code,
    string Name,
    int? ManagerId
);

public record UpdateDepartmentDto(
    string Code,
    string Name,
    int? ManagerId,
    bool IsActive
);
