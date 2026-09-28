
using BackendApp.Data;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Extensions;
using BackendApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Services;

public class LeaveRequestService
{
    private readonly AppDbContext _dbContext;

    public LeaveRequestService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// 1. Employee tạo đơn xin nghỉ phép
    //
    // Luồng xử lý & Business Rules:
    // - Kiểm tra tính hợp lệ của ngày xin nghỉ (EndDate >= StartDate)
    // - Tính tổng số ngày xin nghỉ (TotalRequestedDays = EndDate - StartDate + 1)
    // - Tìm loại phép trong Database để xác định có phải "Nghỉ không lương" hay không
    // - BUSINESS RULE 1 (Kiểm tra quỹ phép):
    //     + Nếu là Phép CÓ HƯỞNG LƯƠNG -> Kiểm tra LeaveBalance, nếu không có hoặc TotalRequestedDays > RemainingDays thì BÁO LỖI
    //     + Nếu là NGHỈ KHÔNG LƯƠNG -> BỎ QUA bước kiểm tra số dư quỹ phép
    // - BUSINESS RULE 2 (Overlap Check):
    //     + Kiểm tra xem khoảng thời gian [StartDate, EndDate] có bị trùng với đơn nào khác (không phải trạng thái REJECTED) không
    // - Nếu hợp lệ -> Tạo đơn LeaveRequest mới với Status = "PENDING"
    // - Lưu đơn vào Database và trả về DTO thông tin đơn vừa tạo
    public async Task<(bool Success, string Message, LeaveRequestResponseDto? Data)> CreateLeaveRequestAsync(int employeeId, CreateLeaveRequestDto dto)
    {
        if (dto.EndDate < dto.StartDate)
            return (false, "Ngày kết thúc không được nhỏ hơn ngày bắt đầu.", null);

        decimal totalRequestedDays = (decimal)(dto.EndDate.Date - dto.StartDate.Date).TotalDays + 1;
        if (totalRequestedDays <= 0)
            return (false, "Số ngày xin nghỉ phải lớn hơn 0.", null);

        var leaveType = await _dbContext.LeaveTypes.FindAsync(dto.LeaveTypeId);
        if (leaveType == null)
            return (false, "Loại phép không tồn tại trong hệ thống.", null);

        int currentYear = dto.StartDate.Year;

        // Xử lý logic Nghỉ không lương
        bool isUnpaidLeave = leaveType.Name.ToLower().Contains("không lương")
                          || leaveType.Name.ToLower().Contains("unpaid");

        // --- BUSINESS RULE 1: Kiểm tra số dư quỹ phép (Chỉ áp dụng với phép CÓ HƯỞNG LƯƠNG) ---
        if (!isUnpaidLeave)
        {
            var balance = await _dbContext.LeaveBalances
                .FirstOrDefaultAsync(lb =>
                    lb.EmployeeId == employeeId &&
                    lb.LeaveTypeId == dto.LeaveTypeId &&
                    lb.Year == currentYear);

            if (balance == null)
                return (false, $"Bạn chưa được cấp quỹ phép cho loại phép '{leaveType.Name}' trong năm {currentYear}.", null);

            if (totalRequestedDays > balance.RemainingDays)
                return (false, $"Số ngày xin nghỉ ({totalRequestedDays} ngày) vượt quá số ngày phép có lương còn lại ({balance.RemainingDays} ngày).", null);
        }

        // --- BUSINESS RULE 2: Overlap Date Validation (Chống trùng lịch cho MỌI loại phép) ---
        bool isOverlapping = await _dbContext.LeaveRequests
            .AnyAsync(lr => lr.EmployeeId == employeeId
                         && lr.Status != "REJECTED"
                         && dto.StartDate.Date <= lr.EndDate.Date
                         && dto.EndDate.Date >= lr.StartDate.Date);

        if (isOverlapping)
            return (false, "Khoảng thời gian xin nghỉ bị trùng với một đơn nghỉ phép khác đã tạo trước đó.", null);

        // Tạo đơn nghỉ phép với trạng thái PENDING
        var request = new LeaveRequest
        {
            EmployeeId = employeeId,
            LeaveTypeId = dto.LeaveTypeId,
            StartDate = dto.StartDate.Date,
            EndDate = dto.EndDate.Date,
            TotalRequestedDays = totalRequestedDays,
            Reason = dto.Reason,
            Status = "PENDING",
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.LeaveRequests.Add(request);
        await _dbContext.SaveChangesAsync();

        var employee = await _dbContext.Employees.FindAsync(employeeId);

        var resultDto = new LeaveRequestResponseDto(
            request.Id, employeeId, employee!.FullName, employee.EmployeeCode,
            leaveType.Id, leaveType.Name, request.StartDate, request.EndDate,
            request.TotalRequestedDays, request.Reason, request.Status,
            null, null, null, request.CreatedAt
        );

        return (true, "Gửi đơn xin nghỉ phép thành công.", resultDto);
    }

    // 2. Employee xem lịch sử đơn xin nghỉ của mình
    //
    // Luồng xử lý:
    // - Nhận employeeId của nhân viên đang đăng nhập
    // - Truy vấn bảng LeaveRequests lọc chính xác theo EmployeeId
    // - Include các thông tin liên kết: Employee, LeaveType, Approver (Người duyệt đơn)
    // - Sắp xếp danh sách đơn giảm dần theo thời gian tạo (CreatedAt)
    // - Mapping dữ liệu thu được sang dạng LeaveRequestResponseDto và trả về
    public async Task<PagedResult<LeaveRequestResponseDto>> GetMyLeaveRequestsAsync(
    LeaveRequestFilterRequestDTO request,
    int employeeId)
    {
        var query = _dbContext.LeaveRequests
            .AsNoTracking()
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .Include(lr => lr.Approver)
            .Where(lr => lr.EmployeeId == employeeId);

        var dtoQuery = query
            .OrderByDescending(lr => lr.CreatedAt)
            .Select(lr => new LeaveRequestResponseDto(
                lr.Id,
                lr.EmployeeId,
                lr.Employee.FullName,
                lr.Employee.EmployeeCode,
                lr.LeaveTypeId,
                lr.LeaveType.Name,
                lr.StartDate,
                lr.EndDate,
                lr.TotalRequestedDays,
                lr.Reason,
                lr.Status,
                lr.ApprovedBy,
                lr.Approver != null ? lr.Approver.FullName : null,
                lr.RejectionReason,
                lr.CreatedAt
            ));

        return await dtoQuery.ToPagedListAsync(
            request.PageIndex,
            request.PageSize
        );
    }

    // 3. Manager xem đơn PENDING của nhân viên thuộc Phòng ban quản lý HOẶC cấp dưới trực tiếp; HR/Admin xem tất cả
    //
    // Đã cập nhật logic kiểm tra phân quyền:
    // - Nếu LÀ HR/Admin: Lấy toàn bộ đơn PENDING.
    // - Nếu KHÔNG PHẢI HR/Admin (Manager): Chỉ xem đơn của nhân viên mà:
    //     + Manager là Người quản lý trực tiếp (lr.Employee.ManagerId == currentUserId)
    //     + HOẶC Manager là Trưởng phòng của phòng ban đó (lr.Employee.Department.ManagerId == currentUserId)
    public async Task<PagedResult<LeaveRequestResponseDto>> GetPendingLeaveRequestsAsync(
    LeaveRequestFilterRequestDTO request,
    int currentUserId,
    bool isHRorAdmin)
    {
        var query = _dbContext.LeaveRequests
       .AsNoTracking()
       .Include(lr => lr.Employee)
           .ThenInclude(e => e.Department)
       .Include(lr => lr.LeaveType)
       .Where(lr => lr.Status == "PENDING");

        // Manager chỉ xem đơn của cấp dưới
        // Không cho Manager tự duyệt đơn của chính mình
        if (!isHRorAdmin)
        {
            query = query.Where(lr =>
                lr.EmployeeId != currentUserId &&
                (
                    lr.Employee.ManagerId == currentUserId ||
                    (
                        lr.Employee.Department != null &&
                        lr.Employee.Department.ManagerId == currentUserId
                    )
                ));
        }

        var dtoQuery = query
            .OrderByDescending(lr => lr.CreatedAt)
            .Select(lr => new LeaveRequestResponseDto(
                lr.Id,
                lr.EmployeeId,
                lr.Employee.FullName,
                lr.Employee.EmployeeCode,
                lr.LeaveTypeId,
                lr.LeaveType.Name,
                lr.StartDate,
                lr.EndDate,
                lr.TotalRequestedDays,
                lr.Reason,
                lr.Status,
                lr.ApprovedBy,
                null,
                lr.RejectionReason,
                lr.CreatedAt
            ));

        return await dtoQuery.ToPagedListAsync(
            request.PageIndex,
            request.PageSize
        );
    }


    // 4. Manager/HR Phê duyệt hoặc Từ chối đơn xin nghỉ
    //
    // Luồng xử lý & Business Rules:
    // - Tìm đơn xin nghỉ theo requestId kèm thông tin Nhân viên và Loại phép
    // - Kiểm tra đơn có tồn tại và đang ở trạng thái "PENDING" không
    // - BUSINESS RULE 3: Kiểm tra người duyệt nếu không phải HR/Admin thì có đúng là Manager trực tiếp (ManagerId) không
    // - Bắt đầu Database Transaction để đảm bảo tính nhất quán dữ liệu
    // - Nếu Phê duyệt (IsApproved = true):
    //     + Nếu là Phép CÓ HƯỞNG LƯƠNG -> Kiểm tra quỹ phép, cộng UsedDays và trừ RemainingDays trong LeaveBalance
    //     + Nếu là NGHỈ KHÔNG LƯƠNG -> Không trừ vào quỹ phép (chỉ cập nhật UsedDays nếu có bản ghi)
    //     + Đổi Status đơn thành "APPROVED" và lưu ApprovedBy = approverId
    // - Nếu Từ chối (IsApproved = false):
    //     + Bắt buộc phải có RejectionReason (Lý do từ chối không được để trống)
    //     + Đổi Status đơn thành "REJECTED", lưu ApprovedBy và RejectionReason
    // - SaveChangesAsync() và Commit Transaction
    // - Nếu có lỗi bất kỳ -> Rollback Transaction và báo lỗi hệ thống
    public async Task<(bool Success, string Message)> ProcessLeaveRequestAsync(int requestId, int approverId, bool isHRorAdmin, ApproveLeaveRequestDto dto)
    {
        var request = await _dbContext.LeaveRequests
            .Include(lr => lr.Employee)
            .Include(lr => lr.LeaveType)
            .FirstOrDefaultAsync(lr => lr.Id == requestId);

        if (request == null)
            return (false, "Không tìm thấy đơn xin nghỉ phép.");

        if (request.Status != "PENDING")
            return (false, "Đơn xin nghỉ này đã được xử lý trước đó.");

        // 1. Kiểm tra đơn này có phải do chính người duyệt tự tạo hay không
        bool isSelfApproval = request.EmployeeId == approverId;

        if (isSelfApproval && !isHRorAdmin)
        {
            return (false, "Bạn không thể tự phê duyệt đơn xin nghỉ phép của chính mình. Đơn này cần được duyệt bởi Admin hoặc Quản lý cấp cao hơn.");
        }

        // 2. Kiểm tra quyền duyệt với nhân viên khác
        bool isDirectManager = request.Employee.ManagerId == approverId;
        bool isDepartmentManager = request.Employee.Department != null && request.Employee.Department.ManagerId == approverId;

        if (!isHRorAdmin && !isDirectManager && !isDepartmentManager)
        {
            return (false, "Bạn không có quyền duyệt đơn của nhân viên này (không thuộc cấp dưới trực tiếp hoặc phòng ban do bạn quản lý).");
        }
        using var transaction = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            if (dto.IsApproved)
            {
                bool isUnpaidLeave = request.LeaveType.Name.ToLower().Contains("không lương")
                                  || request.LeaveType.Name.ToLower().Contains("unpaid");

                int year = request.StartDate.Year;
                var balance = await _dbContext.LeaveBalances
                    .FirstOrDefaultAsync(lb => lb.EmployeeId == request.EmployeeId && lb.LeaveTypeId == request.LeaveTypeId && lb.Year == year);

                // Nếu là Phép CÓ HƯỞNG LƯƠNG -> Bắt buộc kiểm tra và trừ quỹ phép
                if (!isUnpaidLeave)
                {
                    if (balance == null || balance.RemainingDays < request.TotalRequestedDays)
                    {
                        return (false, "Quỹ phép có lương của nhân viên không đủ để duyệt đơn này.");
                    }

                    balance.UsedDays += request.TotalRequestedDays;
                    balance.RemainingDays = balance.TotalDays - balance.UsedDays;
                }
                else if (balance != null)
                {
                    // Nếu là NGHỈ KHÔNG LƯƠNG -> Chỉ ghi nhận số ngày đã nghỉ để phục vụ báo cáo
                    balance.UsedDays += request.TotalRequestedDays;
                }

                request.Status = "APPROVED";
                request.ApprovedBy = approverId;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                    return (false, "Bắt buộc phải nhập lý do khi từ chối đơn xin nghỉ.");

                request.Status = "REJECTED";
                request.ApprovedBy = approverId;
                request.RejectionReason = dto.RejectionReason;
            }

            await _dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            string statusMsg = dto.IsApproved ? "Phê duyệt" : "Từ chối";
            return (true, $"{statusMsg} đơn xin nghỉ phép thành công.");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi hệ thống khi xử lý đơn: {ex.Message}");
        }
    }

    // 5. Thống kê tổng số ngày nghỉ không lương của nhân viên theo Tháng/Năm
    //
    // Luồng xử lý:
    // - Lọc các đơn xin nghỉ có trạng thái APPROVED
    // - Lọc loại phép có tên chứa từ khóa "không lương" hoặc "unpaid"
    // - Lọc theo Tháng và Năm của ngày bắt đầu nghỉ (StartDate)
    // - Nếu truyền employeeId cụ thể -> Lọc cho 1 nhân viên; Nếu không -> Thống kê cho tất cả nhân viên
    // - Nhóm (Group By) theo Nhân viên để tính tổng (SUM) số ngày đã nghỉ (TotalRequestedDays)
    // - Trả về danh sách UnpaidLeaveSummaryDto cho HR/Kế toán chốt lương
    public async Task<List<UnpaidLeaveSummaryDto>> GetUnpaidLeaveSummaryAsync(int month, int year, int? employeeId = null, int? departmentId = null)
    {
        var query = _dbContext.LeaveRequests
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Department)
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.Status == "APPROVED"
                      && (lr.LeaveType.Name.ToLower().Contains("không lương") || lr.LeaveType.Name.ToLower().Contains("unpaid"))
                      && lr.StartDate.Month == month
                      && lr.StartDate.Year == year);

        if (employeeId.HasValue)
        {
            query = query.Where(lr => lr.EmployeeId == employeeId.Value);
        }

        if (departmentId.HasValue)
        {
            query = query.Where(lr => lr.Employee.DepartmentId == departmentId.Value);
        }

        var result = await query
            .GroupBy(lr => new
            {
                lr.EmployeeId,
                lr.Employee.EmployeeCode,
                lr.Employee.FullName,
                DepartmentName = lr.Employee.Department != null ? lr.Employee.Department.Name : null
            })
            .Select(g => new UnpaidLeaveSummaryDto(
                g.Key.EmployeeId,
                g.Key.EmployeeCode,
                g.Key.FullName,
                g.Key.DepartmentName,
                month,
                year,
                g.Sum(lr => lr.TotalRequestedDays)
            ))
            .ToListAsync();

        return result;
    }
}