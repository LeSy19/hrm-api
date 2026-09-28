

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
public class LeaveRequestsController : ControllerBase
{
    private readonly LeaveRequestService _leaveRequestService;

    public LeaveRequestsController(LeaveRequestService leaveRequestService)
    {
        _leaveRequestService = leaveRequestService;
    }

    // 1. Employee nộp đơn xin nghỉ phép
    //
    // Quyền truy cập: StaffAccess (Tất cả nhân viên đã đăng nhập đều có quyền gửi đơn)
    //
    // Luồng xử lý:
    // - Lấy EmployeeId từ JWT Token thông qua ClaimTypes.NameIdentifier
    // - Gọi LeaveRequestService.CreateRequestAsync để xử lý tạo đơn và kiểm tra các Business Rules (Check ngày, Overlap, Số dư phép...)
    // - Trả về HTTP 200 OK nếu thành công hoặc HTTP 400 BadRequest kèm thông báo lỗi
    [HttpPost]
    [Authorize(Policy = RolePolicySetup.Policies.StaffAccess)]
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
    //
    // Quyền truy cập: StaffAccess (Tất cả nhân viên đã đăng nhập)
    //
    // Luồng xử lý:
    // - Trích xuất EmployeeId từ Claim của User đang thực hiện Request
    // - Gọi LeaveRequestService.GetMyRequestsAsync để lấy danh sách đơn nghỉ phép cá nhân
    // - Trả về danh sách LeaveRequestResponseDto sắp xếp giảm dần theo thời gian tạo
    [HttpGet("my-requests")]
    [Authorize(Policy = RolePolicySetup.Policies.StaffAccess)]
    public async Task<ActionResult<PagedResult<LeaveRequestResponseDto>>> GetMyRequests(
    [FromQuery] LeaveRequestFilterRequestDTO request)
    {
        int employeeId = GetCurrentUserId();

        if (employeeId == 0)
            return Unauthorized(new { message = "Không xác định được danh tính người dùng từ Token." });

        var requests = await _leaveRequestService.GetMyLeaveRequestsAsync(
      request,
      employeeId
  );

        return Ok(requests);
    }

    // 3. Manager/HR/Admin xem danh sách đơn đang chờ duyệt (PENDING)
    //
    // Quyền truy cập: Management (Admin, HRManager, Manager)
    //
    // Luồng xử lý:
    // - Lấy UserId của người đang đăng nhập
    // - Kiểm tra xem người dùng có vai trò là HRManager hoặc Admin hay không
    // - Gọi LeaveRequestService.GetPendingRequestsAsync:
    //     + Nếu là HR/Admin -> Trả về tất cả các đơn PENDING trong toàn công ty
    //     + Nếu là Manager -> Chỉ trả về các đơn PENDING của nhân viên cấp dưới trực tiếp (ManagerId)
    [HttpGet("request-pending")]
    [Authorize(Policy = RolePolicySetup.Policies.Management)]
    public async Task<ActionResult<PagedResult<LeaveRequestResponseDto>>> GetPendingRequests(
    [FromQuery] LeaveRequestFilterRequestDTO request)
    {
        int currentUserId = GetCurrentUserId();
        if (currentUserId == 0)
            return Unauthorized(new { message = "Không xác định được danh tính người dùng từ Token." });

        // Chỉ Admin mới thấy toàn bộ đơn trong công ty.
        // Manager sẽ được Service lọc theo ManagerId / Department.ManagerId.
        bool isHRorAdmin = User.IsInRole(UserRoles.Admin);

        var pendingRequests = await _leaveRequestService.GetPendingLeaveRequestsAsync(
            request,
            currentUserId,
            isHRorAdmin
        );

        return Ok(pendingRequests);
    }

    // 4. Manager/HR/Admin Phê duyệt (APPROVED) hoặc Từ chối (REJECTED) đơn xin nghỉ
    //
    // Quyền truy cập: Management (Admin, HRManager, Manager)
    //
    // Luồng xử lý:
    // - Lấy ApproverId từ JWT Token
    // - Xử lý kiểm tra vai trò HR/Admin hay Manager trực tiếp
    // - Gọi LeaveRequestService.ProcessRequestAsync để cập nhật trạng thái đơn, thực hiện trừ/cộng LeaveBalance qua Transaction
    // - Trả về HTTP 200 OK kèm thông báo kết quả xử lý thành công hoặc HTTP 400 BadRequest nếu vi phạm Business Rules
    [HttpPut("{id:int}/process")]
    [Authorize(Policy = RolePolicySetup.Policies.Management)]
    public async Task<IActionResult> ProcessRequest(int id, [FromBody] ApproveLeaveRequestDto dto)
    {
        int approverId = GetCurrentUserId();
        if (approverId == 0)
            return Unauthorized(new { message = "Không xác định được danh tính người dùng từ Token." });

        bool isHRorAdmin = User.IsInRole(UserRoles.Admin);

        var (success, message) = await _leaveRequestService.ProcessLeaveRequestAsync(id, approverId, isHRorAdmin, dto);

        if (!success)
            return BadRequest(new { message });

        return Ok(new { message });
    }

    // 5. HR/Admin/Kế toán xem thống kê tổng hợp số ngày nghỉ không lương trong tháng
    //
    // Quyền truy cập: HRManagement (Admin, HRManager)
    //
    // Luồng xử lý:
    // - Validate giá trị tháng đầu vào (Phải nằm trong khoảng từ 1 đến 12)
    // - Gọi LeaveRequestService.GetUnpaidLeaveSummaryAsync để lấy danh sách tổng số ngày nghỉ không lương đã duyệt trong tháng
    // - Hỗ trợ lọc tùy chọn theo EmployeeId hoặc DepartmentId
    // - Trả về danh sách UnpaidLeaveSummaryDto phục vụ việc chốt công và tính lương
    [HttpGet("unpaid-summary")]
    [Authorize(Policy = RolePolicySetup.Policies.Management)]
    public async Task<IActionResult> GetUnpaidLeaveSummary(
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
    // Hàm phụ trợ trích xuất EmployeeId từ ClaimTypes.NameIdentifier trong JWT Token
    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out int userId) ? userId : 0;
    }
}