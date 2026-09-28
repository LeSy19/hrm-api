using BackendApp.DTOs;
using BackendApp.DTOs.Common;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BackendApp.Configurations;

namespace BackendApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeesController : ControllerBase
{
    private readonly EmployeeService _employeeService;

    public EmployeesController(EmployeeService employeeService)
    {
        _employeeService = employeeService;
    }

    // GET: api/employees
    [HttpGet]
    [Authorize(Policy = RolePolicySetup.Policies.Management)] // Admin/Manager xem danh sách
    public async Task<ActionResult<PagedResult<EmployeeResponseDTO>>> GetAll([FromQuery] EmployeeFilterRequestDTO request)
    {
        var result = await _employeeService.GetAllEmployeesAsync(request);
        return Ok(result);
    }

    // GET: api/employees/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _employeeService.GetEmployeeByIdAsync(id);
        if (employee == null)
            return NotFound(new { message = $"Không tìm thấy Nhân viên có Id = {id}" });

        return Ok(employee);
    }

    // POST: api/employees
    [HttpPost]
    [Authorize(Policy = RolePolicySetup.Policies.Management)] // Admin/Manager tạo nhân viên
    public async Task<IActionResult> Create([FromBody] CreateEmployeeDTO dto)
    {
        try
        {
            var result = await _employeeService.CreateEmployeeAsync(dto);
            if (result == null)
                return BadRequest(new { message = "Username hoặc Email đã được sử dụng trong hệ thống." });

            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // PUT: api/employees/5
    [HttpPut("{id:int}")]
    [Authorize(Policy = RolePolicySetup.Policies.StaffAccess)] // Admin/Manager/Employee mới được cập nhật nhân viên
    public async Task<IActionResult> Update(int id, [FromBody] UpdateEmployeeDTO dto)
    {
        try
        {
            var result = await _employeeService.UpdateEmployeeAsync(id, dto);
            if (result == null)
                return NotFound(new { message = $"Không tìm thấy Nhân viên có Id = {id}" });

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // DELETE: api/employees/5
    [HttpDelete("{id:int}")]
    [Authorize(Policy = RolePolicySetup.Policies.AdminOnly)] // Chỉ Admin mới được xóa nhân viên
    public async Task<IActionResult> Delete(int id)
    {
        var success = await _employeeService.DeleteEmployeeAsync(id);
        if (!success)
            return NotFound(new { message = $"Không tìm thấy Nhân viên có Id = {id}" });

        return Ok(new { message = $"Đã xóa thành công Nhân viên có Id = {id}" });
    }
}