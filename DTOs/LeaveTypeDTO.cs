using BackendApp.DTOs.Common;

namespace BackendApp.DTOs;

public class LeaveTypeFilterRequestDTO : PaginationParam
{
    public string? SearchTerm { get; set; }

}

public record LeaveTypeResponseDto(
    int Id,
    string Name,
    decimal DaysAllowed,
    bool IsActive
);

// DTO tạo mới loại phép
public record CreateLeaveTypeDto(
    string Name,
    decimal DaysAllowed
);

// DTO cập nhật loại phép
public record UpdateLeaveTypeDto(
    string Name,
    decimal DaysAllowed,
    bool IsActive
);