using BackendApp.Data;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Extensions;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Services;

public class LeaveBalanceService
{
    private readonly AppDbContext _context;

    public LeaveBalanceService(AppDbContext context)
    {
        _context = context;
    }

    // 1. Xem quỹ phép của Nhân viên đang đăng nhập (Employee Self-Service)
    public async Task<PagedResult<LeaveBalanceResponseDto>> GetMyBalancesAsync(LeaveBalanceFilterRequestDTO request, int currentEmployeeId, int? year)
    {
        int currentYear = year ?? DateTime.UtcNow.Year;

        var query = _context.LeaveBalances
        .AsNoTracking()
        .Include(lb => lb.Employee)
        .Include(lb => lb.LeaveType)
        .Where(lb =>
            lb.EmployeeId == currentEmployeeId &&
            lb.Year == currentYear
        );


        var dtoQuery = query
        .OrderBy(lb => lb.LeaveType.Name)
        .Select(lb => new LeaveBalanceResponseDto(
            lb.Id,
            lb.EmployeeId,
            lb.Employee.FullName,
            lb.Employee.EmployeeCode,
            lb.LeaveTypeId,
            lb.LeaveType.Name,
            lb.Year,
            lb.TotalDays,
            lb.UsedDays,
            lb.RemainingDays
        ));

        // Thực thi phân trang
        return await dtoQuery.ToPagedListAsync(
            request.PageIndex,
            request.PageSize
        );
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
        // - Nếu có truyền year -> sử dụng year được truyền vào
        // - Nếu không truyền year -> mặc định lấy năm hiện tại
        int currentYear = year ?? DateTime.UtcNow.Year;


        var query = _context.LeaveBalances
            .AsNoTracking()
            .Include(lb => lb.Employee)
            .Include(lb => lb.LeaveType)
            .Where(lb => lb.Year == currentYear);

        // -> Chỉ lấy quỹ phép của nhân viên thuộc phòng ban đó
        // Nếu không truyền departmentId
        // -> Giữ nguyên danh sách của tất cả phòng ban
        if (departmentId.HasValue)
        {
            query = query.Where(lb => lb.Employee.DepartmentId == departmentId.Value);
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
            lb.Year,
            lb.TotalDays,
            lb.UsedDays,
            lb.RemainingDays
        ));

        // Thực thi phân trang
        return await dtoQuery.ToPagedListAsync(
            request.PageIndex,
            request.PageSize
        );
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
        // Check nhân viên & loại phép
        var employee = await _context.Employees.FindAsync(dto.EmployeeId);
        if (employee == null) return (false, "Nhân viên không tồn tại.", null);

        var leaveType = await _context.LeaveTypes.FindAsync(dto.LeaveTypeId);
        if (leaveType == null) return (false, "Loại phép không tồn tại.", null);

        // Kiểm tra nhân viên đã được cấp quỹ phép
        // của loại phép này trong năm đó hay chưa
        //
        // Điều kiện kiểm tra:
        // - Đúng nhân viên
        // - Đúng loại phép
        // - Đúng năm
        var existing = await _context.LeaveBalances
            .FirstOrDefaultAsync(lb =>
            lb.EmployeeId == dto.EmployeeId &&
            lb.LeaveTypeId == dto.LeaveTypeId &&
            lb.Year == dto.Year);

        if (existing != null)
        {
            // Nếu đã có -> Cập nhật tổng số ngày phép
            existing.TotalDays = dto.TotalDays;
            // Tính lại số ngày phép còn lại
            // = Tổng ngày phép - Số ngày đã sử dụng
            existing.RemainingDays = existing.TotalDays - existing.UsedDays;

            await _context.SaveChangesAsync();

            var updatedDto = MapToLeaveBalanceDTO(existing);

            return (true, "Đã cập nhật lại quỹ phép cho nhân viên.", updatedDto);
        }

        // Tạo mới quỹ phép
        var balance = new Models.LeaveBalance
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
    public async Task<(int CreatedCount, int UpdatedCount)> BulkAssignAsync(BulkAssignLeaveBalanceDto dto)
    {
        var activeEmployees = await _context.Employees
            .Where(e => e.Status == "ACTIVE")
            .ToListAsync();

        // CreatedCount: số quỹ phép được tạo mới
        // UpdatedCount: số quỹ phép đã được cập nhật
        int createdCount = 0;
        int updatedCount = 0;

        foreach (var emp in activeEmployees)
        {
            var existing = await _context.LeaveBalances
                .FirstOrDefaultAsync(lb =>
                lb.EmployeeId == emp.Id &&
                lb.LeaveTypeId == dto.LeaveTypeId &&
                lb.Year == dto.Year);

            // Nếu đã tồn tại quỹ phép
            // -> Không tạo bản ghi mới
            // -> Cập nhật lại tổng số ngày phép
            if (existing != null)
            {
                existing.TotalDays = dto.TotalDays;
                existing.RemainingDays = existing.TotalDays - existing.UsedDays;
                updatedCount++;
            }
            else
            {
                var balance = new Models.LeaveBalance
                {
                    EmployeeId = emp.Id,
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

    // 5. Hàm Helper Mapping từ Entity LeaveBalance sang DTO
    public static LeaveBalanceResponseDto MapToLeaveBalanceDTO(Models.LeaveBalance lt)
    {
        return new LeaveBalanceResponseDto(
            lt.Id,
            lt.EmployeeId,
            lt.Employee.FullName,
            lt.Employee.EmployeeCode,
            lt.LeaveTypeId,
            lt.LeaveType.Name,
            lt.Year,
            lt.TotalDays,
            lt.UsedDays,
            lt.RemainingDays
        );
    }


}