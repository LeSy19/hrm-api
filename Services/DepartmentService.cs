using BackendApp.Data;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Extensions;
using BackendApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Services;


public class DepartmentService
{
    private readonly AppDbContext _dbContext;

    public DepartmentService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }


    // Lấy danh sách tất cả phòng ban
    public async Task<PagedResult<DepartmentResponseDto>> GetAllDepartmentsAsync(DepartmentFilterRequestDTO request)
    {
        var query = _dbContext.Departments
            .AsNoTracking(); // Tối ưu truy vấn Read-Only

        // Lọc / Tìm kiếm theo Tên hoặc Mã phòng ban (nếu có)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var keyword = request.SearchTerm.Trim().ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(keyword)
                                  || d.Code.ToLower().Contains(keyword));
        }

        // Projection sang DTO và OrderBy cố định trước khi phân trang
        var dtoQuery = query
            .OrderBy(d => d.Id)
            .Select(d => new DepartmentResponseDto(
                d.Id,
                d.Code,
                d.Name,
                d.ManagerId,
                d.Manager != null ? d.Manager.FullName : null,
                d.Employees.Count,
                d.CreatedAt,
                d.IsActive
            ));

        // Thực thi phân trang
        return await dtoQuery.ToPagedListAsync(request.PageIndex, request.PageSize);
    }

    public async Task<DepartmentResponseDto?> GetDepartmentByIdAsync(int id)
    {
        var department = await _dbContext.Departments
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (department == null) return null;

        return MapToDepartmentDTO(department);
    }

    public async Task<DepartmentResponseDto?> CreateDepartmentAsync(CreateDepartmentDto dto)
    {

        if (await _dbContext.Departments.AnyAsync(d => d.Code == dto.Code))
            return null; // Mã phòng ban đã tồn tại

        var dept = new Department
        {
            Code = dto.Code.ToUpper(),
            Name = dto.Name,
            ManagerId = dto.ManagerId,
            IsActive = true
        };

        _dbContext.Departments.Add(dept);
        await _dbContext.SaveChangesAsync();

        if (dept.ManagerId.HasValue)
            await _dbContext.Entry(dept).Reference(d => d.Manager).LoadAsync();

        return MapToDepartmentDTO(dept);
    }


    public async Task<DepartmentResponseDto?> UpdateDepartmentAsync(int id, UpdateDepartmentDto dto)
    {
        var dept = await _dbContext.Departments
            .Include(d => d.Manager)
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dept == null) return null;

        // Check mã trùng với phòng ban khác
        if (await _dbContext.Departments.AnyAsync(d => d.Code == dto.Code && d.Id != id))
            throw new ArgumentException("Mã phòng ban đã được sử dụng.");

        dept.Code = dto.Code.ToUpper();
        dept.Name = dto.Name;
        dept.ManagerId = dto.ManagerId;
        dept.IsActive = dto.IsActive;

        await _dbContext.SaveChangesAsync();

        return MapToDepartmentDTO(dept);
    }

    public async Task<(bool Success, string Message)> DeleteDepartmentAsync(int id)
    {
        var dept = await _dbContext.Departments
            .Include(d => d.Employees)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (dept == null)
            return (false, "Không tìm thấy phòng ban.");

        // BUSINESS RULE: Không cho phép xóa nếu vẫn còn nhân viên
        if (dept.Employees.Any())
            return (false, $"Không thể xóa phòng ban '{dept.Name}' vì đang có {dept.Employees.Count} nhân viên thuộc phòng này.");

        dept.IsActive = false;
        await _dbContext.SaveChangesAsync();
        return (true, "Xóa phòng ban thành công.");
    }
    public static DepartmentResponseDto MapToDepartmentDTO(Department d)
    {
        return new DepartmentResponseDto(
            d.Id,
            d.Code,
            d.Name,
            d.ManagerId,
            d.Manager != null ? d.Manager.FullName : null,
            d.Employees.Count,
            d.CreatedAt,
            d.IsActive
        );
    }

}