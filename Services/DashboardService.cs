
using BackendApp.Data;
using BackendApp.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Services;

public class DashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    // 1. Thống kê số liệu tổng quan (Summary Metrics)
    //
    // Luồng xử lý:
    // - Đếm tổng số nhân viên đang hoạt động (Status = "ACTIVE")
    // - Đếm tổng số nhân viên thử việc (Status = "PROBATION")
    // - Đếm tổng số phòng ban trong công ty
    // - Lấy ngày hiện tại (Today) và đếm số nhân viên có đơn nghỉ phép APPROVED bao phủ ngày hôm nay
    public async Task<DashboardSummaryDto> GetSummaryMetricsAsync()
    {
        var today = DateTime.UtcNow.Date;

        int totalActive = await _context.Employees
            .CountAsync(e => e.Status == "ACTIVE");

        int totalProbation = await _context.Employees
            .CountAsync(e => e.Status == "PROBATION");

        int totalDepartments = await _context.Departments.CountAsync();

        int employeesOnLeaveToday = await _context.LeaveRequests
            .Where(lr => lr.Status == "APPROVED"
                      && lr.StartDate.Date <= today
                      && lr.EndDate.Date >= today)
            .Select(lr => lr.EmployeeId)
            .Distinct()
            .CountAsync();

        return new DashboardSummaryDto(totalActive, totalProbation, totalDepartments, employeesOnLeaveToday);
    }

    // 2. Thống kê phân bổ nhân sự theo phòng ban
    //
    // Luồng xử lý:
    // - Group nhân viên theo DepartmentId
    // - Tính tổng số nhân viên trong từng phòng ban
    // - Trả về danh sách DepartmentEmployeeCountDto
    public async Task<List<DepartmentEmployeeCountDto>> GetDepartmentEmployeeCountsAsync()
    {
        return await _context.Departments
            .Select(d => new DepartmentEmployeeCountDto(
                d.Id,
                d.Code,
                d.Name,
                d.Employees.Count(e => e.Status == "ACTIVE" || e.Status == "PROBATION")
            ))
            .ToListAsync();
    }

    // 3. Danh sách nhân viên đang nghỉ phép hôm nay
    //
    // Luồng xử lý:
    // - Lọc các đơn xin nghỉ phép có trạng thái APPROVED
    // - Điều kiện: StartDate <= Today <= EndDate
    // - Include thông tin Nhân viên, Phòng ban và Loại phép
    // - Trả về danh sách EmployeeOnLeaveTodayDto
    public async Task<List<EmployeeOnLeaveTodayDto>> GetEmployeesOnLeaveTodayAsync()
    {
        var today = DateTime.UtcNow.Date;

        return await _context.LeaveRequests
            .Include(lr => lr.Employee)
                .ThenInclude(e => e.Department)
            .Include(lr => lr.LeaveType)
            .Where(lr => lr.Status == "APPROVED"
                      && lr.StartDate.Date <= today
                      && lr.EndDate.Date >= today)
            .Select(lr => new EmployeeOnLeaveTodayDto(
                lr.EmployeeId,
                lr.Employee.EmployeeCode,
                lr.Employee.FullName,
                lr.Employee.Department != null ? lr.Employee.Department.Name : "Chưa xếp phòng",
                lr.LeaveType.Name,
                lr.StartDate,
                lr.EndDate,
                lr.TotalRequestedDays
            ))
            .ToListAsync();
    }

    // 4. Thống kê trạng thái đơn nghỉ phép trong tháng
    //
    // Luồng xử lý:
    // - Nhận tham số Month và Year (mặc định tháng/năm hiện tại)
    // - Đếm số lượng đơn theo từng trạng thái: PENDING, APPROVED, REJECTED trong tháng đó
    public async Task<MonthlyLeaveStatsDto> GetMonthlyLeaveStatsAsync(int? month, int? year)
    {
        int targetMonth = month ?? DateTime.UtcNow.Month;
        int targetYear = year ?? DateTime.UtcNow.Year;

        var requestsInMonth = _context.LeaveRequests
            .Where(lr =>
            lr.StartDate.Month == targetMonth &&
            lr.StartDate.Year == targetYear);

        int totalPending = await requestsInMonth.CountAsync(lr => lr.Status == "PENDING");
        int totalApproved = await requestsInMonth.CountAsync(lr => lr.Status == "APPROVED");
        int totalRejected = await requestsInMonth.CountAsync(lr => lr.Status == "REJECTED");

        return new MonthlyLeaveStatsDto(targetMonth, targetYear, totalPending, totalApproved, totalRejected);
    }
}