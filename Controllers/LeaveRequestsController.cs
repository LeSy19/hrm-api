using System.Security.Claims;
using BackendApp.Configurations;
using BackendApp.Constants;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LeaveRequestsController : ControllerBase
{
    private readonly LeaveRequestService _leaveRequestService;

    public LeaveRequestsController(LeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }


    [HttpGet]
    public async Task<ActionResult<PagedResult<LeaveRequestResponseDto>>> GetAllRequests(
    [FromQuery] LeaveRequestFilterRequestDTO request)
    {
        int currentUserId = GetCurrentUserId();

        if (currentUserId == 0)
        {
            return Unauthorized(new
            {
                message = "Không xác định được danh tính người dùng từ Token."
            });
        }

        bool isAdmin = User.IsInRole(UserRoles.Admin);

        var requests = await _leaveRequestService.GetAllLeaveRequestsAsync(
            request,
            currentUserId,
            isAdmin
        );

        return Ok(requests);
    }

    // 1. Employee nộp đơn xin nghỉ phép
    [HttpPost]
    public async Task<IActionResult> CreateRequest([FromBody] CreateLeaveRequestDto dto)
    {
        int employeeId = GetCurrentUserId();
        if (employeeId == 0)
            return Unauthorized(new { message = "Không xác định được danh tính người dùng từ Token." });

        var (success, message, data) = await _leaveRequestService.CreateLeaveRequestAsync(employeeId, dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message, data });
    }

    // 2. Employee xem lịch sử danh sách đơn xin nghỉ của chính mình
    [HttpGet("my-requests")]
    public async Task<ActionResult<PagedResult<LeaveRequestResponseDto>>> GetMyRequests(
        [FromQuery] LeaveRequestFilterRequestDTO request)
    {
        int employeeId = GetCurrentUserId();

        if (employeeId == 0)
            return Unauthorized(new { message = "Không xác định được danh tính người dùng từ Token." });

        var requests = await _leaveRequestService.GetMyLeaveRequestsAsync(request, employeeId);
        return Ok(requests);
    }

    // 3. Manager/HR/Admin xem danh sách đơn đang chờ duyệt (PENDING)
    [HttpGet("request-pending")]
    public async Task<ActionResult<PagedResult<LeaveRequestResponseDto>>> GetPendingRequests(
        [FromQuery] LeaveRequestFilterRequestDTO request)
    {
        int currentUserId = GetCurrentUserId();
        if (currentUserId == 0)
            return Unauthorized(new { message = "Không xác định được danh tính người dùng từ Token." });

        bool isHRorAdmin = User.IsInRole(UserRoles.Admin) || User.IsInRole("HRManager") || User.IsInRole("HR");

        var pendingRequests = await _leaveRequestService.GetPendingLeaveRequestsAsync(
            request,
            currentUserId,
            isHRorAdmin
        );

        return Ok(pendingRequests);
    }

    // 4. Manager/HR/Admin Phê duyệt (APPROVED) hoặc Từ chối (REJECTED) đơn xin nghỉ
    [HttpPut("{id:int}/process")]
    public async Task<IActionResult> ProcessRequest(int id, [FromBody] ApproveLeaveRequestDto dto)
    {
        int approverId = GetCurrentUserId();
        if (approverId == 0)
            return Unauthorized(new { message = "Không xác định được danh tính người dùng từ Token." });

        bool isHRorAdmin = User.IsInRole(UserRoles.Admin) || User.IsInRole("HRManager") || User.IsInRole("HR");

        var (success, message) = await _leaveRequestService.ProcessLeaveRequestAsync(id, approverId, isHRorAdmin, dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // 5. Employee Hủy đơn xin nghỉ (Hoàn lại phép năm nếu đơn đã duyệt trước đó)
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> CancelRequest(int id)
    {
        int employeeId = GetCurrentUserId();
        if (employeeId == 0)
            return Unauthorized(new { message = "Không xác định được danh tính người dùng từ Token." });

        var (success, message) = await _leaveRequestService.CancelLeaveRequestAsync(id, employeeId);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // 6. HR/Admin/Kế toán xem thống kê tổng hợp số ngày nghỉ không lương trong tháng
    [HttpGet("unpaid-summary")]
    public async Task<ActionResult<PagedResult<UnpaidLeaveSummaryDto>>> GetUnpaidLeaveSummary(
        [FromQuery] int month,
        [FromQuery] int year,
        [FromQuery] int? employeeId = null,
        [FromQuery] int? departmentId = null)
    {
        if (month < 1 || month > 12)
            return BadRequest(new { message = "Tháng không hợp lệ. Giá trị phải nằm trong khoảng từ 1 đến 12." });

        if (year < 2000 || year > 2100)
            return BadRequest(new { message = "Năm không hợp lệ." });

        var summary = await _leaveRequestService.GetUnpaidLeaveSummaryAsync(month, year, employeeId, departmentId);
        return Ok(summary);
    }

    // --- Private Helper Method ---
    private int GetCurrentUserId()
    {
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(employeeIdClaim, out int userId) ? userId : 0;
    }
}