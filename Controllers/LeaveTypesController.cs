
using BackendApp.Configurations;
using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaveTypesController : ControllerBase
{
    private readonly LeaveTypeService _service;

    public LeaveTypesController(LeaveTypeService service)
    {
        _service = service;
    }

    // 1. Lấy danh sách tất cả các loại phép
    [HttpGet]
    [Authorize(Policy = RolePolicySetup.Policies.StaffAccess)]
    public async Task<ActionResult<PagedResult<LeaveTypeResponseDto>>> GetAllLeaveTypes([FromQuery] LeaveTypeFilterRequestDTO request)
    {
        var leaveTypes = await _service.GetAllLeaveTypesAsync(request);
        return Ok(leaveTypes);
    }

    // 2. Lấy loại phép theo Id
    [HttpGet("{id:int}")]
    [Authorize(Policy = RolePolicySetup.Policies.StaffAccess)]
    public async Task<ActionResult<LeaveTypeResponseDto>> GetLeaveTypeById(int id)
    {
        var res = await _service.GetLeaveTypeByIdAsync(id);
        return res == null ? NotFound(new { message = "Không tìm thấy loại phép." }) : Ok(res);
    }

    // 3. Tạo mới loại phép
    [HttpPost]
    [Authorize(Policy = RolePolicySetup.Policies.Management)]
    public async Task<IActionResult> Create([FromBody] CreateLeaveTypeDto dto)
    {
        try
        {
            var res = await _service.CreateLeaveTypeAsync(dto);
            if (res == null) return BadRequest(new { message = "Tên loại phép đã tồn tại trong hệ thống." });
            return CreatedAtAction(nameof(GetLeaveTypeById), new { id = res.Id }, res);
        }
        catch (ArgumentException ex)
        {

            return BadRequest(new { message = ex.Message }); ;
        }

    }

    // PUT: api/leavetypes/5 (Chỉ Admin & HR có quyền sửa)
    [HttpPut("{id:int}")]
    [Authorize(Policy = RolePolicySetup.Policies.Management)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateLeaveTypeDto dto)
    {
        try
        {
            var res = await _service.UpdateLeaveTypeAsync(id, dto);
            return res == null ? NotFound(new { message = "Không tìm thấy loại phép." }) : Ok(res);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/leavetypes/5 (Chỉ Admin có quyền xóa)
    [HttpDelete("{id:int}")]
    [Authorize(Policy = RolePolicySetup.Policies.AdminOnly)]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, message) = await _service.DeleteLeaveTypeAsync(id);
        return success ? Ok(new { message }) : BadRequest(new { message });
    }


}