
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
        // Ưu tiên lấy Claim Custom "EmployeeId", nếu không có mới lấy NameIdentifier
        var employeeIdClaim = User.FindFirst("EmployeeId")?.Value
                              ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(employeeIdClaim) || !int.TryParse(employeeIdClaim, out int employeeId))
        {
            return Unauthorized(new { message = "Không xác định được thông tin nhân viên từ token." });
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
        if (dto == null)
            return BadRequest(new { message = "Dữ liệu không hợp lệ." });

        try
        {
            var (createdCount, updatedCount) = await _service.BulkAssignAsync(dto);

            return Ok(new
            {
                message = $"Khởi tạo quỹ phép năm {dto.Year} hoàn tất.",
                createdCount,
                updatedCount
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Lỗi hệ thống khi cấp phép hàng loạt: {ex.Message}" });
        }
    }

}