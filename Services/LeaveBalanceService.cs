using BackendApp.Data;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Extensions;
using BackendApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Services;

public class LeaveBalanceService
{
    private readonly AppDbContext _context;

    public LeaveBalanceService(AppDbContext context)
    {
        _context = context;
    }

    // 1. Xem quỹ phép của Nhân viên đang đăng nhập
    public async Task<PagedResult<LeaveBalanceResponseDto>> GetMyBalancesAsync(LeaveBalanceFilterRequestDTO request, int currentEmployeeId, int? year)
    {
        int currentYear = year ?? DateTime.UtcNow.Year;

        var query = _context.LeaveBalances
            .AsNoTracking()
            .Where(lb => lb.EmployeeId == currentEmployeeId && lb.Year == currentYear);

        var dtoQuery = query
            .OrderBy(lb => lb.LeaveType.Name)
            .Select(lb => new LeaveBalanceResponseDto(
                lb.Id,
                lb.EmployeeId,
                lb.Employee.FullName,
                lb.Employee.EmployeeCode,
                lb.LeaveTypeId,
                lb.LeaveType.Name,
                lb.LeaveType.IsPaid,
                lb.Year,
                lb.TotalDays,
                lb.UsedDays,
                lb.RemainingDays
            ));

        return await dtoQuery.ToPagedListAsync(request.PageIndex, request.PageSize);
    }

    // 2. Xem quỹ phép của tất cả nhân viên(HR/Admin quản lý)
    //
    // Luồng xử lý:
    // - Xác định năm cần lọc (mặc định là năm hiện tại)
    // - Tạo truy vấn cơ bản lọc theo năm
    // - Nếu có tham số departmentId -> Lọc bổ sung theo phòng ban của nhân viên
    // - Select dữ liệu ra dạng DTO (bao gồm tên nhân viên, mã nhân viên, tên loại phép)
    // - Trả về danh sách quỹ phép cho Admin/HR
    public async Task<PagedResult<LeaveBalanceResponseDto>> GetAllBalancesAsync(LeaveBalanceFilterRequestDTO request, int? year, int? departmentId)
    {
        int currentYear = year ?? DateTime.UtcNow.Year;

        var query = _context.LeaveBalances
            .AsNoTracking()
            .Where(lb => lb.Year == currentYear);

        if (departmentId.HasValue)
        {
            query = query.Where(lb => lb.Employee.DepartmentId == departmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(lb => lb.Employee.FullName.ToLower().Contains(term) ||
                                      lb.Employee.EmployeeCode.ToLower().Contains(term));
        }

        var dtoQuery = query
            .OrderBy(lb => lb.Employee.FullName)
            .ThenBy(lb => lb.LeaveType.Name)
            .Select(lb => new LeaveBalanceResponseDto(
                lb.Id,
                lb.EmployeeId,
                lb.Employee.FullName,
                lb.Employee.EmployeeCode,
                lb.LeaveTypeId,
                lb.LeaveType.Name,
                lb.LeaveType.IsPaid,
                lb.Year,
                lb.TotalDays,
                lb.UsedDays,
                lb.RemainingDays
            ));

        return await dtoQuery.ToPagedListAsync(request.PageIndex, request.PageSize);
    }

    // 3. Cấp quỹ phép cá nhân
    //
    // Luồng xử lý:
    // - Kiểm tra nhân viên và loại phép có tồn tại trong DB không
    // - Kiểm tra xem nhân viên đã có quỹ phép loại này trong năm đó chưa
    // - Nếu ĐÃ TỒN TẠI: Cập nhật lại TotalDays và tính lại RemainingDays = TotalDays - UsedDays
    // - Nếu CHƯA TỒN TẠI: Tạo bản ghi LeaveBalance mới (UsedDays = 0, RemainingDays = TotalDays)
    // - Lưu thay đổi vào Database và trả về DTO kết quả
    public async Task<(bool Success, string Message, LeaveBalanceResponseDto? Data)> AssignBalanceAsync(AssignLeaveBalanceDto dto)
    {
        if (dto.TotalDays < 0)
            return (false, "Tổng số ngày phép không được nhỏ hơn 0.", null);

        var employee = await _context.Employees.FindAsync(dto.EmployeeId);
        if (employee == null) return (false, "Nhân viên không tồn tại.", null);

        var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
        if (leaveType == null) return (false, "Loại phép không tồn tại.", null);

        // NGHỈ KHÔNG PHÉP / KHÔNG HƯỞNG LƯƠNG: Không tạo LeaveBalance
        if (!leaveType.IsPaid)
        {
            return (false, $"Loại phép '{leaveType.Name}' là nghỉ không hưởng lương, không cần quản lý quỹ phép.", null);
        }

        // Include sẵn Employee và LeaveType để tránh NullReferenceException khi map DTO
        var existing = await _context.LeaveBalances
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == dto.EmployeeId &&
                lb.LeaveTypeId == dto.LeaveTypeId &&
                lb.Year == dto.Year);

        if (existing != null)
        {
            existing.TotalDays = dto.TotalDays;
            existing.RemainingDays = existing.TotalDays - existing.UsedDays;

            await _context.SaveChangesAsync();
            return (true, "Đã cập nhật lại quỹ phép cho nhân viên.", MapToLeaveBalanceDTO(existing));
        }

        var balance = new LeaveBalance
        {
            EmployeeId = dto.EmployeeId,
            LeaveTypeId = dto.LeaveTypeId,
            Year = dto.Year,
            TotalDays = dto.TotalDays,
            UsedDays = 0,
            RemainingDays = dto.TotalDays
        };

        _context.LeaveBalances.Add(balance);
        await _context.SaveChangesAsync();

        // Reload Navigation Property để map DTO không bị null
        await _context.Entry(balance).Reference(b => b.Employee).LoadAsync();
        await _context.Entry(balance).Reference(b => b.LeaveType).LoadAsync();

        return (true, "Cấp quỹ phép thành công.", MapToLeaveBalanceDTO(balance));
    }

    // 4. Cấp quỹ phép hàng loạt cho TẤT CẢ nhân viên đang hoạt động (ACTIVE)
    //
    // Luồng xử lý:
    // - Lấy danh sách tất cả nhân viên có trạng thái STATUS = "ACTIVE"
    // - Khai báo 2 biến đếm: createdCount (số bản ghi tạo mới) và updatedCount (số bản ghi cập nhật)
    // - Duyệt từng nhân viên:
    //     + Nếu đã có quỹ phép loại đó trong năm -> Cập nhật TotalDays & RemainingDays (tăng updatedCount)
    //     + Nếu chưa có -> Tạo mới bản ghi LeaveBalance (tăng createdCount)
    // - Lưu tất cả thay đổi vào Database trong một đợt SaveChangesAsync()
    // - Trả về số lượng bản ghi đã tạo mới và đã cập nhật
    // 4. Cấp quỹ phép hàng loạt (Đã tối ưu - Giải quyết lỗi N+1 Query)
    public async Task<(int CreatedCount, int UpdatedCount)> BulkAssignAsync(BulkAssignLeaveBalanceDto dto)
    {
        if (dto.TotalDays < 0)
            throw new ArgumentException("Tổng số ngày phép không được nhỏ hơn 0.");

        var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
        if (leaveType == null || !leaveType.IsPaid)
        {
            // Nếu là loại phép không hưởng lương thì bỏ qua không cấp quỹ
            return (0, 0);
        }

        // Lấy toàn bộ danh sách nhân viên ACTIVE
        var activeEmployeeIds = await _context.Employees
            .Where(e => e.Status == "ACTIVE")
            .Select(e => e.Id)
            .ToListAsync();

        if (!activeEmployeeIds.Any()) return (0, 0);

        // Query 1 lần duy nhất lấy toàn bộ Balance hiện có trong năm của các nhân viên trên
        var existingBalances = await _context.LeaveBalances
            .Where(lb => lb.LeaveTypeId == dto.LeaveTypeId &&
                         lb.Year == dto.Year &&
                         activeEmployeeIds.Contains(lb.EmployeeId))
            .ToDictionaryAsync(lb => lb.EmployeeId);

        int createdCount = 0;
        int updatedCount = 0;

        foreach (var empId in activeEmployeeIds)
        {
            if (existingBalances.TryGetValue(empId, out var existing))
            {
                existing.TotalDays = dto.TotalDays;
                existing.RemainingDays = existing.TotalDays - existing.UsedDays;
                updatedCount++;
            }
            else
            {
                var balance = new Models.LeaveBalance
                {
                    EmployeeId = empId,
                    LeaveTypeId = dto.LeaveTypeId,
                    Year = dto.Year,
                    TotalDays = dto.TotalDays,
                    UsedDays = 0,
                    RemainingDays = dto.TotalDays
                };
                _context.LeaveBalances.Add(balance);
                createdCount++;
            }
        }

        await _context.SaveChangesAsync();
        return (createdCount, updatedCount);
    }

    // Helper Expression dùng cho IQueryable Select
    private static System.Linq.Expressions.Expression<Func<LeaveBalance, LeaveBalanceResponseDto>> MapToDtoExpression(LeaveBalance lb)
    {
        return lb => new LeaveBalanceResponseDto(
            lb.Id,
            lb.EmployeeId,
            lb.Employee.FullName,
            lb.Employee.EmployeeCode,
            lb.LeaveTypeId,
            lb.LeaveType.Name,
            lb.LeaveType.IsPaid,
            lb.Year,
            lb.TotalDays,
            lb.UsedDays,
            lb.RemainingDays
        );
    }

    // Helper Mapping Entity -> DTO cho bối cảnh In-Memory Object
    public static LeaveBalanceResponseDto MapToLeaveBalanceDTO(LeaveBalance lt)
    {
        return new LeaveBalanceResponseDto(
            lt.Id,
            lt.EmployeeId,
            lt.Employee?.FullName ?? string.Empty,
            lt.Employee?.EmployeeCode ?? string.Empty,
            lt.LeaveTypeId,
            lt.LeaveType?.Name ?? string.Empty,
            lt.LeaveType?.IsPaid ?? true,
            lt.Year,
            lt.TotalDays,
            lt.UsedDays,
            lt.RemainingDays
        );
    }

}