using BackendApp.Configurations;
using BackendApp.DTOs;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = RolePolicySetup.Policies.Management)] // Chỉ Admin, HRManager và Manager có quyền xem Dashboard
public class DashboardController : ControllerBase
{
    private readonly DashboardService _dashboardService;

    public DashboardController(DashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // 1. Lấy chỉ số thống kê tổng quan (Summary Metrics)
    // GET: api/dashboard/summary
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummaryMetrics()
    {
        var result = await _dashboardService.GetSummaryMetricsAsync();
        return Ok(result);
    }

    // 2. Lấy thống kê phân bổ nhân sự theo phòng ban
    // GET: api/dashboard/department-counts
    [HttpGet("department-counts")]
    public async Task<IActionResult> GetDepartmentEmployeeCounts()
    {
        var result = await _dashboardService.GetDepartmentEmployeeCountsAsync();
        return Ok(result);
    }

    // 3. Lấy danh sách nhân viên đang nghỉ phép hôm nay
    // GET: api/dashboard/on-leave-today
    [HttpGet("on-leave-today")]
    public async Task<IActionResult> GetEmployeesOnLeaveToday()
    {
        var result = await _dashboardService.GetEmployeesOnLeaveTodayAsync();
        return Ok(result);
    }

    // 4. Thống kê tình trạng đơn nghỉ phép trong tháng
    // GET: api/dashboard/monthly-leave-stats?month=10&year=2026
    [HttpGet("monthly-leave-stats")]
    public async Task<IActionResult> GetMonthlyLeaveStats([FromQuery] int? month, [FromQuery] int? year)
    {
        var result = await _dashboardService.GetMonthlyLeaveStatsAsync(month, year);
        return Ok(result);
    }
}