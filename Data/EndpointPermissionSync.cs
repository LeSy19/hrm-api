using BackendApp.Configurations.Authorization;
using BackendApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace BackendApp.Data;

/// <summary>
/// Quét toàn bộ action trong controller và đồng bộ vào bảng Permissions.
/// </summary>
public static class EndpointPermissionSync
{
    public static async Task SyncAsync(AppDbContext db, IActionDescriptorCollectionProvider provider)
    {
        var found = provider.ActionDescriptors.Items
            .OfType<ControllerActionDescriptor>()
            .Where(d => !d.EndpointMetadata.Any(m => m is IAllowAnonymous || m is AuthenticatedOnlyAttribute))
            .Select(d => new
            {
                Code = PermissionKey.From(d),
                Module = d.ControllerName,
                Action = d.ActionName,
                Method = d.ActionConstraints?.OfType<HttpMethodActionConstraint>()
                            .FirstOrDefault()?.HttpMethods.FirstOrDefault() ?? "GET",
                Route = d.AttributeRouteInfo?.Template ?? ""
            })
            .GroupBy(x => x.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        var existing = (await db.Permissions.ToListAsync())
            .ToDictionary(p => p.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var e in found)
        {
            if (existing.TryGetValue(e.Code, out var p))
            {
                // Chỉ cập nhật thông tin kỹ thuật, KHÔNG ghi đè Name/Description admin đã sửa
                p.HttpMethod = e.Method;
                p.RouteTemplate = e.Route;
                p.Module = e.Module;
                p.Action = e.Action;
                p.IsActive = true;
            }
            else
            {
                db.Permissions.Add(new Permission
                {
                    Code = e.Code,
                    Name = $"{e.Method} /{e.Route}",
                    Description = string.Empty,
                    Module = e.Module,
                    Action = e.Action,
                    HttpMethod = e.Method,
                    RouteTemplate = e.Route,
                    DisplayOrder = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        // Endpoint đã bị xóa khỏi code thì vô hiệu hóa
        var foundCodes = found.Select(f => f.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var p in existing.Values.Where(p => !foundCodes.Contains(p.Code)))
        {
            p.IsActive = false;
        }

        await db.SaveChangesAsync();
    }
}