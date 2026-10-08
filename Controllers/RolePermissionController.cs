using BackendApp.Configurations.Authorization;
using BackendApp.Constants;
using BackendApp.Data;
using BackendApp.DTOs;
using BackendApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = UserRoles.Admin)] // Chỉ ADMIN được quản lý phân quyền
[AuthenticatedOnly]                  // Không đưa controller này vào danh sách phân quyền
public class RolePermissionController : ControllerBase
{
    private readonly IRolePermissionService _service;
    private readonly AppDbContext _db;

    public RolePermissionController(IRolePermissionService service, AppDbContext db)
    {
        _service = service;
        _db = db;
    }

    /// <summary>Danh sách role để admin chọn role cần gán quyền.</summary>
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Id)
            .Select(r => new RoleDto { Id = r.Id, Name = r.Name, Description = r.Description })
            .ToListAsync();
        return Ok(roles);
    }

    /// <summary>Tất cả endpoint (permission) nhóm theo module/controller.</summary>
    [HttpGet("permissions")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var data = await _service.GetAllPermissionsGroupedByModuleAsync();
        return Ok(data);
    }

    /// <summary>Các permission mà một role đang được cấp.</summary>
    [HttpGet("roles/{roleId:int}/permissions")]
    public async Task<IActionResult> GetRolePermissions(int roleId)
    {
        var permissionIds = await _service.GetPermissionIdsByRoleIdAsync(roleId);
        return Ok(new { roleId, permissionIds });
    }

    /// <summary>Lưu danh sách permission cho role (ghi đè toàn bộ).</summary>
    [HttpPut("roles/{roleId:int}/permissions")]
    public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsDto dto)
    {
        try
        {
            await _service.UpdateRolePermissionsAsync(roleId, dto.PermissionIds);
            return Ok(new { message = "Cập nhật phân quyền thành công" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Đổi tên/mô tả hiển thị của một permission cho dễ hiểu.</summary>
    [HttpPut("permissions/{id:int}")]
    public async Task<IActionResult> UpdatePermissionInfo(int id, [FromBody] UpdatePermissionInfoDto dto)
    {
        var p = await _db.Permissions.FindAsync(id);
        if (p == null) return NotFound(new { message = "Không tìm thấy permission." });

        p.Name = dto.Name;
        p.Description = dto.Description;
        await _db.SaveChangesAsync();
        return Ok(new { message = "Đã cập nhật thông tin permission." });
    }

    /// <summary>Quét lại toàn bộ endpoint mà không cần khởi động lại app.</summary>
    [HttpPost("sync")]
    public async Task<IActionResult> Sync([FromServices] IActionDescriptorCollectionProvider provider)
    {
        await EndpointPermissionSync.SyncAsync(_db, provider);
        return Ok(new { message = "Đã đồng bộ endpoint." });
    }
}