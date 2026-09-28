using BackendApp.Data;
using BackendApp.Models;
using Microsoft.EntityFrameworkCore;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Extensions;

namespace BackendApp.Services;

public class EmployeeService
{

    private readonly AppDbContext _context;

    public EmployeeService(AppDbContext context)
    {
        _context = context;
    }

    // 1. READ ALL - CÓ PHÂN TRANG
    public async Task<PagedResult<EmployeeResponseDTO>> GetAllEmployeesAsync(EmployeeFilterRequestDTO request)
    {
        var query = _context.Employees
            .Include(e => e.Role)
            .Include(e => e.Department)
            .Include(e => e.JobTitle)
            .Include(e => e.Manager)
            .AsNoTracking(); // Tối ưu hiệu năng cho thao tác đọc

        // Lọc/Tìm kiếm (nếu Frontend gửi SearchTerm)
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var keyword = request.SearchTerm.Trim().ToLower();
            query = query.Where(e => e.FullName.ToLower().Contains(keyword)
                                  || e.Email.ToLower().Contains(keyword)
                                  || e.EmployeeCode.ToLower().Contains(keyword));
        }

        // Lọc theo phòng ban (nếu Frontend gửi DepartmentId)
        if (request.DepartmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == request.DepartmentId.Value);
        }

        // Tạo Query mapping sang DTO
        var dtoQuery = query
            .OrderBy(e => e.Id) // BẮT BUỘC có OrderBy trước khi Skip/Take
            .Select(e => MapToEmployeeDTO(e));

        // Gọi Extension Method phân trang dùng chung
        return await dtoQuery.ToPagedListAsync(request.PageIndex, request.PageSize);
    }
    // 2. READ BY ID (Lấy nhân viên theo Id)
    public async Task<EmployeeResponseDTO?> GetEmployeeByIdAsync(int id)
    {
        var employee = await _context.Employees
            .Include(e => e.Role)
            .Include(e => e.Department)
            .Include(e => e.JobTitle)
            .Include(e => e.Manager)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) return null;

        return MapToEmployeeDTO(employee);
    }

    // 3. CREATE (Tạo mới nhân viên)
    public async Task<EmployeeResponseDTO?> CreateEmployeeAsync(CreateEmployeeDTO dto)
    {
        // Kiểm tra xem Username hoặc Email đã tồn tại chưa
        if (await _context.Employees.AnyAsync(e => e.Username == dto.Username || e.Email == dto.Email))
            return null;

        // Kiểm tra xem RoleId có hợp lệ không
        if (!await _context.Roles.AnyAsync(r => r.Id == dto.RoleId))
            throw new ArgumentException("RoleId không tồn tại trong hệ thống.");

        // Sinh mã nhân viên tự động (EMP-001, EMP-002...)
        string newEmployeeCode = await GenerateEmployeeCodeAsync();

        var employee = new Employee
        {
            EmployeeCode = newEmployeeCode,
            Username = dto.Username,
            Email = dto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            FullName = dto.FullName,
            Phone = dto.Phone,
            HireDate = dto.HireDate,
            EndDate = dto.EndDate,
            Status = dto.Status,
            RoleId = dto.RoleId,
            DepartmentId = dto.DepartmentId,
            JobTitleId = dto.JobTitleId,
            ManagerId = dto.ManagerId
        };

        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();

        // Load lại các Relationship để trả về DTO đầy đủ
        await _context.Entry(employee).Reference(e => e.Role).LoadAsync();
        if (employee.DepartmentId.HasValue) await _context.Entry(employee).Reference(e => e.Department).LoadAsync();
        if (employee.JobTitleId.HasValue) await _context.Entry(employee).Reference(e => e.JobTitle).LoadAsync();
        if (employee.ManagerId.HasValue) await _context.Entry(employee).Reference(e => e.Manager).LoadAsync();

        return MapToEmployeeDTO(employee);
    }

    // 4. UPDATE (Cập nhật thông tin nhân viên)
    public async Task<EmployeeResponseDTO?> UpdateEmployeeAsync(int id, UpdateEmployeeDTO dto)
    {
        var employee = await _context.Employees
            .Include(e => e.Role)
            .Include(e => e.Department)
            .Include(e => e.JobTitle)
            .Include(e => e.Manager)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (employee == null) return null;

        if (!await _context.Roles.AnyAsync(r => r.Id == dto.RoleId))
            throw new ArgumentException("RoleId không tồn tại.");

        employee.Email = dto.Email;
        employee.FullName = dto.FullName;
        employee.Phone = dto.Phone;
        employee.HireDate = dto.HireDate;
        employee.EndDate = dto.EndDate;
        employee.Status = dto.Status;
        employee.RoleId = dto.RoleId;
        employee.DepartmentId = dto.DepartmentId;
        employee.JobTitleId = dto.JobTitleId;
        employee.ManagerId = dto.ManagerId;

        await _context.SaveChangesAsync();

        return MapToEmployeeDTO(employee);
    }

    // 5. DELETE (Xóa nhân viên)
    public async Task<bool> DeleteEmployeeAsync(int id)
    {
        var employee = await _context.Employees.FindAsync(id);
        if (employee == null) return false;

        employee.Status = "TERMINATED";
        await _context.SaveChangesAsync();
        return true;
    }

    // --- HÀM PHỤ TRỢ MAPPING DTO & SINH MÃ NHÂN VIÊN ---
    private static EmployeeResponseDTO MapToEmployeeDTO(Employee e)
    {
        return new EmployeeResponseDTO(
            e.Id,
            e.EmployeeCode,
            e.Username,
            e.Email,
            e.FullName,
            e.Phone,
            e.HireDate,
            e.EndDate,
            e.Status,
            e.RoleId,
            e.Role?.Name ?? string.Empty,
            e.DepartmentId,
            e.Department?.Name,
            e.JobTitleId,
            e.JobTitle?.TitleName,
            e.ManagerId,
            e.Manager?.FullName,
            e.CreatedAt
        );
    }

    private async Task<string> GenerateEmployeeCodeAsync()
    {
        var lastEmployee = await _context.Employees
            .OrderByDescending(e => e.Id)
            .FirstOrDefaultAsync();

        int nextNumber = (lastEmployee?.Id ?? 0) + 1;
        return $"EMP-{nextNumber:D3}"; // Trả về dạng EMP-001, EMP-002...
    }

}