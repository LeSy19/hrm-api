using BackendApp.DTOs.Common;

namespace BackendApp.DTOs;

public class JobTitleFilterRequestDTO : PaginationParam
{
    public string? SearchTerm { get; set; }

}
public record JobTitleResponseDto(
    int Id,
    string TitleName,
    string Level,
    int TotalEmployees,
    bool IsActive
);

public record CreateJobTitleDto(
    string TitleName,
    string Level
);

public record UpdateJobTitleDto(
    string TitleName,
    string Level,
    bool IsActive
);