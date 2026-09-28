namespace BackendApp.Services;

using BackendApp.Data;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Extensions;
using BackendApp.Models;
using Microsoft.EntityFrameworkCore;

public class JobTitleService
{
    private readonly AppDbContext _context;

    public JobTitleService(AppDbContext context)
    {
        _context = context;
    }

    // 1. READ ALL (Lấy danh sách chức danh kèm số lượng nhân viên)
    public async Task<PagedResult<JobTitleResponseDto>> GetAllJobTitlesAsync(JobTitleFilterRequestDTO request)
    {
        var query = _context.JobTitles.AsNoTracking();

        // Tìm kiếm theo Chức danh (TitleName) hoặc Cấp bậc (Level)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var keyword = request.SearchTerm.Trim().ToLower();
            query = query.Where(j => j.TitleName.ToLower().Contains(keyword)
                                  || j.Level.ToLower().Contains(keyword));
        }

        // Projection sang DTO và OrderBy trước khi phân trang
        var dtoQuery = query
            .OrderBy(j => j.Id)
            .Select(j => new JobTitleResponseDto(
                j.Id,
                j.TitleName,
                j.Level,
                j.Employees.Count,
                j.IsActive
            ));

        // Thực thi phân trang
        return await dtoQuery.ToPagedListAsync(request.PageIndex, request.PageSize);
    }


    // 2. READ BY ID (Lấy chức danh theo Id)
    public async Task<JobTitleResponseDto?> GetJobTitleByIdAsync(int id)
    {
        var jobTitle = await _context.JobTitles
            .Include(j => j.Employees)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (jobTitle == null) return null;

        return MapToJobTitleDTO(jobTitle);
    }

    public async Task<JobTitleResponseDto> CreateJobTitleAsync(CreateJobTitleDto dto)
    {

        //Kiểm tra TitleName đã tồn tại chưa
        if (await _context.JobTitles.AnyAsync(j => j.TitleName == dto.TitleName))
            throw new ArgumentException("Chức danh đã tồn tại trong hệ thống.");

        var job = new JobTitle
        {
            TitleName = dto.TitleName,
            Level = dto.Level,
            IsActive = true
        };

        _context.JobTitles.Add(job);
        await _context.SaveChangesAsync();

        return MapToJobTitleDTO(job);
    }

    public async Task<JobTitleResponseDto?> UpdateJobTitleAsync(int id, UpdateJobTitleDto dto)
    {
        var job = await _context.JobTitles
            .Include(j => j.Employees)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null) return null;

        job.TitleName = dto.TitleName;
        job.Level = dto.Level;
        job.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return MapToJobTitleDTO(job);
    }

    public async Task<(bool Success, string Message)> DeleteJobTitleAsync(int id)
    {
        var job = await _context.JobTitles
            .Include(j => j.Employees)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return (false, "Không tìm thấy chức danh.");

        // BUSINESS RULE: Không cho phép xóa nếu có nhân viên đang giữ chức danh này
        if (job.Employees.Any())
            return (false, $"Không thể xóa chức danh '{job.TitleName}' vì đang có {job.Employees.Count} nhân viên đảm nhận.");

        job.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, "Xóa chức danh thành công.");
    }

    public static JobTitleResponseDto MapToJobTitleDTO(JobTitle j)
    {
        return new JobTitleResponseDto(
            j.Id,
            j.TitleName,
            j.Level,
            j.Employees.Count,
            j.IsActive
        );
    }
}
