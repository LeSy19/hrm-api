using BackendApp.Constants;
using BackendApp.Data;
using BackendApp.DTOs;
using BackendApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Services;

public interface IRolePermissionService
{
    Task<List<ModulePermissionsDto>> GetAllPermissionsGroupedByModuleAsync();
    Task<List<int>> GetPermissionIdsByRoleIdAsync(int roleId);
    Task<List<string>> GetPermissionCodesByRoleIdAsync(int roleId);
    Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds);
}

public class RolePermissionService : IRolePermissionService
{
    private readonly AppDbContext _dbContext;
    private readonly IPermissionService _permissionService;

    public RolePermissionService(AppDbContext dbContext, IPermissionService permissionService)
    {
        _dbContext = dbContext;
        _permissionService = permissionService;
    }

    public async Task<List<ModulePermissionsDto>> GetAllPermissionsGroupedByModuleAsync()
    {
        var permissions = await _dbContext.Permissions
            .Where(p => p.IsActive)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.DisplayOrder)
            .ThenBy(p => p.Action)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Code = p.Code,
                Name = p.Name,
                Description = p.Description,
                Module = p.Module,
                Action = p.Action,
                HttpMethod = p.HttpMethod,
                RouteTemplate = p.RouteTemplate,
                DisplayOrder = p.DisplayOrder
            })
            .ToListAsync();

        return permissions
            .GroupBy(p => p.Module)
            .Select(g => new ModulePermissionsDto
            {
                Module = g.Key,
                Permissions = g.ToList()
            })
            .ToList();
    }

    public async Task<List<int>> GetPermissionIdsByRoleIdAsync(int roleId)
    {
        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.Permission.IsActive)
            .Select(rp => rp.PermissionId)
            .ToListAsync();
    }

    public async Task<List<string>> GetPermissionCodesByRoleIdAsync(int roleId)
    {
        return await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId && rp.Permission.IsActive)
            .Select(rp => rp.Permission.Code)
            .ToListAsync();
    }

    public async Task UpdateRolePermissionsAsync(int roleId, List<int> permissionIds)
    {
        var role = await _dbContext.Roles.FindAsync(roleId)
            ?? throw new KeyNotFoundException($"Role with ID {roleId} not found.");

        if (string.Equals(role.Name, UserRoles.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Không thể chỉnh sửa quyền của role ADMIN (luôn có toàn quyền).");
        }

        // Chỉ chấp nhận các Permission thật sự tồn tại và đang hoạt động
        var requestedIds = permissionIds.Distinct().ToList();
        var validIds = await _dbContext.Permissions
            .Where(p => p.IsActive && requestedIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync();
        var targetIds = validIds.ToHashSet();

        var existing = await _dbContext.RolePermissions
            .Where(rp => rp.RoleId == roleId)
            .ToListAsync();
        var existingIds = existing.Select(rp => rp.PermissionId).ToHashSet();

        var toRemove = existing.Where(rp => !targetIds.Contains(rp.PermissionId)).ToList();
        var toAdd = targetIds
            .Where(id => !existingIds.Contains(id))
            .Select(id => new RolePermission
            {
                RoleId = roleId,
                PermissionId = id,
                AssignedAt = DateTime.UtcNow
            })
            .ToList();

        if (toRemove.Count > 0) _dbContext.RolePermissions.RemoveRange(toRemove);
        if (toAdd.Count > 0) await _dbContext.RolePermissions.AddRangeAsync(toAdd);

        await _dbContext.SaveChangesAsync();

        await _permissionService.InvalidateRolePermissionsCacheAsync(roleId);
    }
}