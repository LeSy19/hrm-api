
using System.Security.Claims;
using BackendApp.Configurations;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveBalancesController : ControllerBase
{
    private readonly LeaveBalanceService _service;

    public LeaveBalancesController(LeaveBalanceService service)
    {
        _service = service;
    }

    // Employee xem quỹ phép của chính mình
    [HttpGet("my-balance")]
    [Authorize(Policy = RolePolicySetup.Policies.StaffAccess)]
    public async Task<ActionResult<PagedResult<LeaveBalanceResponseDto>>> GetMyBalance(
    [FromQuery] LeaveBalanceFilterRequestDTO request,
    [FromQuery] int? year)
    {
        // Lấy UserId từ Claim trong JWT Token
        // NameIdentifier chứa ID của nhân viên đang đăng nhập
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        // Kiểm tra UserId có tồn tại và có chuyển được sang kiểu int hay không
        // Nếu không hợp lệ -> không xác định được danh tính người dùng
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int employeeId))
        {
            return Unauthorized(new { message = "Không xác định được danh tính người dùng." });
        }

        var result = await _service.GetMyBalancesAsync(
        request,
        employeeId,
        year
    );

        return Ok(result);
    }

    // HR / Admin xem danh sách quỹ phép toàn công ty
    [HttpGet]
    [Authorize(Policy = RolePolicySetup.Policies.Management)]
    public async Task<ActionResult<PagedResult<LeaveBalanceResponseDto>>> GetAllBalances(
    [FromQuery] LeaveBalanceFilterRequestDTO request,
    [FromQuery] int? year,
    [FromQuery] int? departmentId)
    {
        var result = await _service.GetAllBalancesAsync(
        request,
        year,
        departmentId
    );

        return Ok(result);
    }

    // HR / Admin cấp phép lẻ cho 1 nhân viên
    [HttpPost("assign")]
    [Authorize(Policy = RolePolicySetup.Policies.Management)]
    public async Task<IActionResult> AssignBalance([FromBody] AssignLeaveBalanceDto dto)
    {
        var (success, message, data) = await _service.AssignBalanceAsync(dto);
        if (!success) return BadRequest(new { message });
        return Ok(new { message, data });
    }

    // 4. HR / Admin cấp phép hàng loạt đầu năm cho tất cả nhân viên
    [HttpPost("bulk-assign")]
    [Authorize(Policy = RolePolicySetup.Policies.Management)]
    public async Task<IActionResult> BulkAssignBalance([FromBody] BulkAssignLeaveBalanceDto dto)
    {
        var (createdCount, updatedCount) = await _service.BulkAssignAsync(dto);
        return Ok(new
        {
            message = $"Khởi tạo quỹ phép năm {dto.Year} hoàn tất.",
            createdCount,
            updatedCount
        });
    }

}